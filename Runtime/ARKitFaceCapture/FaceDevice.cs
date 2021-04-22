using Unity.LiveCapture.CompanionApp;
using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A device used to control face animation capture and playback.
    /// </summary>
    /// <remarks>
    /// The face capture data is in the format of Apple's ARKit face tracking.
    /// </remarks>
    /// <seealso href="https://developer.apple.com/documentation/arkit/tracking_and_visualizing_faces"/>
    [ExcludeFromPreset]
    [CreateDeviceMenuItemAttribute("ARKit Face Device")]
    [AddComponentMenu("Live Capture/ARKit Face Capture/ARKit Face Device")]
    public class FaceDevice : CompanionAppDevice<IFaceClient>
    {
        [SerializeField]
        FaceActor m_Actor;
        [SerializeField, HideInInspector]
        FaceLiveLink m_LiveLink = new FaceLiveLink();
        [SerializeField, HideInInspector]
        FacePose m_Pose = FacePose.identity;
        FaceDeviceRecorder m_Recorder = new FaceDeviceRecorder();
        TimestampTracker m_TimestampTracker = new TimestampTracker();

        /// <summary>
        /// Gets the <see cref="FaceActor"/> currently assigned to this device.
        /// </summary>
        /// <returns>The assigned actor, or null if none is assigned.</returns>
        public FaceActor actor
        {
            get => m_Actor;
            set
            {
                if (m_Actor != value)
                {
                    m_Actor = value;
                    m_LiveLink.SetAnimator(null);

                    if (m_Actor != null)
                    {
                        m_Pose = m_Actor.facePose;
                    }

                    Refresh();
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_LiveLink.Initialize();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();

            m_LiveLink.SetAnimator(null);
            m_LiveLink.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnRecordingChanged()
        {
            if (IsRecording())
            {
                m_TimestampTracker.Reset();
                m_Recorder.frameRate = GetTakeRecorder().frameRate;
                m_Recorder.Clear();

                UpdateTimestampTracker();
            }
        }

        /// <inheritdoc />
        protected override void OnClientAssigned()
        {
            var client = GetClient();

            client.facePoseSampleReceived += OnFacePoseSampleReceived;
        }

        /// <inheritdoc />
        protected override void OnClientUnassigned()
        {
            var client = GetClient();

            client.facePoseSampleReceived -= OnFacePoseSampleReceived;
        }

        /// <inheritdoc/>
        public override void Write(ITakeBuilder takeBuilder)
        {
            if (m_Actor == null || m_Recorder.IsEmpty())
            {
                return;
            }

            takeBuilder.CreateAnimationTrack("Face", m_Actor.animator, m_Recorder.Bake());
        }

        /// <inheritdoc/>
        public override void BuildLiveLink(PlayableGraph graph)
        {
            m_LiveLink.Build(graph);
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            UpdateTimestampTracker();
            UpdateLiveLink();
            UpdateClient();
        }

        void UpdateLiveLink()
        {
            var animator = default(Animator);

            if (m_Actor != null)
            {
                animator = m_Actor.animator;
            }

            m_LiveLink.SetAnimator(animator);
            m_LiveLink.SetActive(IsLive());
            m_LiveLink.pose = m_Pose;
            m_LiveLink.Update();
        }

        void OnFacePoseSampleReceived(FaceSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.timestamp);
            m_Pose = sample.facePose;

            if (IsRecording())
            {
                m_Recorder.channels = m_LiveLink.channels;
                m_Recorder.time = m_TimestampTracker.localTime;
                m_Recorder.Record(ref m_Pose);
            }

            Refresh();
        }

        void UpdateTimestampTracker()
        {
            var takeRecorder = GetTakeRecorder();
            var time = (float)takeRecorder.slate.time;

            m_TimestampTracker.time = time;
        }
    }
}
