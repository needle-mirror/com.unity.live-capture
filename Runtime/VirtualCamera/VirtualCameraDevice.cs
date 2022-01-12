using System;
using System.Collections.Generic;
using Unity.LiveCapture.CompanionApp;
using Unity.LiveCapture.VirtualCamera.Rigs;
using UnityEngine;
using UnityEngine.Playables;

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
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "ref-component-virtual-camera-device" + Documentation.endURL)]
    [DisallowMultipleComponent]
    public class VirtualCameraDevice : CompanionAppDevice<IVirtualCameraClient>
    {
        const float k_Epsilon = 0.0001f;
        const float k_MaxSpeed = Mathf.Infinity;

        static List<VirtualCameraDevice> s_Instances = new List<VirtualCameraDevice>();
        internal static IEnumerable<VirtualCameraDevice> instances => s_Instances;

        [SerializeField]
        internal LensAsset m_DefaultLensAsset;
        [SerializeField]
        VirtualCameraActor m_Actor;
        [SerializeField, EnumFlagButtonGroup(100f)]
        VirtualCameraChannelFlags m_Channels = VirtualCameraChannelFlags.All;
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
        [SerializeField, NonReorderable]
        List<Snapshot> m_Snapshots = new List<Snapshot>();
        VideoServer m_VideoServer = new VideoServer();

        readonly SmoothDampSampler m_FocusDistanceSampler = new SmoothDampSampler();
        readonly SmoothDampSampler m_FocalLengthSampler = new SmoothDampSampler();
        readonly SmoothDampSampler m_ApertureSampler = new SmoothDampSampler();

        float m_FocusDistanceDampingVelocity;
        float m_FocalLengthDampingVelocity;
        float m_ApertureDampingVelocity;
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
        Lens m_InterpolatedLens;
        LensAsset m_LastLensAsset;
        bool m_LensIntrinsicsDirty;
        bool m_LastScreenAFRaycastIsValid;
        IScreenshotImpl m_ScreenshotImpl = new ScreenshotImpl();
        bool m_ActorAlignRequested;
        VirtualCameraActor m_LastActor;

        internal VirtualCameraRecorder Recorder => m_Recorder;

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
                m_InterpolatedLens = m_Lens;
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

        void Reset()
        {
            m_LensAsset = m_DefaultLensAsset;

            if (m_LensAsset != null)
            {
                m_Lens = m_LensAsset.DefaultValues;
                m_InterpolatedLens = m_Lens;
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

            if (!IsLive())
            {
                m_InterpolatedLens = m_Lens;
            }
        }

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

        internal void SetScreenshotImpl(IScreenshotImpl impl)
        {
            Debug.Assert(impl != null);

            m_ScreenshotImpl = impl;
        }

        internal int GetSnapshotCount()
        {
            return m_Snapshots.Count;
        }

        internal Snapshot GetSnapshot(int index)
        {
            return m_Snapshots[index];
        }

        internal void TakeSnapshot()
        {
            if (IsRecording() || !this.IsLiveActive())
            {
                return;
            }

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

            m_Snapshots.Add(snapshot);

            SendSnapshots();
        }

        internal void GoToSnapshot(int index)
        {
            if (index < 0 || index >= m_Snapshots.Count)
            {
                return;
            }

            GoToSnapshot(m_Snapshots[index]);
        }

        internal void GoToSnapshot(Snapshot snapshot)
        {
            if (IsRecording())
            {
                return;
            }

            SetOrigin(snapshot.Pose);

            var takeRecorderInternal = GetTakeRecorder() as ITakeRecorderInternal;

            if (takeRecorderInternal != null)
            {
                takeRecorderInternal.SetPreviewTime(snapshot.Slate, snapshot.Time);
            }

            SetLive(true);
            Refresh();
        }

        internal void LoadSnapshot(int index)
        {
            if (index < 0 || index >= m_Snapshots.Count)
            {
                return;
            }

            LoadSnapshot(m_Snapshots[index]);
        }

        internal void LoadSnapshot(Snapshot snapshot)
        {
            if (IsRecording())
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
            if (IsRecording() || index < 0 || index >= m_Snapshots.Count)
            {
                return;
            }

            m_Snapshots.RemoveAt(index);

            SendSnapshots();
        }

        void Awake()
        {
            m_FocusPlaneRenderer = GetComponent<FocusPlaneRenderer>();
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_MeshIntersectionTracker.Initialize();
            m_Raycaster = RaycasterFactory.Create();

            InitializeDriver();

            s_Instances.Add(this);
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            s_Instances.Remove(this);

            RaycasterFactory.Dispose(m_Raycaster);
            m_MeshIntersectionTracker.Dispose();

            base.OnDisable();

            Refresh();
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

            takeBuilder.CreateAnimationTrack("Virtual Camera", m_Actor.Animator, m_Recorder.Bake(), metadata);
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

            if (m_Settings.FocusMode != FocusMode.Clear)
                UpdateFocusRig(true);

            SendSettings();
        }

        /// <inheritdoc/>
        [Obsolete("Use LiveUpdate instead")]
        public override void BuildLiveLink(PlayableGraph graph) {}

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
            SendSnapshots();
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

            var camera = GetCamera();

            if (m_Settings.FocusMode == FocusMode.ReticleAF || m_Settings.FocusMode == FocusMode.TrackingAF)
            {
                UpdateFocusRig(false);
            }

            if (IsRecording())
            {
                RecordSmoothDampedLens();
            }

            UpdateInterpolatedLens(Time.unscaledDeltaTime);

            m_FocusPlaneRenderer.SetCamera(camera);
            m_VideoServer.Camera = camera;
            m_VideoServer.Update();

            UpdateClient();
        }

        /// <inheritdoc/>
        public override void LiveUpdate()
        {
            if (m_Actor == null)
            {
                return;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                m_Actor.LocalPosition = m_Rig.Pose.position;
                m_Actor.LocalPositionEnabled = true;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                m_Actor.LocalEulerAngles = m_Rig.Pose.rotation.eulerAngles;
                m_Actor.LocalEulerAnglesEnabled = true;
            }

            var lens = m_Actor.Lens;
            var lensIntrinsics = m_Actor.LensIntrinsics;

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
            {
                lens.FocalLength = m_InterpolatedLens.FocalLength;
                lensIntrinsics.FocalLengthRange = m_LensIntrinsics.FocalLengthRange;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                m_Actor.DepthOfFieldEnabled = IsDepthOfFieldEnabled();

                lens.FocusDistance = m_InterpolatedLens.FocusDistance;
                lensIntrinsics.CloseFocusDistance = m_LensIntrinsics.CloseFocusDistance;
            }

            if (m_Channels.HasFlag(VirtualCameraChannelFlags.Aperture))
            {
                lens.Aperture = m_InterpolatedLens.Aperture;
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

        void RecordSmoothDampedLens()
        {
            var currentTime = (float)GetTakeRecorder().GetPreviewTime();

            m_FocusDistanceSampler.SmoothTime = m_Settings.FocusDistanceDamping;
            m_FocalLengthSampler.SmoothTime = m_Settings.FocalLengthDamping;
            m_ApertureSampler.SmoothTime = m_Settings.ApertureDamping;
            m_FocusDistanceSampler.Time = currentTime;
            m_FocalLengthSampler.Time = currentTime;
            m_ApertureSampler.Time = currentTime;

            while (m_FocusDistanceSampler.MoveNext())
            {
                var keyFrame = m_FocusDistanceSampler.Current;
                m_Recorder.Time = keyFrame.time;
                m_Recorder.RecordFocusDistance(keyFrame.value);
            }

            while (m_FocalLengthSampler.MoveNext())
            {
                var keyFrame = m_FocalLengthSampler.Current;
                m_Recorder.Time = keyFrame.time;
                m_Recorder.RecordFocalLength(keyFrame.value);
            }

            while (m_ApertureSampler.MoveNext())
            {
                var keyFrame = m_ApertureSampler.Current;
                m_Recorder.Time = keyFrame.time;
                m_Recorder.RecordAperture(keyFrame.value);
            }
        }

        void UpdateInterpolatedLens(float deltaTime)
        {
            m_InterpolatedLens.FocusDistance = Mathf.SmoothDamp(
                m_InterpolatedLens.FocusDistance, m_Lens.FocusDistance,
                ref m_FocusDistanceDampingVelocity, m_Settings.FocusDistanceDamping, k_MaxSpeed, deltaTime);

            m_InterpolatedLens.FocalLength = Mathf.SmoothDamp(
                m_InterpolatedLens.FocalLength, m_Lens.FocalLength,
                ref m_FocalLengthDampingVelocity, m_Settings.FocalLengthDamping, k_MaxSpeed, deltaTime);

            m_InterpolatedLens.Aperture = Mathf.SmoothDamp(
                m_InterpolatedLens.Aperture, m_Lens.Aperture,
                ref m_ApertureDampingVelocity, m_Settings.ApertureDamping, k_MaxSpeed, deltaTime);
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

        void UpdateTimestampTracker()
        {
            var time = (float)GetTakeRecorder().GetPreviewTime();

            m_TimestampTracker.Time = time;
            m_FocusDistanceSampler.Time = time;
            m_FocalLengthSampler.Time = time;
            m_ApertureSampler.Time = time;
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

            m_InterpolatedLens = m_Lens;

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
                m_LensMetadata = m_Lens;
                m_InterpolatedLens = m_Lens;

                m_TimestampTracker.Reset();

                m_FocusDistanceSampler.Reset();
                m_FocalLengthSampler.Reset();
                m_ApertureSampler.Reset();

                m_FocusDistanceSampler.SmoothTime = m_Settings.FocusDistanceDamping;
                m_FocalLengthSampler.SmoothTime = m_Settings.FocalLengthDamping;
                m_ApertureSampler.SmoothTime = m_Settings.ApertureDamping;

                var frameRate = GetTakeRecorder().FrameRate;

                m_FocusDistanceSampler.FrameRate = frameRate;
                m_FocalLengthSampler.FrameRate = frameRate;
                m_ApertureSampler.FrameRate = frameRate;

                m_Recorder.FrameRate = frameRate;
                m_Recorder.Clear();

                UpdateTimestampTracker();

                m_FocusDistanceSampler.Initialize(m_Lens.FocusDistance);
                m_FocalLengthSampler.Initialize(m_Lens.FocalLength);
                m_ApertureSampler.Initialize(m_Lens.Aperture);

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
            m_TimestampTracker.SetTimestamp(sample.Timestamp);

            var settings = GetRigSettings();
            var frameInterval = 0d;
            var takeRecorder = GetTakeRecorder();
            var deltaTime = Mathf.Max(0f, sample.Timestamp - m_LastJoysticksTimeStamp);

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

            m_LastJoysticksTimeStamp = sample.Timestamp;

            Refresh();
        }

        void OnPoseSampleReceived(PoseSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.Timestamp);
            var deltaTime = sample.Timestamp - m_LastPoseTimeStamp;

            // If true the state will refresh the last input and the rebase offset
            if (m_PoseRigNeedsInitialize)
            {
                m_Rig.LastInput = sample.Pose;
                m_Rig.RebaseOffset = Quaternion.Euler(0f, sample.Pose.rotation.eulerAngles.y - m_Rig.LocalPose.rotation.eulerAngles.y, 0f);
                m_LastPoseTimeStamp = sample.Timestamp;
                m_PoseRigNeedsInitialize = false;
            }

            var settings = GetRigSettings();

            if (!(m_Driver is ICustomDamping))
                sample.Pose = VirtualCameraDamping.Calculate(m_Rig.LastInput, sample.Pose, m_Settings.Damping, deltaTime);

            m_Rig.Update(sample.Pose, settings);

            if (IsRecording())
            {
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.RecordPosition(m_Rig.Pose.position);
                m_Recorder.RecordRotation(m_Rig.Pose.rotation);
            }

            m_LastPoseTimeStamp = sample.Timestamp;

            Refresh();
        }

        void OnFocalLengthSampleReceived(FocalLengthSample sample)
        {
            m_Lens.FocalLength = sample.FocalLength;
            m_Lens.Validate(m_LensIntrinsics);

            if (IsRecording())
            {
                m_FocalLengthSampler.Add(new Keyframe(sample.Timestamp, sample.FocalLength), true);
            }

            Refresh();
        }

        void OnFocusDistanceSampleReceived(FocusDistanceSample sample)
        {
            m_Lens.FocusDistance = sample.FocusDistance;
            m_Lens.Validate(m_LensIntrinsics);

            if (IsRecording())
            {
                m_FocusDistanceSampler.Add(new Keyframe(sample.Timestamp, sample.FocusDistance), true);
            }

            Refresh();
        }

        void OnApertureSampleReceived(ApertureSample sample)
        {
            m_Lens.Aperture = sample.Aperture;
            m_Lens.Validate(m_LensIntrinsics);

            if (IsRecording())
            {
                m_ApertureSampler.Add(new Keyframe(sample.Timestamp, sample.Aperture), true);
            }

            Refresh();
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

            if (m_Recorder.Channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                m_Recorder.RecordLocalPositionEnabled(true);
            }

            if (m_Recorder.Channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                m_Recorder.RecordLocalEulerAnglesEnabled(true);
            }

            RecordSmoothDampedLens();
        }

        void UpdateDamping()
        {
            if (m_Driver is ICustomDamping customDamping)
                customDamping.SetDamping(m_Settings.Damping);
        }

        void UpdateFocusRig(bool reticlePositionChanged)
        {
            // Depth of field activation may change as a result of a focus distance update.
            var prevIsDepthOfFieldEnabled = IsDepthOfFieldEnabled();

            var distance = m_Lens.FocusDistance;

            UpdateAutoFocusDistance(ref distance, reticlePositionChanged);

            // In principle DoF activation should not change if the focus distance has not changed.
            // But writing it this way is more robust and makes an eventual issue easier to diagnose.
            if (IsRecording())
            {
                var isDepthOfFieldEnabled = IsDepthOfFieldEnabled();
                if (isDepthOfFieldEnabled != prevIsDepthOfFieldEnabled)
                {
                    m_Recorder.RecordEnableDepthOfField(isDepthOfFieldEnabled);
                }
            }

            if (m_Lens.FocusDistance != distance)
            {
                m_Lens.FocusDistance = distance;
                m_Lens.Validate(m_LensIntrinsics);

                if (IsRecording())
                {
                    m_FocusDistanceSampler.Add(new Keyframe((float)GetTakeRecorder().GetPreviewTime(), distance));
                }

                SendLens();
                Refresh();
            }
        }

        void UpdateAutoFocusDistance(ref float distance, bool reticlePositionChanged)
        {
            switch (m_Settings.FocusMode)
            {
                case FocusMode.Manual:
                case FocusMode.ReticleAF:
                {
                    if (m_Settings.FocusMode == FocusMode.Manual && !reticlePositionChanged)
                        throw new InvalidOperationException(
                            $"{nameof(UpdateFocusRig)} was invoked while focusMode is set to [{FocusMode.Manual}], " +
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
                        }
                    }

                    if (m_MeshIntersectionTracker.TryUpdate(out var worldPosition))
                    {
                        var cameraTransform = m_VideoServer.Camera.transform;
                        var hitVector = worldPosition - cameraTransform.position;
                        var depthVector = Vector3.Project(hitVector, cameraTransform.forward);
                        distance = depthVector.magnitude + m_Settings.FocusDistanceOffset;
                    }

                    break;
                }
                case FocusMode.Clear:
                    throw new InvalidOperationException(
                        $"UpdateFocusRig was invoked while focusMode is set to [{FocusMode.Clear}]");
            }
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

        void SendSnapshots()
        {
            if (TryGetInternalClient(out var client))
            {
                var descriptor = SnapshotListDescriptor.Create(m_Snapshots);

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

        static bool Approximately(float a, float b) => Mathf.Abs(a - b) < k_Epsilon;
    }
}
