using System;
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
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "ref-component-arkit-face-device" + Documentation.endURL)]
    public class FaceDevice : CompanionAppDevice<IFaceClient>
    {
        [SerializeField]
        FaceActor m_Actor;
        [SerializeField, HideInInspector, EnumFlagButtonGroup(100f)]
        FaceChannelFlags m_Channels = FaceChannelFlags.All;
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
            set => m_Actor = value;
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
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();
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

        /// <inheritdoc/>
        protected override string GetAssetName()
        {
            return m_Actor != null ? m_Actor.name : name;
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
        [Obsolete("Use LiveUpdate instead")]
        public override void BuildLiveLink(PlayableGraph graph) {}

        /// <inheritdoc/>
        public override void LiveUpdate()
        {
            if (m_Actor == null)
            {
                return;
            }

            if (m_Channels.HasFlag(FaceChannelFlags.BlendShapes))
            {
                m_Actor.BlendShapes = m_Pose.BlendShapes;
            }

            if (m_Channels.HasFlag(FaceChannelFlags.HeadPosition))
            {
                m_Actor.HeadPosition = m_Pose.HeadPosition;
            }

            if (m_Channels.HasFlag(FaceChannelFlags.HeadRotation))
            {
                m_Actor.HeadOrientation = m_Pose.HeadOrientation.eulerAngles;
            }

            if (m_Channels.HasFlag(FaceChannelFlags.Eyes))
            {
                m_Actor.LeftEyeOrientation = m_Pose.LeftEyeOrientation.eulerAngles;
                m_Actor.RightEyeOrientation = m_Pose.RightEyeOrientation.eulerAngles;
            }

            m_Actor.BlendShapesEnabled = m_Channels.HasFlag(FaceChannelFlags.BlendShapes);
            m_Actor.HeadPositionEnabled = m_Channels.HasFlag(FaceChannelFlags.HeadPosition);
            m_Actor.HeadOrientationEnabled = m_Channels.HasFlag(FaceChannelFlags.HeadRotation);
            m_Actor.EyeOrientationEnabled = m_Channels.HasFlag(FaceChannelFlags.Eyes);
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            UpdateTimestampTracker();
            UpdateClient();
        }

        void OnFacePoseSampleReceived(FaceSample sample)
        {
            m_TimestampTracker.SetTimestamp(sample.Timestamp);
            m_Pose = sample.FacePose;

            if (IsRecording())
            {
                m_Recorder.Channels = m_Channels;
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
