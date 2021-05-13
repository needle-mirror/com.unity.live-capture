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
    [RequireComponent(typeof(FocusPlaneRenderer))]
    [CreateDeviceMenuItemAttribute("Virtual Camera Device")]
    [AddComponentMenu("Live Capture/Virtual Camera/Virtual Camera Device")]
    public class VirtualCameraDevice : CompanionAppDevice<IVirtualCameraClient>
    {
        static List<VirtualCameraDevice> s_Instances = new List<VirtualCameraDevice>();
        internal static IEnumerable<VirtualCameraDevice> instances => s_Instances;

        [SerializeField]
        internal LensAsset m_DefaultLensAsset;
        [SerializeField]
        VirtualCameraActor m_Actor;
        [SerializeField]
        VirtualCameraLiveLink m_LiveLink = new VirtualCameraLiveLink();
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

        float m_LastPoseTimeStamp;
        float m_LastJoysticksTimeStamp;
        bool m_RigNeedsInitialize;
        IRaycaster m_Raycaster;
        ICameraDriver m_Driver;
        FocusPlaneRenderer m_FocusPlaneRenderer;
        VirtualCameraRecorder m_Recorder = new VirtualCameraRecorder();
        TimestampTracker m_TimestampTracker = new TimestampTracker();
        MeshIntersectionTracker m_MeshIntersectionTracker = new MeshIntersectionTracker();
        Lens m_LensMetadata;
        LensAsset m_LastLensAsset;
        bool m_LastScreenAFRaycastIsValid;
        IScreenshotImpl m_ScreenshotImpl = new ScreenshotImpl();

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
                    m_LiveLink.SetAnimator(null);

                    InitializeLocalPose();
                    InitializeDriver();
                    Refresh();
                    UpdateOverlaysIfNeeded(m_Settings);
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
                    m_Rig.Refresh(GetRigSettings());
                    SetFocusMode(m_Settings.FocusMode);
                    UpdateOverlaysIfNeeded(m_Settings);
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
                m_Lens.Validate(m_LensIntrinsics);
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
            }
        }

        /// <inheritdoc/>
        protected virtual void OnValidate()
        {
            InitializeDriver();
            ValidateLensIntrinsics();

            m_Settings.Validate();
            m_CameraBody.Validate();
            m_Rig.Refresh(GetRigSettings());
            UpdateOverlaysIfNeeded(m_Settings);
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
                frameLines.enabled = settings.GateMask;
                frameLines.ShowAspectRatio = settings.FrameLines;
                frameLines.ShowCenterMarker = settings.CenterMarker;
            }
        }

        void ValidateLensIntrinsics()
        {
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
            if (IsRecording() || !IsLive())
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
            UpdateOverlaysIfNeeded(m_Settings);
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

            m_LiveLink.SetAnimator(null);

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
                CropAspect = m_Settings.CropAspect
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

        void SetFocalLength(float value)
        {
            if (m_Lens.FocalLength != value)
            {
                m_Lens.FocalLength = value;
                m_Lens.Validate(m_LensIntrinsics);

                Refresh();
            }
        }

        void SetFocusDistance(float value)
        {
            if (m_Lens.FocusDistance != value)
            {
                m_Lens.FocusDistance = value;
                m_Lens.Validate(m_LensIntrinsics);

                Refresh();
            }
        }

        void SetAperture(float value)
        {
            if (m_Lens.Aperture != value)
            {
                m_Lens.Aperture = value;
                m_Lens.Validate(m_LensIntrinsics);

                Refresh();
            }
        }

        internal void SetReticlePosition(Vector2 reticlePosition)
        {
            m_Settings.ReticlePosition = reticlePosition;

            if (m_Settings.FocusMode != FocusMode.Clear)
                UpdateFocusRig(true);

            SendSettings();
        }

        /// <inheritdoc/>
        public override void BuildLiveLink(PlayableGraph graph)
        {
            m_LiveLink.Build(graph);
        }

        void UpdateLiveLink()
        {
            var changed = m_LiveLink.Position != m_Rig.Pose.position
                || m_LiveLink.Rotation != m_Rig.Pose.rotation
                || m_LiveLink.Lens != m_Lens
                || m_LiveLink.LensIntrinsics != m_LensIntrinsics
                || m_LiveLink.DepthOfFieldEnabled != IsDepthOfFieldEnabled()
                || m_LiveLink.CropAspect != m_Settings.CropAspect;

            var animator = default(Animator);

            if (m_Actor != null)
            {
                animator = m_Actor.Animator;
            }

            m_LiveLink.SetAnimator(animator);
            m_LiveLink.SetActive(IsLive());
            m_LiveLink.Position = m_Rig.Pose.position;
            m_LiveLink.Rotation = m_Rig.Pose.rotation;
            m_LiveLink.Lens = m_Lens;
            m_LiveLink.LensIntrinsics = m_LensIntrinsics;
            m_LiveLink.CameraBody = m_CameraBody;
            m_LiveLink.DepthOfFieldEnabled = IsDepthOfFieldEnabled();
            m_LiveLink.CropAspect = m_Settings.CropAspect;
            m_LiveLink.Update();

            UpdateDamping();

            if (changed)
            {
                Refresh();
            }
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
            SendSnapshots();
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            UpdateTimestampTracker();
            UpdateRecorder();

            var camera = GetCamera();

            if (m_Settings.FocusMode == FocusMode.ReticleAF || m_Settings.FocusMode == FocusMode.TrackingAF)
            {
                UpdateFocusRig(false);
            }

            UpdateLiveLink();

            m_FocusPlaneRenderer.SetCamera(camera);
            m_VideoServer.Camera = camera;
            m_VideoServer.Update();

            UpdateClient();
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

            m_Recorder.Channels = m_LiveLink.Channels;
            m_Recorder.Time = time;
        }

        void UpdateTimestampTracker()
        {
            var time = (float)GetTakeRecorder().GetPreviewTime();

            m_TimestampTracker.Time = time;
        }

        /// <inheritdoc/>
        protected override void OnClientAssigned()
        {
            Register();

            m_LastLensAsset = null;
            m_RigNeedsInitialize = true;
            InitializeLocalPose();
            StartVideoServer();

            if (TryGetInternalClient(out var client))
            {
                client.Initialize();
            }
        }

        /// <inheritdoc/>
        protected override void OnClientUnassigned()
        {
            var client = GetClient();

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
                client.SettingsReceived += OnSettingsReceived;
                client.ReticlePositionReceived += OnReticlePositionReceived;
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
                client.SettingsReceived -= OnSettingsReceived;
                client.ReticlePositionReceived -= OnReticlePositionReceived;
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
                m_TimestampTracker.Reset();
                m_Recorder.FrameRate = GetTakeRecorder().FrameRate;
                m_Recorder.Clear();

                UpdateTimestampTracker();
                RecordCurrentValues();
            }
        }

        internal void InitializeLocalPose()
        {
            if (m_Actor != null)
            {
                m_Rig.WorldToLocal(new Pose(m_Actor.transform.localPosition, m_Actor.transform.localRotation));
            }
        }

        void OnChannelFlagsReceived(VirtualCameraChannelFlags channelFlags)
        {
            m_LiveLink.Channels = channelFlags;
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
        }

        void OnPoseSampleReceived(PoseSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.Timestamp);
            var deltaTime = sample.Timestamp - m_LastPoseTimeStamp;

            // If true the state will refresh the last input and the rebase offset
            if (m_RigNeedsInitialize)
            {
                m_Rig.LastInput = sample.Pose;
                m_Rig.RebaseOffset = Quaternion.Euler(0f, sample.Pose.rotation.eulerAngles.y - m_Rig.LocalPose.rotation.eulerAngles.y, 0f);
                m_LastPoseTimeStamp = sample.Timestamp;
                m_RigNeedsInitialize = false;
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
            m_TimestampTracker.SetTimestamp(sample.Timestamp);

            SetFocalLength(sample.FocalLength);

            if (IsRecording())
            {
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.RecordFocalLength(sample.FocalLength);
            }
        }

        void OnFocusDistanceSampleReceived(FocusDistanceSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.Timestamp);

            SetFocusDistance(sample.FocusDistance);

            if (IsRecording())
            {
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.RecordFocusDistance(sample.FocusDistance);
            }
        }

        void OnApertureSampleReceived(ApertureSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.Timestamp);

            SetAperture(sample.Aperture);

            if (IsRecording())
            {
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.RecordAperture(sample.Aperture);
            }
        }

        void OnSettingsReceived(Settings value)
        {
            Settings = value;
        }

        void OnReticlePositionReceived(Vector2 reticlePosition)
        {
            SetReticlePosition(reticlePosition);
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

            this.LensAsset = lensAsset;
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
            m_Recorder.RecordFocalLength(m_Lens.FocalLength);
            m_Recorder.RecordFocusDistance(m_Lens.FocusDistance);
            m_Recorder.RecordAperture(m_Lens.Aperture);
            m_Recorder.RecordEnableDepthOfField(IsDepthOfFieldEnabled());
            m_Recorder.RecordLensIntrinsics(m_LensIntrinsics);
            m_Recorder.RecordCropAspect(m_Settings.CropAspect);
        }

        void UpdateDamping()
        {
            if (m_Driver is ICustomDamping customDamping)
                customDamping.SetDamping(m_Settings.Damping);
        }

        void UpdateFocusRig(bool reticlePositionChanged)
        {
            var distance = m_Lens.FocusDistance;
            var prevIsDepthOfFieldEnabled = IsDepthOfFieldEnabled();

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

            if (m_Lens.FocusDistance != distance)
            {
                SetFocusDistance(distance);

                if (IsRecording())
                {
                    m_Recorder.RecordFocusDistance(distance);
                    var isDepthOfFieldEnabled = IsDepthOfFieldEnabled();
                    if (isDepthOfFieldEnabled != prevIsDepthOfFieldEnabled)
                    {
                        m_Recorder.RecordEnableDepthOfField(isDepthOfFieldEnabled);
                    }
                }

                SendLens();
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
                client.SendChannelFlags(m_LiveLink.Channels);
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
            if (m_LensAsset == m_LastLensAsset)
            {
                return;
            }

            if (TryGetInternalClient(out var client))
            {
                var descriptor = LensKitDescriptor.Create(m_LensAsset);

                client.SendLensKitDescriptor(descriptor);

                m_LastLensAsset = m_LensAsset;
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
    }
}
