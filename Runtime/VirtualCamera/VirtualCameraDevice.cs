using System;
using System.Collections.Generic;
using System.Linq;
using Unity.LiveCapture.CompanionApp;
using Unity.LiveCapture.VirtualCamera.Rigs;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A device used to control a virtual camera.
    /// </summary>
    /// <remarks>
    /// The virtual camera mimics the experience of using a real camera in a Unity scene. The connected client can
    /// control most of the state, such as the camera pose and lens settings, but other features like autofocus need to
    /// be computed in the editor as it needs to query the scene. The render from the virtual camera in the editor can
    /// be streamed to the client to give visual feedback of the camera state, similar to a camera viewfinder.
    /// A <see cref="VirtualCameraActor"/> and a <see cref="IVirtualCameraClient"/> must be assigned before the device
    /// is useful. The actor is needed to store live or evaluated playback state and affect the scene.
    /// </remarks>
    [AddComponentMenu("")]
    [RequireComponent(typeof(FocusPlaneRenderer))]
    [CreateDeviceMenuItemAttribute("Virtual Camera Device")]
    [HelpURL(Documentation.baseURL + "ref-component-virtual-camera-device" + Documentation.endURL)]
    [DisallowMultipleComponent]
    public class VirtualCameraDevice : CompanionAppDevice<IVirtualCameraClient>, ITimedDataSource
    {
        // Due to a quirk in how LensPostProcessor works, if there's not enough keyframes
        // in the buffer, we can run into a situation where we constantly "miss" the
        // keyframes when performing the sampling. Setting the buffer size to some higher value
        // minimizes this possibility.
        const int k_MinBufferSize = 5;

        const int k_TimeShiftTolerance = 15;

        // If the editor/app runs slower than this fps, generate interpolated lens samples at this fps
        readonly FrameRate k_MinSamplingFrameRate = StandardFrameRate.FPS_60_00;

        static readonly Snapshot[] k_EmptySnapshots = new Snapshot[0];

        static List<VirtualCameraDevice> s_Instances = new List<VirtualCameraDevice>();

        internal static IEnumerable<VirtualCameraDevice> instances => s_Instances;

        [SerializeField, HideInInspector]
        string m_Guid;

        [SerializeField]
        internal LensAsset m_DefaultLensAsset;

        [SerializeField]
        VirtualCameraActor m_Actor;

        [SerializeField, EnumFlagButtonGroup(100f)]
        VirtualCameraChannelFlags m_Channels = VirtualCameraChannelFlags.All;

        /// <summary>
        /// The current lens as set from the inspector or the companion app
        /// </summary>
        /// <remarks>
        /// Note that this is not necessarily the lens used on the actor, due to
        /// post-processing.
        /// </remarks>
        [SerializeField]
        Lens m_Lens = Lens.DefaultParams;

        [SerializeField]
        LensAsset m_LensAsset;

        [SerializeField]
        LensIntrinsics m_LensIntrinsics = LensIntrinsics.DefaultParams;

        [SerializeField]
        CameraBody m_CameraBody = CameraBody.DefaultParams;

        [SerializeField]
        VirtualCameraRigState m_Rig = VirtualCameraRigState.Identity;

        [SerializeField]
        Settings m_Settings = Settings.DefaultData;

        // Obsolete. Use m_SnapshotLibrary instead.
        [SerializeField, NonReorderable]
        internal List<Snapshot> m_Snapshots = new List<Snapshot>();
        [SerializeField]
        SnapshotLibrary m_SnapshotLibrary;
        VideoServer m_VideoServer = new VideoServer();

        [SerializeField, HideInInspector]
        FrameTime m_SyncPresentationOffset;

        [SerializeField]
        bool m_IsSynchronized;

        [SerializeField, HideInInspector]
        int m_BufferSize = k_MinBufferSize;

        float m_LastPoseTimeStamp;

        float m_LastJoysticksTimeStamp;
        bool m_PoseRigNeedsInitialize;
        IRaycaster m_Raycaster;
        ICameraDriver m_Driver;
        FocusPlaneRenderer m_FocusPlaneRenderer;
        VirtualCameraRecorder m_Recorder = new VirtualCameraRecorder();
        TimestampTracker m_TimestampTracker = new TimestampTracker();
        MeshIntersectionTracker m_MeshIntersectionTracker = new MeshIntersectionTracker();
        Lens m_LensMetadata;
        LensAsset m_LastLensAsset;
        Snapshot[] m_LastSnapshots;
        bool m_LensIntrinsicsDirty;
        bool m_LastScreenAFRaycastIsValid;
        IScreenshotImpl m_ScreenshotImpl = new ScreenshotImpl();
        bool m_ActorAlignRequested;
        VirtualCameraActor m_LastActor;

        internal VirtualCameraRecorder Recorder => m_Recorder;

        static readonly FrameRate k_SyncBufferNominalFrameRate = StandardFrameRate.FPS_60_00;
        TimedDataBuffer<Pose> m_PoseBuffer;
        LensPostProcessor m_LensPostProcessor;

        // Record the first synced sample to arrive after start of recording
        double? m_FirstSampleTimecode;
        ClientTimeEstimator m_ClientTimeEstimator = new ClientTimeEstimator();

        // Values to be used in LiveUpdate()
        Lens m_LensForActorUpdate;
        Pose m_PoseForActorUpdate;

        /// <summary>
        /// Gets the <see cref="VirtualCameraActor"/> currently assigned to this device.
        /// </summary>
        /// <returns>The assigned actor, or null if none is assigned.</returns>
        public VirtualCameraActor Actor
        {
            get => m_Actor;
            set
            {
                if (m_Actor != value)
                {
                    m_Actor = value;
                    ValidateActor();
                }
            }
        }

        /// <summary>
        /// The position and rotation of the current device in world coordinates.
        /// </summary>
        public Pose Pose => m_Rig.Pose;

        /// <summary>
        /// The position and rotation of the world's origin.
        /// </summary>
        public Pose Origin => m_Rig.Origin;

        internal Pose LocalPose => m_Rig.LocalPose;

        /// <summary>
        /// The <see cref="VirtualCamera.Settings"/> of the current device.
        /// </summary>
        internal Settings Settings
        {
            get => m_Settings;
            set
            {
                if (m_Settings != value)
                {
                    m_Settings = value;
                    m_Settings.Validate();
                    m_Rig.Refresh(GetRigSettings());
                    SetFocusMode(m_Settings.FocusMode);
                    Refresh();
                }
            }
        }

        /// <summary>
        /// The <see cref="VirtualCamera.Lens"/> of the current device.
        /// </summary>
        public Lens Lens
        {
            get => m_Lens;
            set
            {
                m_Lens = value;
                ValidateLensIntrinsics();
                m_LensPostProcessor.Reset(m_Lens, m_ClientTimeEstimator.Now);
                m_LensForActorUpdate = m_Lens;
            }
        }

        /// <summary>
        /// The <see cref="VirtualCamera.LensAsset"/> of the current device.
        /// </summary>
        public LensAsset LensAsset
        {
            get => m_LensAsset;
            set
            {
                if (m_LensAsset != value)
                {
                    m_LensAsset = value;
                    ValidateLensIntrinsics();
                }
            }
        }

        /// <summary>
        /// The <see cref="VirtualCamera.LensIntrinsics"/> of the current device.
        /// </summary>
        public LensIntrinsics LensIntrinsics => m_LensIntrinsics;

        /// <summary>
        /// The <see cref="VirtualCamera.CameraBody"/> of the current device.
        /// </summary>
        public CameraBody CameraBody => m_CameraBody;

        /// <summary>
        /// The <see cref="VirtualCamera.SnapshotLibrary"/> of the current device.
        /// </summary>
        internal SnapshotLibrary SnapshotLibrary
        {
            get => m_SnapshotLibrary;
            set => m_SnapshotLibrary = value;
        }

        bool TryGetInternalClient(out IVirtualCameraClientInternal client)
        {
            client = GetClient() as IVirtualCameraClientInternal;

            return client != null;
        }

        void InitializeDriver()
        {
            m_Driver = null;

            if (m_Actor != null)
            {
                m_Driver = m_Actor.GetComponent(typeof(ICameraDriver)) as ICameraDriver;
            }
        }

        LensPostProcessor CreateLensPostProcessor()
        {
            return new LensPostProcessor
            {
                UpdateFocusRigFunction = (distance, reticlePositionChanged) =>
                {
                    if (!reticlePositionChanged &&
                        (m_Settings.FocusMode == FocusMode.Clear || m_Settings.FocusMode == FocusMode.Manual))
                    {
                        return (false, distance);
                    }

                    var focusUpdated = UpdateAutoFocusDistance(ref distance, reticlePositionChanged);
                    return (focusUpdated, distance);
                },
                ValidationFunction = lens =>
                {
                    lens.Validate(m_LensIntrinsics);
                    return lens;
                },
                BufferSize = m_BufferSize
            };
        }

        void Reset()
        {
            m_LensAsset = m_DefaultLensAsset;

            if (m_LensAsset != null)
            {
                m_Lens = m_LensAsset.DefaultValues;
            }

            // Find & assign the first actor not already assigned to a device.
            // Do nothing if there's multiple potential candidates.
            if (m_Actor == null)
            {
                VirtualCameraActor validActor = null;
                var actors = FindObjectsOfType(typeof(VirtualCameraActor));

                foreach (var instance in actors)
                {
                    var actor = instance as VirtualCameraActor;

                    // Actors from a different scene aren't a valid object reference
                    if (actor.gameObject.scene == gameObject.scene)
                    {
                        var alreadyAssignedToADevice = false;

                        foreach (var device in s_Instances)
                        {
                            if (device.m_Actor == actor)
                            {
                                alreadyAssignedToADevice = true;
                                break;
                            }
                        }

                        if (alreadyAssignedToADevice)
                        {
                            continue;
                        }

                        if (validActor == null)
                        {
                            validActor = actor;
                        }
                        else
                        {
                            validActor = null;
                            break;
                        }
                    }
                }

                m_Actor = validActor;
            }
        }

        /// <inheritdoc/>
        protected virtual void OnValidate()
        {
            ValidateActor();
            ValidateLensIntrinsics();

            m_Settings.Validate();
            m_CameraBody.Validate();
            m_Rig.Refresh(GetRigSettings());
            UpdateOverlaysIfNeeded(m_Settings);

            if (m_LensPostProcessor == null)
            {
                m_LensPostProcessor = CreateLensPostProcessor();
            }

            if (IsSynchronized)
            {
                m_LensPostProcessor.AddLensKeyframe(GetSynchronizerTime(), m_Lens);
            }
            else
            {
                m_LensPostProcessor.AddLensKeyframe(m_ClientTimeEstimator.Now, m_Lens);
            }
        }

        double GetSynchronizerTime() =>
            Synchronizer != null ? Synchronizer.CurrentTimecode.ToSeconds(Synchronizer.FrameRate) : default;

        void ValidateActor()
        {
            InitializeDriver();

            // If actor has changed
            if (m_Actor != null && m_Actor != m_LastActor)
            {
                m_LastActor = m_Actor;

                UpdateRigOriginFromActor();
            }
        }

        void UpdateOverlaysIfNeeded(Settings settings)
        {
            if (m_FocusPlaneRenderer != null)
            {
                m_FocusPlaneRenderer.enabled = settings.FocusPlane;
            }

            var currentCamera = GetCamera();
            if (currentCamera != null && FrameLinesMap.Instance.TryGetInstance(currentCamera, out var frameLines))
            {
                frameLines.GateFit = settings.GateFit;
                frameLines.GateMaskEnabled = settings.GateMask;
                frameLines.AspectRatioEnabled = settings.AspectRatioLines;
                frameLines.CenterMarkerEnabled = settings.CenterMarker;
            }
        }

        void ValidateLensIntrinsics()
        {
            var oldIntrinsics = m_LensIntrinsics;

            if (m_LensAsset != null)
            {
                m_LensIntrinsics = m_LensAsset.Intrinsics;
            }
            else if (m_DefaultLensAsset != null)
            {
                m_LensIntrinsics = m_DefaultLensAsset.Intrinsics;
            }
            else
            {
                m_LensIntrinsics = LensIntrinsics.DefaultParams;
            }

            m_LensIntrinsics.Validate();
            m_Lens.Validate(m_LensIntrinsics);
            m_LensIntrinsicsDirty = m_LensIntrinsics != oldIntrinsics;
        }

        /// <inheritdoc/>
        protected override void OnSlateChanged(ISlate slate)
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendVirtualCameraTrackMetadataListDescriptor(VcamTrackMetadataListDescriptor.Create(slate));
            }
        }

        internal void SetScreenshotImpl(IScreenshotImpl impl)
        {
            Debug.Assert(impl != null);

            m_ScreenshotImpl = impl;
        }

        internal int GetSnapshotCount()
        {
            return m_SnapshotLibrary == null ? 0 : m_SnapshotLibrary.Count;
        }

        internal Snapshot GetSnapshot(int index)
        {
            if (m_SnapshotLibrary == null)
            {
                return null;
            }

            return m_SnapshotLibrary.Get(index);
        }

        internal void TakeSnapshot()
        {
            if (IsRecording() || !this.IsLiveAndReady())
            {
                return;
            }

            SnapshotLibraryUtility.EnforceSnapshotLibrary(this);

            var snapshot = new Snapshot()
            {
                Pose = m_Rig.Pose,
                LensAsset = m_LensAsset,
                Lens = m_Lens,
                CameraBody = m_CameraBody,
            };

            var sceneNumber = 0;
            var shotName = string.Empty;
            var takeRecorder = GetTakeRecorder();

            if (takeRecorder != null)
            {
                var slate = takeRecorder.GetActiveSlate();

                snapshot.Slate = slate;
                snapshot.Time = takeRecorder.GetPreviewTime();
                snapshot.FrameRate = takeRecorder.FrameRate;

                if (slate != null)
                {
                    shotName = slate.ShotName;
                    sceneNumber = slate.SceneNumber;
                }
            }

            snapshot.Screenshot = m_ScreenshotImpl.Take(
                GetCamera(),
                sceneNumber,
                shotName,
                snapshot.Time,
                snapshot.FrameRate);

            SnapshotLibraryUtility.AddSnapshot(m_SnapshotLibrary, snapshot);
        }

        internal void GoToSnapshot(int index)
        {
            if (m_SnapshotLibrary == null)
            {
                return;
            }

            GoToSnapshot(m_SnapshotLibrary.Get(index));
        }

        internal void GoToSnapshot(Snapshot snapshot)
        {
            if (snapshot == null || IsRecording())
            {
                return;
            }

            SetOrigin(snapshot.Pose);

            var takeRecorderInternal = GetTakeRecorder() as ITakeRecorderInternal;

            if (takeRecorderInternal != null)
            {
                takeRecorderInternal.SetPreviewTime(snapshot.Slate, snapshot.Time);
            }

            Refresh();
        }

        internal void LoadSnapshot(int index)
        {
            if (m_SnapshotLibrary == null)
            {
                return;
            }

            LoadSnapshot(m_SnapshotLibrary.Get(index));
        }

        internal void LoadSnapshot(Snapshot snapshot)
        {
            if (snapshot == null || IsRecording())
            {
                return;
            }

            var snapshotLensAsset = snapshot.LensAsset;

            if (snapshotLensAsset != null)
            {
                LensAsset = snapshotLensAsset;
            }

            Lens = snapshot.Lens;
            m_CameraBody = snapshot.CameraBody;

            GoToSnapshot(snapshot);
        }

        internal void DeleteSnapshot(int index)
        {
            if (m_SnapshotLibrary == null || IsRecording())
            {
                return;
            }

            m_SnapshotLibrary.RemoveAt(index);
        }

        void Awake()
        {
            m_FocusPlaneRenderer = GetComponent<FocusPlaneRenderer>();
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            TimedDataSourceManager.Instance.EnsureIdIsValid(ref m_Guid);
            TimedDataSourceManager.Instance.Register(this);

            m_MeshIntersectionTracker.Initialize();
            m_Raycaster = RaycasterFactory.Create();

            InitializeDriver();

            s_Instances.Add(this);
            m_PoseBuffer = new TimedDataBuffer<Pose>(k_SyncBufferNominalFrameRate, m_BufferSize);
            m_LensPostProcessor = CreateLensPostProcessor();
            m_ClientTimeEstimator.Reset();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            s_Instances.Remove(this);

            TimedDataSourceManager.Instance.Unregister(this);

            RaycasterFactory.Dispose(m_Raycaster);
            m_MeshIntersectionTracker.Dispose();

            base.OnDisable();

            Refresh();
        }

        /// <inheritdoc/>
        public override bool IsReady()
        {
            return base.IsReady() && m_Actor != null;
        }

        /// <inheritdoc/>
        public override void Write(ITakeBuilder takeBuilder)
        {
            if (m_Actor == null || m_Recorder.IsEmpty())
            {
                return;
            }

            var metadata = new VirtualCameraTrackMetadata
            {
                Channels = m_Recorder.Channels,
                Lens = m_LensMetadata,
                CameraBody = m_CameraBody,
                LensAsset = m_LensAsset,
                CropAspect = m_Settings.AspectRatio
            };

            takeBuilder.CreateAnimationTrack(
                "Virtual Camera",
                m_Actor.Animator,
                m_Recorder.Bake(),
                metadata,
                m_FirstSampleTimecode);
        }

        /// <summary>
        /// Gets the video server used to stream the shot from to this virtual camera.
        /// </summary>
        /// <returns>The currently assigned video server, or null if none is assigned.</returns>
        internal VideoServer GetVideoServer()
        {
            return m_VideoServer;
        }

        internal void RequestAlignWithActor()
        {
            m_ActorAlignRequested = true;
        }

        internal void SetOrigin(Pose pose)
        {
            var originRotation = Quaternion.Euler(0f, pose.rotation.eulerAngles.y, 0f);

            m_Rig.Origin = new Pose()
            {
                position = pose.position,
                rotation = originRotation
            };

            m_Rig.WorldToLocal(pose);
            m_Rig.Refresh(GetRigSettings());
        }

        internal void SetLocalPose(Pose pose)
        {
            m_Rig.LocalPose = pose;
            m_Rig.Refresh(GetRigSettings());
        }

        void SetFocusMode(FocusMode focusMode)
        {
            var lastDepthOfFieldEnabled = IsDepthOfFieldEnabled();
            m_Settings.FocusMode = focusMode;
            m_LastScreenAFRaycastIsValid = false;
            var depthOfFieldEnabled = IsDepthOfFieldEnabled();

            if (lastDepthOfFieldEnabled != depthOfFieldEnabled)
            {
                Refresh();

                if (IsRecording())
                {
                    m_Recorder.RecordEnableDepthOfField(depthOfFieldEnabled);
                }
            }
        }

        internal void SetReticlePosition(Vector2 reticlePosition)
        {
            m_Settings.ReticlePosition = reticlePosition;
            m_Settings.Validate();

            m_LensPostProcessor.MarkReticlePositionChanged();

            SendSettings();
        }

        /// <inheritdoc/>
        public override void UpdateClient()
        {
            base.UpdateClient();

            SendChannelFlags();
            SendLensKitIfNeeded();
            SendLens();
            SendCameraBody();
            SendSettings();
            SendVideoStreamState();
            SendSnapshotsIfNeeded();
        }

        /// <summary>
        /// Specify a fixed delta time for testing or offline processing.
        /// </summary>
        internal double? DeltaTimeOverride { get; set; }

        internal void MarkClientTime(double time)
        {
            m_ClientTimeEstimator.Mark(time);
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            if (m_ActorAlignRequested)
            {
                m_ActorAlignRequested = false;
                UpdateRigOriginFromActor();
            }

            ValidateLensIntrinsics();
            UpdateOverlaysIfNeeded(m_Settings);
            UpdateTimestampTracker();
            UpdateRecorder();
            UpdateLensPostProcessorSettings();
            var deltaTime = DeltaTimeOverride ?? Time.unscaledDeltaTime;
            m_ClientTimeEstimator.Update(deltaTime);

            // If we're updating slower than k_MinSamplingFrameRate,
            // generate samples at k_MinSamplingFrameRate.
            // Otherwise, generate samples at the game's/editor's fps.
            m_LensPostProcessor.SamplingFrameRate =
                deltaTime > k_MinSamplingFrameRate.FrameInterval ?
                    k_MinSamplingFrameRate :
                    new FrameRate((int)Math.Round(1.0 / deltaTime));

            if (!IsSynchronized)
            {
                // Unsynchronized updates to for post-processing lens samples works like the synchronized case,
                // except that we're using the estimated client time instead of the Timecode provider time
                Lens? maybeLensSample = null;

                if (DeltaTimeOverride == null &&
                    Math.Abs(m_ClientTimeEstimator.Now - m_LensPostProcessor.CurrentTime) > deltaTime * k_TimeShiftTolerance)
                {
                    // Device time shift detected. This can happen
                    // 1) on the first update, or client was restarted
                    // 2) when a timecode source was selected/changed on the device
                    // 3) if there is a really long gap between pose samples, and the clocks have drifted
                    m_LensPostProcessor.Reset(m_Lens, m_ClientTimeEstimator.Now);

                    // This could cause a discontinuity if we were in the middle of doing an interpolation,
                    // but it's more important that the actor gets updated with the correct value.
                    maybeLensSample = m_Lens;
                }

                // Compute post-processed lens samples. There could be multiple samples per update
                // if we are interpolating at a high frame rate.
                foreach (var sample in m_LensPostProcessor.ProcessTo(m_ClientTimeEstimator.Now))
                {
                    maybeLensSample = sample.lens;
                    if (IsRecording())
                    {
                        m_TimestampTracker.SetTimestamp((float)sample.time);
                        Recorder.Time = m_TimestampTracker.LocalTime;
                        m_Recorder.RecordAperture(sample.lens.Aperture);
                        m_Recorder.RecordFocusDistance(sample.lens.FocusDistance);
                        m_Recorder.RecordFocalLength(sample.lens.FocalLength);
                    }
                }

                // Use the last post-processed lens sample for livelink update
                UpdateActorData(maybeLensSample ?? m_LensForActorUpdate, m_Rig.Pose);

                // Expose the target (unfiltered) focal length for the UI
                m_Lens.FocusDistance = m_LensPostProcessor.FocusDistanceTarget;
            }

            var camera = GetCamera();
            m_FocusPlaneRenderer.SetCamera(camera);
            m_VideoServer.Camera = camera;
            m_VideoServer.Update();

            UpdateClient();
        }

        void UpdateActorData(Lens lens, Pose pose)
        {
            m_LensForActorUpdate = lens;
            m_PoseForActorUpdate = pose;
        }

        /// <inheritdoc/>
        public override void LiveUpdate()
        {
            Debug.Assert(m_Actor != null, "Actor is null");

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                m_Actor.LocalPosition = m_PoseForActorUpdate.position;
                m_Actor.LocalPositionEnabled = true;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                m_Actor.LocalEulerAngles = m_PoseForActorUpdate.rotation.eulerAngles;
                m_Actor.LocalEulerAnglesEnabled = true;
            }

            var lens = m_Actor.Lens;
            var lensIntrinsics = m_Actor.LensIntrinsics;

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
            {
                lens.FocalLength = m_LensForActorUpdate.FocalLength;
                lensIntrinsics.FocalLengthRange = m_LensIntrinsics.FocalLengthRange;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                m_Actor.DepthOfFieldEnabled = IsDepthOfFieldEnabled();

                lens.FocusDistance = m_LensForActorUpdate.FocusDistance;
                lensIntrinsics.CloseFocusDistance = m_LensIntrinsics.CloseFocusDistance;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.Aperture))
            {
                lens.Aperture = m_LensForActorUpdate.Aperture;
                lensIntrinsics.ApertureRange = m_LensIntrinsics.ApertureRange;
            }

            lensIntrinsics.LensShift = m_LensIntrinsics.LensShift;
            lensIntrinsics.BladeCount = m_LensIntrinsics.BladeCount;
            lensIntrinsics.Curvature = m_LensIntrinsics.Curvature;
            lensIntrinsics.BarrelClipping = m_LensIntrinsics.BarrelClipping;
            lensIntrinsics.Anamorphism = m_LensIntrinsics.Anamorphism;

            m_Actor.Lens = lens;
            m_Actor.LensIntrinsics = lensIntrinsics;
            m_Actor.CameraBody = m_CameraBody;
            m_Actor.CropAspect = m_Settings.AspectRatio;

            if (m_Driver is ICustomDamping customDamping)
                customDamping.SetDamping(m_Settings.Damping);
        }

        Camera GetCamera()
        {
            var camera = default(Camera);

            if (m_Driver != null)
            {
                camera = m_Driver.GetCamera();
            }

            return camera;
        }

        void UpdateRecorder()
        {
            var time = (float)GetTakeRecorder().GetPreviewTime();

            m_Recorder.Channels = m_Channels;
            m_Recorder.Time = time;
        }

        void UpdateLensPostProcessorSettings()
        {
            m_LensPostProcessor.ApertureDamping = m_Settings.ApertureDamping;
            m_LensPostProcessor.FocalLengthDamping = m_Settings.FocalLengthDamping;
            m_LensPostProcessor.FocusDistanceDamping = m_Settings.FocusDistanceDamping;
        }

        void UpdateTimestampTracker()
        {
            m_TimestampTracker.Time = (float)GetTakeRecorder().GetPreviewTime();
        }

        /// <inheritdoc/>
        protected override string GetAssetName()
        {
            return m_Actor != null ? m_Actor.name : name;
        }

        /// <inheritdoc/>
        protected override void OnClientAssigned()
        {
            Register();

            m_LastLensAsset = null;

            m_PoseRigNeedsInitialize = true;
            StartVideoServer();
        }

        /// <inheritdoc/>
        protected override void OnClientUnassigned()
        {
            Unregister();
            StopVideoServer();
        }

        void Register()
        {
            if (TryGetInternalClient(out var client))
            {
                client.ChannelFlagsReceived += OnChannelFlagsReceived;
                client.PoseSampleReceived += OnPoseSampleReceived;
                client.FocalLengthSampleReceived += OnFocalLengthSampleReceived;
                client.FocusDistanceSampleReceived += OnFocusDistanceSampleReceived;
                client.ApertureSampleReceived += OnApertureSampleReceived;
                client.DampingEnabledReceived += OnDampingEnabledReceived;
                client.BodyDampingReceived += OnBodyDampingReceived;
                client.AimDampingReceived += OnAimDampingReceived;
                client.FocalLengthDampingReceived += OnFocalLengthDampingReceived;
                client.FocusDistanceDampingReceived += OnFocusDistanceDampingReceived;
                client.ApertureDampingReceived += OnApertureDampingReceived;
                client.PositionLockReceived += OnPositionLockReceived;
                client.RotationLockReceived += OnRotationLockReceived;
                client.AutoHorizonReceived += OnAutoHorizonReceived;
                client.ErgonomicTiltReceived += OnErgonomicTiltReceived;
                client.RebasingReceived += OnRebasingReceived;
                client.MotionScaleReceived += OnMotionScaleReceived;
                client.JoystickSensitivityReceived += OnJoystickSensitivityReceived;
                client.PedestalSpaceReceived += OnPedestalSpaceReceived;
                client.FocusModeReceived += OnFocusModeReceived;
                client.FocusReticlePositionReceived += OnFocusReticlePositionReceived;
                client.FocusDistanceOffsetReceived += OnFocusDistanceOffsetReceived;
                client.CropAspectReceived += OnCropAspectReceived;
                client.GateFitReceived += OnGateFitReceived;
                client.ShowGateMaskReceived += OnShowGateMaskReceived;
                client.ShowFrameLinesReceived += OnShowFrameLinesReceived;
                client.ShowCenterMarkerReceived += OnShowCenterMarkerReceived;
                client.ShowFocusPlaneReceived += OnShowFocusPlaneReceived;
                client.SetPoseToOrigin += OnSetPoseToOrigin;
                client.SetLensAsset += OnSetLensAsset;
                client.TakeSnapshot += OnTakeSnapshot;
                client.GoToSnapshot += OnGoToSnapshot;
                client.LoadSnapshot += OnLoadSnapshot;
                client.DeleteSnapshot += OnDeleteSnapshot;
                client.JoysticksSampleReceived += OnJoysticksSampleReceived;
            }
        }

        void Unregister()
        {
            if (TryGetInternalClient(out var client))
            {
                client.ChannelFlagsReceived -= OnChannelFlagsReceived;
                client.PoseSampleReceived -= OnPoseSampleReceived;
                client.FocalLengthSampleReceived -= OnFocalLengthSampleReceived;
                client.FocusDistanceSampleReceived -= OnFocusDistanceSampleReceived;
                client.ApertureSampleReceived -= OnApertureSampleReceived;
                client.DampingEnabledReceived -= OnDampingEnabledReceived;
                client.BodyDampingReceived -= OnBodyDampingReceived;
                client.AimDampingReceived -= OnAimDampingReceived;
                client.FocalLengthDampingReceived -= OnFocalLengthDampingReceived;
                client.FocusDistanceDampingReceived -= OnFocusDistanceDampingReceived;
                client.ApertureDampingReceived -= OnApertureDampingReceived;
                client.PositionLockReceived -= OnPositionLockReceived;
                client.RotationLockReceived -= OnRotationLockReceived;
                client.AutoHorizonReceived -= OnAutoHorizonReceived;
                client.ErgonomicTiltReceived -= OnErgonomicTiltReceived;
                client.RebasingReceived -= OnRebasingReceived;
                client.MotionScaleReceived -= OnMotionScaleReceived;
                client.JoystickSensitivityReceived -= OnJoystickSensitivityReceived;
                client.PedestalSpaceReceived -= OnPedestalSpaceReceived;
                client.FocusModeReceived -= OnFocusModeReceived;
                client.FocusReticlePositionReceived -= OnFocusReticlePositionReceived;
                client.FocusDistanceOffsetReceived -= OnFocusDistanceOffsetReceived;
                client.CropAspectReceived -= OnCropAspectReceived;
                client.GateFitReceived -= OnGateFitReceived;
                client.ShowGateMaskReceived -= OnShowGateMaskReceived;
                client.ShowFrameLinesReceived -= OnShowFrameLinesReceived;
                client.ShowCenterMarkerReceived -= OnShowCenterMarkerReceived;
                client.ShowFocusPlaneReceived -= OnShowFocusPlaneReceived;
                client.SetPoseToOrigin -= OnSetPoseToOrigin;
                client.SetLensAsset -= OnSetLensAsset;
                client.TakeSnapshot -= OnTakeSnapshot;
                client.GoToSnapshot -= OnGoToSnapshot;
                client.LoadSnapshot -= OnLoadSnapshot;
                client.DeleteSnapshot -= OnDeleteSnapshot;
                client.JoysticksSampleReceived -= OnJoysticksSampleReceived;
            }
        }

        /// <inheritdoc/>
        protected override void OnRecordingChanged()
        {
            if (IsRecording())
            {
                var frameRate = GetTakeRecorder().FrameRate;

                m_LensMetadata = m_Lens;

                m_TimestampTracker.Reset();
                m_FirstSampleTimecode = null;

                m_Recorder.FrameRate = frameRate;
                m_Recorder.Clear();

                UpdateTimestampTracker();

                RecordCurrentValues();

                // Lens interpolation requires continuous updates.
                Refresh();
            }
        }

        void UpdateRigOriginFromActor()
        {
            if (m_Actor != null)
            {
                SetOrigin(new Pose(m_Actor.transform.localPosition, m_Actor.transform.localRotation));
            }
        }

        void OnChannelFlagsReceived(VirtualCameraChannelFlags channelFlags)
        {
            m_Channels = channelFlags;
        }

        void OnJoysticksSampleReceived(JoysticksSample sample)
        {
            float timestamp = (float)sample.Time;
            m_TimestampTracker.SetTimestamp(timestamp);

            var settings = GetRigSettings();
            var frameInterval = 0d;
            var takeRecorder = GetTakeRecorder();
            var deltaTime = Mathf.Max(0f, timestamp - m_LastJoysticksTimeStamp);

            if (takeRecorder != null)
            {
                frameInterval = takeRecorder.FrameRate.FrameInterval;
            }

            if (deltaTime > frameInterval * 4d)
            {
                deltaTime = (float)frameInterval;
            }

            m_Rig.Translate(sample.Joysticks, deltaTime, m_Settings.JoystickSensitivity, m_Settings.PedestalSpace, settings);

            if (IsRecording())
            {
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.RecordPosition(m_Rig.Pose.position);
                m_Recorder.RecordRotation(m_Rig.Pose.rotation);
            }

            m_LastJoysticksTimeStamp = timestamp;

            Refresh();
        }

        void OnPoseSampleReceived(PoseSample sample)
        {
            float timestamp = (float)sample.Time;
            m_TimestampTracker.SetTimestamp(timestamp);
            var deltaTime = timestamp - m_LastPoseTimeStamp;

            // If true the state will refresh the last input and the rebase offset
            if (m_PoseRigNeedsInitialize)
            {
                m_Rig.LastInput = sample.Pose;
                m_Rig.RebaseOffset = Quaternion.Euler(0f, sample.Pose.rotation.eulerAngles.y - m_Rig.LocalPose.rotation.eulerAngles.y, 0f);
                m_PoseRigNeedsInitialize = false;
            }

            var settings = GetRigSettings();

            if (!(m_Driver is ICustomDamping))
                sample.Pose = VirtualCameraDamping.Calculate(m_Rig.LastInput, sample.Pose, m_Settings.Damping, deltaTime);

            m_Rig.Update(sample.Pose, settings);

            if (IsRecording())
            {
                if (IsSynchronized)
                {
                    if (m_FirstSampleTimecode == null)
                    {
                        m_FirstSampleTimecode = sample.Time;
                    }

                    m_Recorder.Time = sample.Time - m_FirstSampleTimecode.Value;
                }
                else
                {
                    m_Recorder.Time = m_TimestampTracker.LocalTime;
                }

                m_Recorder.RecordPosition(m_Rig.Pose.position);
                m_Recorder.RecordRotation(m_Rig.Pose.rotation);
            }

            m_LastPoseTimeStamp = timestamp;

            if (IsSynchronized)
            {
                m_PoseBuffer.Add(sample.Time, m_Rig.Pose);
            }
            else
            {
                Refresh();
            }

            // We use this event to track the time on the client. It's the most frequent
            // provider of timed samples
            MarkClientTime(sample.Time);
        }

        void OnFocalLengthSampleReceived(FocalLengthSample sample)
        {
            m_Lens.FocalLength = sample.FocalLength;
            m_Lens.Validate(m_LensIntrinsics);
            if (m_FirstSampleTimecode == null)
            {
                m_FirstSampleTimecode = sample.Time;
            }

            m_LensPostProcessor.AddFocalLengthKeyframe(sample.Time, sample.FocalLength);
        }

        void OnFocusDistanceSampleReceived(FocusDistanceSample sample)
        {
            m_Lens.FocusDistance = sample.FocusDistance;
            m_Lens.Validate(m_LensIntrinsics);

            if (m_FirstSampleTimecode == null)
            {
                m_FirstSampleTimecode = sample.Time;
            }


            m_LensPostProcessor.AddFocusDistanceKeyframe(sample.Time, m_Lens.FocusDistance);
        }

        void OnApertureSampleReceived(ApertureSample sample)
        {
            m_Lens.Aperture = sample.Aperture;
            m_Lens.Validate(m_LensIntrinsics);
            if (m_FirstSampleTimecode == null)
            {
                m_FirstSampleTimecode = sample.Time;
            }

            m_LensPostProcessor.AddApertureKeyframe(sample.Time, sample.Aperture);
        }

        void OnDampingEnabledReceived(bool damping)
        {
            var settings = Settings;
            settings.Damping.Enabled = damping;
            Settings = settings;
        }

        void OnBodyDampingReceived(Vector3 damping)
        {
            var settings = Settings;
            settings.Damping.Body = damping;
            Settings = settings;
        }

        void OnAimDampingReceived(float damping)
        {
            var settings = Settings;
            settings.Damping.Aim = damping;
            Settings = settings;
        }

        void OnFocalLengthDampingReceived(float damping)
        {
            var settings = Settings;
            settings.FocalLengthDamping = damping;
            Settings = settings;
        }

        void OnFocusDistanceDampingReceived(float damping)
        {
            var settings = Settings;
            settings.FocusDistanceDamping = damping;
            Settings = settings;
        }

        void OnApertureDampingReceived(float damping)
        {
            var settings = Settings;
            settings.ApertureDamping = damping;
            Settings = settings;
        }

        void OnPositionLockReceived(PositionAxis axes)
        {
            var settings = Settings;
            settings.PositionLock = axes;
            Settings = settings;
        }

        void OnRotationLockReceived(RotationAxis axes)
        {
            var settings = Settings;
            settings.RotationLock = axes;
            Settings = settings;
        }

        void OnAutoHorizonReceived(bool autoHorizon)
        {
            var settings = Settings;
            settings.AutoHorizon = autoHorizon;
            Settings = settings;
        }

        void OnErgonomicTiltReceived(float tilt)
        {
            var settings = Settings;
            settings.ErgonomicTilt = tilt;
            Settings = settings;
        }

        void OnRebasingReceived(bool rebasing)
        {
            var settings = Settings;
            settings.Rebasing = rebasing;
            Settings = settings;
        }

        void OnMotionScaleReceived(Vector3 scale)
        {
            var settings = Settings;
            settings.MotionScale = scale;
            Settings = settings;
        }

        void OnJoystickSensitivityReceived(Vector3 sensitivity)
        {
            var settings = Settings;
            settings.JoystickSensitivity = sensitivity;
            Settings = settings;
        }

        void OnPedestalSpaceReceived(Space space)
        {
            var settings = Settings;
            settings.PedestalSpace = space;
            Settings = settings;
        }

        void OnFocusModeReceived(FocusMode mode)
        {
            var settings = Settings;
            settings.FocusMode = mode;
            Settings = settings;
        }

        void OnFocusReticlePositionReceived(Vector2 position)
        {
            SetReticlePosition(position);
        }

        void OnFocusDistanceOffsetReceived(float offset)
        {
            var settings = Settings;
            settings.FocusDistanceOffset = offset;
            Settings = settings;
        }

        void OnCropAspectReceived(float aspect)
        {
            var settings = Settings;
            settings.AspectRatio = aspect;
            Settings = settings;
        }

        void OnGateFitReceived(GateFit gateFit)
        {
            var settings = Settings;
            settings.GateFit = gateFit;
            Settings = settings;
        }

        void OnShowGateMaskReceived(bool show)
        {
            var settings = Settings;
            settings.GateMask = show;
            Settings = settings;
        }

        void OnShowFrameLinesReceived(bool show)
        {
            var settings = Settings;
            settings.AspectRatioLines = show;
            Settings = settings;
        }

        void OnShowCenterMarkerReceived(bool show)
        {
            var settings = Settings;
            settings.CenterMarker = show;
            Settings = settings;
        }

        void OnShowFocusPlaneReceived(bool show)
        {
            var settings = Settings;
            settings.FocusPlane = show;
            Settings = settings;
        }

        void OnSetPoseToOrigin()
        {
            m_Rig.Reset();
            Refresh();
        }

        void OnSetLensAsset(SerializableGuid guid)
        {
#if UNITY_EDITOR
            var lensAsset = AssetDatabaseUtility.LoadAssetWithGuid<LensAsset>(guid.ToString());

            if (lensAsset == null)
            {
                lensAsset = m_DefaultLensAsset;
            }

            LensAsset = lensAsset;
#endif
        }

        void OnTakeSnapshot()
        {
            TakeSnapshot();
        }

        void OnGoToSnapshot(int index)
        {
            GoToSnapshot(index);
        }

        void OnLoadSnapshot(int index)
        {
            LoadSnapshot(index);
        }

        void OnDeleteSnapshot(int index)
        {
            DeleteSnapshot(index);
        }

        void RecordCurrentValues()
        {
            UpdateRecorder();

            m_Recorder.RecordPosition(m_Rig.Pose.position);
            m_Recorder.RecordRotation(m_Rig.Pose.rotation);
            m_Recorder.RecordEnableDepthOfField(IsDepthOfFieldEnabled());
            m_Recorder.RecordLensIntrinsics(m_LensIntrinsics);
            m_Recorder.RecordCropAspect(m_Settings.AspectRatio);
            m_Recorder.RecordAperture(m_LensForActorUpdate.Aperture);
            m_Recorder.RecordFocalLength(m_LensForActorUpdate.FocalLength);
            m_Recorder.RecordFocusDistance(m_LensForActorUpdate.FocusDistance);

            if (m_Recorder.Channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                m_Recorder.RecordLocalPositionEnabled(true);
            }

            if (m_Recorder.Channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                m_Recorder.RecordLocalEulerAnglesEnabled(true);
            }
        }

        bool UpdateAutoFocusDistance(ref float distance, bool reticlePositionChanged)
        {
            bool distanceUpdated = false;
            switch (m_Settings.FocusMode)
            {
                case FocusMode.Manual:
                case FocusMode.ReticleAF:
                {
                    if (m_Settings.FocusMode == FocusMode.Manual && !reticlePositionChanged)
                        throw new InvalidOperationException(
                            $"{nameof(UpdateAutoFocusDistance)} was invoked while focusMode is set to [{FocusMode.Manual}], " +
                            $"despite [{nameof(reticlePositionChanged)}] being false.");

                    m_LastScreenAFRaycastIsValid = m_Raycaster.Raycast(m_VideoServer.Camera, m_Settings.ReticlePosition, out distance);

                    if (!m_LastScreenAFRaycastIsValid)
                    {
                        distance = LensLimits.FocusDistance.y;
                    }
                    else if (m_Settings.FocusMode != FocusMode.Manual)
                    {
                        distance += m_Settings.FocusDistanceOffset;
                    }

                    distanceUpdated = true;
                    break;
                }
                case FocusMode.TrackingAF:
                {
                    if (reticlePositionChanged)
                    {
                        if (m_Raycaster.Raycast(m_VideoServer.Camera, m_Settings.ReticlePosition, out var ray, out var gameObject, out var hit))
                        {
                            m_MeshIntersectionTracker.TryTrack(gameObject, ray, hit.point);
                        }
                        else
                        {
                            m_MeshIntersectionTracker.Reset();
                            distance = LensLimits.FocusDistance.y;
                            distanceUpdated = true;
                        }
                    }

                    if (m_MeshIntersectionTracker.TryUpdate(out var worldPosition))
                    {
                        var cameraTransform = m_VideoServer.Camera.transform;
                        var hitVector = worldPosition - cameraTransform.position;
                        var depthVector = Vector3.Project(hitVector, cameraTransform.forward);
                        distance = depthVector.magnitude + m_Settings.FocusDistanceOffset;
                        distanceUpdated = true;
                    }

                    break;
                }
            }

            return distanceUpdated;
        }

        bool IsDepthOfFieldEnabled()
        {
            switch (m_Settings.FocusMode)
            {
                case FocusMode.Manual:
                    return true;
                case FocusMode.ReticleAF:
                    return m_LastScreenAFRaycastIsValid;
                case FocusMode.TrackingAF:
                    return m_MeshIntersectionTracker.CurrentMode != MeshIntersectionTracker.Mode.None;
            }

            return false;
        }

        VirtualCameraRigSettings GetRigSettings()
        {
            return new VirtualCameraRigSettings()
            {
                PositionLock = m_Settings.PositionLock,
                RotationLock = m_Settings.RotationLock,
                Rebasing = m_Settings.Rebasing,
                MotionScale = m_Settings.MotionScale,
                ErgonomicTilt = -m_Settings.ErgonomicTilt,
                ZeroDutch = m_Settings.AutoHorizon
            };
        }

        void StartVideoServer()
        {
            m_VideoServer.StartServer();
            InitializeVideoResolution();
        }

        void StopVideoServer()
        {
            m_VideoServer.StopServer();
        }

        void InitializeVideoResolution()
        {
            if (TryGetInternalClient(out var client))
            {
                m_VideoServer.BaseResolution = client.ScreenResolution;
            }
        }

        void SendChannelFlags()
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendChannelFlags(m_Channels);
            }
        }

        void SendLens()
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendLens(m_Lens);
            }
        }

        void SendLensKitIfNeeded()
        {
            if (m_LensAsset == m_LastLensAsset && !m_LensIntrinsicsDirty)
            {
                return;
            }

            if (TryGetInternalClient(out var client))
            {
                var descriptor = LensKitDescriptor.Create(m_LensAsset);

                client.SendLensKitDescriptor(descriptor);

                m_LastLensAsset = m_LensAsset;
                m_LensIntrinsicsDirty = false;
            }
        }

        void SendSnapshotsIfNeeded()
        {
            //Library is valid and we send contents to the client for the first time,
            //or we update client for the first time with empty contents
            var requiresUpdate = m_LastSnapshots == null;

            if (!requiresUpdate)
            {
                if (m_SnapshotLibrary == null)
                {
                    //Library is set to null.
                    requiresUpdate = m_LastSnapshots != k_EmptySnapshots;
                }
                else
                {
                    //Library's contents are different from what we've sent previously.
                    requiresUpdate = !Enumerable.SequenceEqual(m_LastSnapshots, m_SnapshotLibrary);
                }
            }

            if (requiresUpdate && TryGetInternalClient(out var client))
            {
                if (m_SnapshotLibrary == null)
                {
                    m_LastSnapshots = k_EmptySnapshots;
                }
                else
                {
                    m_LastSnapshots = m_SnapshotLibrary.Snapshots;
                }

                var descriptor = SnapshotListDescriptor.Create(m_LastSnapshots);

                client.SendSnapshotListDescriptor(descriptor);
            }
        }

        void SendCameraBody()
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendCameraBody(m_CameraBody);
            }
        }

        void SendSettings()
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendSettings(m_Settings);
            }
        }

        void SendVideoStreamState()
        {
            if (TryGetInternalClient(out var client))
            {
                client.SendVideoStreamState(new VideoStreamState
                {
                    IsRunning = m_VideoServer.IsRunning,
                    Port = m_VideoServer.Port,
                });
            }
        }

        /// <inheritdoc />
        public bool IsSynchronized
        {
            get => m_IsSynchronized;
            set
            {
                if (m_IsSynchronized != value)
                {
                    m_TimestampTracker.Reset();

                    // Set a reasonable initial time for the post processor. It doesn't have to be exact:
                    // just needs to be close to the "present" time
                    m_LensPostProcessor.Reset(m_Lens, value ? GetSynchronizerTime() : m_ClientTimeEstimator.Now);
                }

                m_IsSynchronized = value;
            }
        }

        /// <inheritdoc/>
        public ISynchronizer Synchronizer { get; set; }

        /// <inheritdoc/>
        public string Id => m_Guid;

        /// <inheritdoc/>
        string IRegistrable.FriendlyName => name;

        /// <inheritdoc/>
        public int BufferSize
        {
            get => m_BufferSize;
            set
            {
                var newSize = Math.Max(k_MinBufferSize, value);
                m_PoseBuffer.SetCapacity(newSize);
                m_LensPostProcessor.BufferSize = newSize;
                m_BufferSize = newSize;
            }
        }

        /// <inheritdoc/>
        public int? MaxBufferSize => null;
        /// <inheritdoc/>
        public int? MinBufferSize => k_MinBufferSize;


        /// <inheritdoc/>
        public FrameTime PresentationOffset
        {
            get => m_SyncPresentationOffset;
            set => m_SyncPresentationOffset = value;
        }

        /// <inheritdoc/>
        public TimedSampleStatus PresentAt(Timecode timecode, FrameRate frameRate)
        {
            Debug.Assert(IsSynchronized, "Attempting to call PresentAt() when data source is not being synchronized");

            m_LensPostProcessor.SamplingFrameRate = frameRate;

            // Get the frame time with respect to our buffer's frame rate
            var requestedFrameTime = FrameTime.Remap(timecode.ToFrameTime(frameRate), frameRate, m_PoseBuffer.FrameRate);

            // Apply offset (at our buffer's frame rate)
            var presentationTime = requestedFrameTime + PresentationOffset;

            Lens? maybeLensSample = null;

            // Note that is it not possible for lens keyframes to be behind, since the postprocessor
            // does extrapolation. However, it is possible for lens keyframes to be ahead or missing.
            foreach (var(time, lens) in m_LensPostProcessor.ProcessTo(presentationTime.ToSeconds(k_SyncBufferNominalFrameRate)))
            {
                // Record post-processed samples. Depending on the rate at which PresentAt is called,
                // we could potentially generate more than one sample per update.
                if (IsRecording() && m_FirstSampleTimecode is {} firstSampleTime)
                {
                    m_Recorder.Time = (float)(time - firstSampleTime);
                    m_Recorder.RecordAperture(lens.Aperture);
                    m_Recorder.RecordFocalLength(lens.FocalLength);
                    m_Recorder.RecordFocusDistance(lens.FocusDistance);
                }

                // Save the latest lens sample for presentation
                maybeLensSample = lens;
            }

            // Finally, try to retrieve the pose sample from the buffer
            var status = m_PoseBuffer.TryGetSample(presentationTime, out var pose);
            if (status != TimedSampleStatus.DataMissing)
            {
                UpdateActorData(maybeLensSample ?? m_LensForActorUpdate, pose);
                Refresh();
            }

            m_Lens.FocusDistance = m_LensPostProcessor.FocusDistanceTarget;

            return status;
        }
    }
}
