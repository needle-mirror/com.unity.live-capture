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
    /// A <see cref="VirtualCameraActor"/> and a <see cref="VirtualCameraClient"/> must be assigned before the device
    /// is useful. The actor is needed to store live or evaluated playback state and affect the scene.
    /// </remarks>
    [RequireComponent(typeof(FilmFormat))]
    [RequireComponent(typeof(FocusPlane))]
    [ExcludeFromPreset]
    [CreateDeviceMenuItemAttribute("Virtual Camera Device")]
    [AddComponentMenu("Live Capture/Virtual Camera/Virtual Camera Device")]
    public class VirtualCameraDevice : CompanionAppDevice<IVirtualCameraClient>
    {
        static List<VirtualCameraDevice> s_Instances = new List<VirtualCameraDevice>();
        internal static IEnumerable<VirtualCameraDevice> instances => s_Instances;

        [SerializeField]
        VirtualCameraActor m_Actor;
        [SerializeField]
        VirtualCameraLiveLink m_LiveLink = new VirtualCameraLiveLink();
        [SerializeField]
        Lens m_Lens = Lens.defaultParams;
        [SerializeField]
        CameraBody m_CameraBody = CameraBody.defaultParams;
        [SerializeField]
        VirtualCameraRigState m_Rig = VirtualCameraRigState.identity;
        [SerializeField]
        CameraState m_Settings = CameraState.defaultData;
        [SerializeField]
        VideoServer m_VideoServer = new VideoServer();
        [SerializeField]
        LensPreset m_LensPreset;

        float m_LastPoseTimeStamp;
        bool m_RigNeedsInitialize;
        IRaycaster m_Raycaster;
        ICameraDriver m_Driver;
        FilmFormat m_FilmFormat;
        FocusPlane m_FocusPlane;
        VirtualCameraRecorder m_Recorder = new VirtualCameraRecorder();
        TimestampTracker m_TimestampTracker = new TimestampTracker();
        MeshIntersectionTracker m_MeshIntersectionTracker = new MeshIntersectionTracker();
        Lens m_LensMetadata;

        internal VirtualCameraRecorder recorder => m_Recorder;

        /// <summary>
        /// Gets the <see cref="VirtualCameraActor"/> currently assigned to this device.
        /// </summary>
        /// <returns>The assigned actor, or null if none is assigned.</returns>
        public VirtualCameraActor actor
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
                }
            }
        }

        /// <summary>
        /// The position and rotation of the current device in world coordinates.
        /// </summary>
        public Pose pose => m_Rig.pose;

        /// <summary>
        /// The position and rotation of the world's origin.
        /// </summary>
        public Pose origin => m_Rig.origin;

        /// <summary>
        /// The <see cref="CameraState"/> of the current device.
        /// </summary>
        public CameraState cameraState => m_Settings;

        /// <summary>
        /// The <see cref="Lens"/> of the current device.
        /// </summary>
        public Lens lens
        {
            get => m_Lens;
            internal set => m_Lens = value;
        }

        /// <summary>
        /// The <see cref="LensPreset"/> of the current device.
        /// </summary>
        public LensPreset lensPreset => m_LensPreset;

        /// <summary>
        /// The <see cref="CameraBody"/> of the current device.
        /// </summary>
        public CameraBody cameraBody => m_CameraBody;

        void InitializeDriver()
        {
            m_Driver = null;

            if (m_Actor != null)
            {
                m_Driver = m_Actor.GetComponent(typeof(ICameraDriver)) as ICameraDriver;
            }
        }

        /// <inheritdoc/>
        protected virtual void OnValidate()
        {
            InitializeDriver();

            m_Lens.Validate();
            m_CameraBody.Validate();
            m_Rig.Refresh(GetVirtualCameraRigSettings());
        }

        void Awake()
        {
            m_FilmFormat = GetComponent<FilmFormat>();
            m_FocusPlane = GetComponent<FocusPlane>();
            m_FocusPlane.enabled = false;
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

            var metadata = new VirtualCameraTrackMetadata();
            metadata.channels = m_Recorder.channels;
            metadata.lens = m_LensMetadata;
            metadata.cameraBody = m_CameraBody;

            takeBuilder.CreateAnimationTrack("Virtual Camera", m_Actor.animator, m_Recorder.Bake(), metadata);
        }

        /// <summary>
        /// Gets the video server used to stream the shot from to this virtual camera.
        /// </summary>
        /// <returns>The currently assigned video server, or null if none is assigned.</returns>
        internal VideoServer GetVideoServer()
        {
            return m_VideoServer;
        }

        /// <summary>
        /// Sets the <see cref="CameraState"> parameters of the device.
        /// </summary>
        internal void SetCameraState(CameraState state)
        {
            SetFocusMode(state.focusMode);

            m_Settings = state;
            m_Rig.Refresh(GetVirtualCameraRigSettings());

            Refresh();
        }

        void SetFocusMode(FocusMode focusMode)
        {
            var lastDepthOfFieldEnabled = IsDepthOfFieldEnabled();
            m_Settings.focusMode = focusMode;
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
            if (m_Lens.focalLength != value)
            {
                m_Lens.focalLength = value;
                m_Lens.ValidateFocalLength();

                Refresh();
            }
        }

        void SetFocusDistance(float value)
        {
            if (m_Lens.focusDistance != value)
            {
                m_Lens.focusDistance = value;
                m_Lens.ValidateFocusDistance();

                Refresh();
            }
        }

        void SetAperture(float value)
        {
            if (m_Lens.aperture != value)
            {
                m_Lens.aperture = value;
                m_Lens.ValidateAperture();

                Refresh();
            }
        }

        internal void SetReticlePosition(Vector2 reticlePosition)
        {
            m_Settings.reticlePosition = reticlePosition;

            if (m_Settings.focusMode != FocusMode.Disabled)
                UpdateFocusRig(true);

            SendCameraState();
        }

        /// <inheritdoc/>
        public override void BuildLiveLink(PlayableGraph graph)
        {
            m_LiveLink.Build(graph);
        }

        void UpdateLiveLink()
        {
            var changed = m_LiveLink.position != m_Rig.pose.position ||
                m_LiveLink.rotation != m_Rig.pose.rotation ||
                m_LiveLink.lens != m_Lens ||
                m_LiveLink.depthOfFieldEnabled != IsDepthOfFieldEnabled();

            var animator = default(Animator);

            if (m_Actor != null)
            {
                animator = m_Actor.animator;
            }

            m_LiveLink.SetAnimator(animator);
            m_LiveLink.SetActive(IsLive());
            m_LiveLink.position = m_Rig.pose.position;
            m_LiveLink.rotation = m_Rig.pose.rotation;
            m_LiveLink.lens = m_Lens;
            m_LiveLink.cameraBody = m_CameraBody;
            m_LiveLink.depthOfFieldEnabled = IsDepthOfFieldEnabled();
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

            SendCameraLens();
            SendCameraBody();
            SendCameraState();
            SendVideoStreamState();
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            UpdateTimestampTracker();
            UpdateRecorder();

            var camera = GetCamera();

            if (m_Settings.focusMode == FocusMode.Auto || m_Settings.focusMode == FocusMode.Spatial)
            {
                UpdateFocusRig(false);
            }

            UpdateLiveLink();

            m_FilmFormat.SetCamera(camera);
            m_FocusPlane.SetCamera(camera);
            m_VideoServer.camera = camera;
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
            var takeRecorder = GetTakeRecorder();
            var time = (float)takeRecorder.slate.time;

            m_Recorder.channels = m_LiveLink.channels;
            m_Recorder.time = time;
        }

        void UpdateTimestampTracker()
        {
            var takeRecorder = GetTakeRecorder();
            var time = (float)takeRecorder.slate.time;

            m_TimestampTracker.time = time;
        }

        /// <inheritdoc/>
        protected override void OnClientAssigned()
        {
            var client = GetClient();

            client.poseSampleReceived += OnPoseSampleReceived;
            client.focalLengthSampleReceived += OnFocalLengthSampleReceived;
            client.focusDistanceSampleReceived += OnFocusDistanceSampleReceived;
            client.apertureSampleReceived += OnApertureSampleReceived;
            client.cameraStateReceived += OnCameraStateReceived;
            client.reticlePositionReceived += OnReticlePositionReceived;
            client.setPoseToOrigin += OnSetPoseToOrigin;

            m_RigNeedsInitialize = true;
            InitializeLocalPose();
            StartVideoServer();

            client.Initialize();
        }

        /// <inheritdoc/>
        protected override void OnClientUnassigned()
        {
            var client = GetClient();

            client.poseSampleReceived -= OnPoseSampleReceived;
            client.focalLengthSampleReceived -= OnFocalLengthSampleReceived;
            client.focusDistanceSampleReceived -= OnFocusDistanceSampleReceived;
            client.apertureSampleReceived -= OnApertureSampleReceived;
            client.cameraStateReceived -= OnCameraStateReceived;
            client.reticlePositionReceived -= OnReticlePositionReceived;
            client.setPoseToOrigin -= OnSetPoseToOrigin;

            StopVideoServer();
        }

        /// <inheritdoc/>
        protected override void OnRecordingChanged()
        {
            if (IsRecording())
            {
                m_LensMetadata = m_Lens;
                m_TimestampTracker.Reset();
                m_Recorder.frameRate = GetTakeRecorder().frameRate;
                m_Recorder.Clear();

                UpdateTimestampTracker();
                RecordCurrentValues();
            }
        }

        void InitializeLocalPose()
        {
            if (m_Actor != null)
            {
                m_Rig.WorldToLocal(new Pose(m_Actor.transform.position, m_Actor.transform.rotation));
            }
        }

        void OnPoseSampleReceived(PoseSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.timestamp);

            var deltaTime = sample.timestamp - m_LastPoseTimeStamp;

            // If true the state will refresh the last input and the rebase offset
            if (m_RigNeedsInitialize)
            {
                m_Rig.lastInput = sample.pose;
                m_Rig.rebaseOffset = Quaternion.Euler(0f, sample.pose.rotation.eulerAngles.y - m_Rig.localPose.rotation.eulerAngles.y, 0f);
                m_LastPoseTimeStamp = sample.timestamp;
                m_RigNeedsInitialize = false;
            }

            var settings = GetVirtualCameraRigSettings();

            if (!(m_Driver is ICustomDamping))
                sample.pose = VirtualCameraDamping.Calculate(m_Rig.lastInput, sample.pose, m_Settings.damping, deltaTime);

            m_Rig.Translate(sample.joystick, deltaTime, m_Settings.joystickSpeed, m_Settings.pedestalSpace, settings);
            m_Rig.Update(sample.pose, settings);

            if (IsRecording())
            {
                m_Recorder.time = m_TimestampTracker.localTime;
                m_Recorder.RecordPosition(m_Rig.pose.position);
                m_Recorder.RecordRotation(m_Rig.pose.rotation);
            }

            m_LastPoseTimeStamp = sample.timestamp;

            Refresh();
        }

        void OnFocalLengthSampleReceived(FocalLengthSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.timestamp);

            SetFocalLength(sample.focalLength);

            if (IsRecording())
            {
                m_Recorder.time = m_TimestampTracker.localTime;
                m_Recorder.RecordFocalLength(sample.focalLength);
            }
        }

        void OnFocusDistanceSampleReceived(FocusDistanceSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.timestamp);

            SetFocusDistance(sample.focusDistance);

            if (IsRecording())
            {
                m_Recorder.time = m_TimestampTracker.localTime;
                m_Recorder.RecordFocusDistance(sample.focusDistance);
            }
        }

        void OnApertureSampleReceived(ApertureSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.timestamp);

            SetAperture(sample.aperture);

            if (IsRecording())
            {
                m_Recorder.time = m_TimestampTracker.localTime;
                m_Recorder.RecordAperture(sample.aperture);
            }
        }

        void OnCameraStateReceived(CameraState state)
        {
            SetCameraState(state);
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

        void RecordCurrentValues()
        {
            UpdateRecorder();

            m_Recorder.RecordPosition(m_Rig.pose.position);
            m_Recorder.RecordRotation(m_Rig.pose.rotation);
            m_Recorder.RecordFocalLength(m_Lens.focalLength);
            m_Recorder.RecordFocusDistance(m_Lens.focusDistance);
            m_Recorder.RecordAperture(m_Lens.aperture);
            m_Recorder.RecordEnableDepthOfField(IsDepthOfFieldEnabled());
        }

        void UpdateDamping()
        {
            if (m_Driver is ICustomDamping customDamping)
                customDamping.SetDamping(m_Settings.damping);
        }

        void UpdateFocusRig(bool reticlePositionChanged)
        {
            var distance = 0f;
            var shouldUpdateDistance = false;
            switch (m_Settings.focusMode)
            {
                case FocusMode.Manual:
                case FocusMode.Auto:
                {
                    if (m_Settings.focusMode == FocusMode.Manual && !reticlePositionChanged)
                        throw new InvalidOperationException(
                            $"UpdateFocusRig was invoked while focusMode is set to [{FocusMode.Manual}], " +
                            $"despite [{nameof(reticlePositionChanged)}] being false.");

                    if (m_Raycaster.Raycast(m_VideoServer.camera, m_Settings.reticlePosition, out distance))
                        shouldUpdateDistance = true;
                    break;
                }
                case FocusMode.Spatial:
                {
                    if (reticlePositionChanged)
                    {
                        if (m_Raycaster.Raycast(m_VideoServer.camera, m_Settings.reticlePosition, out var ray, out var gameObject, out var hit))
                        {
                            m_MeshIntersectionTracker.TryTrack(gameObject, ray, hit.point);
                        }
                        else
                        {
                            m_MeshIntersectionTracker.Reset();
                        }
                    }

                    if (m_MeshIntersectionTracker.TryUpdate(out var worldPosition))
                    {
                        var cameraTransform = m_VideoServer.camera.transform;
                        var hitVector = worldPosition - cameraTransform.position;
                        var depthVector = Vector3.Project(hitVector, cameraTransform.forward);
                        distance = depthVector.magnitude;
                        shouldUpdateDistance = true;
                    }
                    break;
                }
                case FocusMode.Disabled:
                    throw new InvalidOperationException(
                        $"UpdateFocusRig was invoked while focusMode is set to [{FocusMode.Disabled}]");
            }

            if (shouldUpdateDistance && m_Lens.focusDistance != distance)
            {
                SetFocusDistance(distance);

                if (IsRecording())
                {
                    m_Recorder.RecordFocusDistance(distance);
                }

                SendCameraLens();
            }
        }

        bool IsDepthOfFieldEnabled()
        {
            switch (m_Settings.focusMode)
            {
                case FocusMode.Manual:
                case FocusMode.Auto:
                    return true;
                case FocusMode.Spatial:
                    return m_MeshIntersectionTracker.mode != MeshIntersectionTracker.Mode.None;
            }

            return false;
        }

        VirtualCameraRigSettings GetVirtualCameraRigSettings()
        {
            return new VirtualCameraRigSettings()
            {
                positionLock = m_Settings.positionLock,
                rotationLock = m_Settings.rotationLock,
                rebasing = m_Settings.rebasing,
                motionScale = m_Settings.motionScale,
                ergonomicTilt = -m_Settings.ergonomicTilt,
                zeroDutch = m_Settings.zeroDutch
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
            var client = GetClient();

            if (client != null)
            {
                m_VideoServer.baseResolution = client.screenResolution;
            }
        }

        void SendCameraLens()
        {
            var client = GetClient();

            if (client != null)
            {
                client.SendCameraLens(m_Lens);
            }
        }

        void SendCameraBody()
        {
            var client = GetClient();

            if (client != null)
            {
                client.SendCameraBody(m_CameraBody);
            }
        }

        void SendCameraState()
        {
            var client = GetClient();

            if (client != null)
            {
                client.SendCameraState(m_Settings);
            }
        }

        void SendVideoStreamState()
        {
            var client = GetClient();

            if (client != null)
            {
                client.SendVideoStreamState(new VideoStreamState
                {
                    isRunning = m_VideoServer.isRunning,
                    port = m_VideoServer.port,
                });
            }
        }
    }
}
