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
        FacePose m_Pose = FacePose.Identity;
        FaceDeviceRecorder m_Recorder = new FaceDeviceRecorder();
        TimestampTracker m_TimestampTracker = new TimestampTracker();

        /// <summary>
        /// Gets the <see cref="FaceActor"/> currently assigned to this device.
        /// </summary>
        /// <returns>The assigned actor, or null if none is assigned.</returns>
        public FaceActor Actor
        {
            get => m_Actor;
            set
            {
                if (m_Actor != value)
                {
                    m_Actor = value;
                    m_LiveLink.SetAnimator(null);
                    Refresh();
                }
            }
        }

        bool TryGetInternalClient(out IFaceClientInternal client)
        {
            client = GetClient() as IFaceClientInternal;

            return client != null;
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
                m_Recorder.FrameRate = GetTakeRecorder().FrameRate;
                m_Recorder.Clear();

                UpdateTimestampTracker();
            }
        }

        /// <inheritdoc />
        protected override void OnClientAssigned()
        {
            if (TryGetInternalClient(out var client))
            {
                client.FacePoseSampleReceived += OnFacePoseSampleReceived;
            }
        }

        /// <inheritdoc />
        protected override void OnClientUnassigned()
        {
            if (TryGetInternalClient(out var client))
            {
                client.FacePoseSampleReceived -= OnFacePoseSampleReceived;
            }
        }

        /// <inheritdoc/>
        public override void Write(ITakeBuilder takeBuilder)
        {
            if (m_Actor == null || m_Recorder.IsEmpty())
            {
                return;
            }

            takeBuilder.CreateAnimationTrack("Face", m_Actor.Animator, m_Recorder.Bake());
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
                animator = m_Actor.Animator;
            }

            m_LiveLink.SetAnimator(animator);
            m_LiveLink.SetActive(IsLive());
            m_LiveLink.Pose = m_Pose;
            m_LiveLink.Update();
        }

        void OnFacePoseSampleReceived(FaceSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.Timestamp);
            m_Pose = sample.FacePose;

            if (IsRecording())
            {
                m_Recorder.Channels = m_LiveLink.Channels;
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.Record(ref m_Pose);
            }

            Refresh();
        }

        void UpdateTimestampTracker()
        {
            var time = (float)GetTakeRecorder().GetPreviewTime();

            m_TimestampTracker.Time = time;
        }
    }
}
