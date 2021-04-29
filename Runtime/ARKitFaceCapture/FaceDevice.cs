using Unity.LiveCapture.CompanionApp;
using UnityEngine;

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
    [HelpURL(Documentation.baseURL + "ref-component-arkit-face-device" + Documentation.endURL)]
    public class FaceDevice : CompanionAppDevice<IFaceClient>, ITimedDataSource
    {
        [SerializeField, HideInInspector]
        string m_Guid;

        [SerializeField]
        FaceActor m_Actor;

        [SerializeField, HideInInspector, EnumFlagButtonGroup(100f)]
        FaceChannelFlags m_Channels = FaceChannelFlags.All;

        [SerializeField, HideInInspector]
        FacePose m_Pose = FacePose.Identity;

        [SerializeField, HideInInspector]
        FrameTime m_SyncPresentationOffset;

        [SerializeField]
        bool m_IsSynchronized;

        [SerializeField, HideInInspector]
        int m_BufferSize = 1;

        [SerializeField]
        FaceDeviceRecorder m_Recorder = new FaceDeviceRecorder();

        readonly TimestampTracker m_TimestampTracker = new TimestampTracker();
        double? m_FirstSampleTimecode;

        /// <summary>
        /// The nominal frame rate used for doing frame number conversions in the synchronization buffer.
        /// </summary>
        /// <remarks>
        /// It doesn't really matter what this value is, but choosing a frame rate that closely
        /// matches the actual data rate of this source makes the contents of the buffer
        /// easier to inspect by a human.
        /// </remarks>
        static readonly FrameRate k_SyncBufferNominalFrameRate = StandardFrameRate.FPS_60_00;
        TimedDataBuffer<FacePose> m_SyncBuffer;

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
                    RegisterLiveProperties();
                }
            }
        }

        /// <inheritdoc/>
        public string Id => m_Guid;

        /// <inheritdoc/>
        string IRegistrable.FriendlyName => name;

        /// <inheritdoc/>
        public ISynchronizer Synchronizer { get; set; }

        /// <inheritdoc/>
        public FrameRate FrameRate => m_SyncBuffer.FrameRate;

        /// <inheritdoc/>
        public int BufferSize
        {
            get => m_BufferSize;
            set
            {
                m_SyncBuffer.SetCapacity(value);
                m_BufferSize = value;
            }
        }

        // No minimimum or maximum buffer size
        /// <inheritdoc/>
        public int? MaxBufferSize => null;
        /// <inheritdoc/>
        public int? MinBufferSize => null;

        /// <inheritdoc/>
        public FrameTime PresentationOffset
        {
            get => m_SyncPresentationOffset;
            set => m_SyncPresentationOffset = value;
        }

        /// <inheritdoc />
        public bool IsSynchronized
        {
            get => m_IsSynchronized;
            set => m_IsSynchronized = value;
        }

        /// <inheritdoc />
        public bool TryGetBufferRange(out FrameTime oldestSample, out FrameTime newestSample)
        {
            return m_SyncBuffer.TryGetBufferRange(out oldestSample, out newestSample);
        }

        bool TryGetInternalClient(out IFaceClientInternal client)
        {
            client = GetClient() as IFaceClientInternal;

            return client != null;
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            TimedDataSourceManager.Instance.EnsureIdIsValid(ref m_Guid);
            TimedDataSourceManager.Instance.Register(this);

            m_SyncBuffer = new TimedDataBuffer<FacePose>(k_SyncBufferNominalFrameRate, m_BufferSize);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        /// <remaks>
        /// This is also called when the object is destroyed and can be used for any cleanup code.
        ///  When scripts are reloaded after compilation has finished, OnDisable will be called, followed by an OnEnable after the script has been loaded.
        /// </remaks>
        protected override void OnDisable()
        {
            base.OnDisable();

            TimedDataSourceManager.Instance.Unregister(this);
        }

        void OnValidate()
        {
            m_Recorder.Validate();
        }

        /// <summary>
        /// Indicates whether a device is ready for recording.
        /// </summary>
        /// <returns>
        /// true if ready for recording; otherwise, false.
        /// </returns>
        public override bool IsReady()
        {
            return base.IsReady() && m_Actor != null;
        }

        /// <summary>
        /// The device calls this method when the recording state has changed.
        /// </summary>
        protected override void OnRecordingChanged()
        {
            if (IsRecording())
            {
                m_TimestampTracker.Reset();
                m_Recorder.FrameRate = GetTakeRecorder().FrameRate;
                m_Recorder.Prepare();
                m_FirstSampleTimecode = null;

                UpdateTimestampTracker();
            }
        }

        /// <summary>
        /// Gets the name used for the take asset name.
        /// </summary>
        /// <returns>The name of the asset.</returns>
        protected override string GetAssetName()
        {
            return m_Actor != null ? m_Actor.name : name;
        }

        /// <summary>
        /// The device calls this method when a new client is assigned.
        /// </summary>
        protected override void OnClientAssigned()
        {
            if (TryGetInternalClient(out var client))
            {
                client.FacePoseSampleReceived += OnFacePoseSampleReceived;
            }
        }

        /// <summary>
        /// The device calls this method when the client is unassigned.
        /// </summary>
        protected override void OnClientUnassigned()
        {
            if (TryGetInternalClient(out var client))
            {
                client.FacePoseSampleReceived -= OnFacePoseSampleReceived;
            }
        }

        /// <summary>
        /// The device calls this method when a live performance starts and properties are about to change.
        /// </summary>
        /// <param name="previewer">The <see cref="IPropertyPreviewer"/> to use to register live properties.</param>
        protected override void OnRegisterLiveProperties(IPropertyPreviewer previewer)
        {
            Debug.Assert(m_Actor != null);

            foreach (var previewable in m_Actor.GetComponents<IPreviewable>())
            {
                previewable.Register(previewer);
            }
        }

        /// <inheritdoc/>
        public override void Write(ITakeBuilder takeBuilder)
        {
            if (m_Actor == null || m_Recorder.IsEmpty())
            {
                return;
            }

            takeBuilder.CreateAnimationTrack(
                "Face",
                m_Actor.Animator,
                m_Recorder.Bake(),
                startTime: m_FirstSampleTimecode);
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            UpdateTimestampTracker();
            UpdateClient();
        }

        /// <inheritdoc/>
        public override void LiveUpdate()
        {
            Debug.Assert(m_Actor != null, "Actor is null");

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
        public TimedSampleStatus PresentAt(Timecode timecode, FrameRate frameRate)
        {
            Debug.Assert(IsSynchronized, "Attempting to call PresentAt() when data source is not being synchronized");

            // Get the frame time with respect to our buffer's frame rate
            var requestedFrameTime = FrameTime.Remap(timecode.ToFrameTime(frameRate), frameRate, m_SyncBuffer.FrameRate);

            // Apply offset (at our buffer's frame rate)
            var presentationTime = requestedFrameTime + PresentationOffset;

            var status = m_SyncBuffer.TryGetSample(presentationTime, out var facePose);
            if (status != TimedSampleStatus.DataMissing)
            {
                if (IsRecording())
                {
                    // If we're recording a track with Timecode, we require the first recorded sample appear
                    // at t=0 (the start of the clip).
                    // This is necessary for the TakeBuilder to do proper clip alignment.
                    var sampleTime = presentationTime.ToSeconds(m_SyncBuffer.FrameRate);
                    m_FirstSampleTimecode ??= sampleTime;
                    var delta = sampleTime - m_FirstSampleTimecode.Value;
                    m_Recorder.Time = delta;
                    m_Recorder.Channels = m_Channels;
                    m_Recorder.Record(ref facePose);
                }

                m_Pose = facePose;

                Refresh();
            }

            return status;
        }

        void OnFacePoseSampleReceived(FaceSample sample)
        {
            if (!IsSynchronized)
            {
                m_Pose = sample.FacePose;
                Refresh();
            }
            else
            {
                m_SyncBuffer.Add(sample.Time, sample.FacePose);
            }

            if (IsRecording() && !IsSynchronized)
            {
                m_TimestampTracker.SetTimestamp(sample.Time);
                m_Recorder.Time = m_TimestampTracker.LocalTime;
                m_Recorder.Channels = m_Channels;
                m_Recorder.Record(ref m_Pose);
            }
        }

        void UpdateTimestampTracker()
        {
            var time = GetTakeRecorder().GetPreviewTime();

            m_TimestampTracker.Time = time;
        }
    }
}
