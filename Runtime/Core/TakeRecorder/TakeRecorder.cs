using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A take recorder that manages a set of capture devices.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    [ExcludeFromPreset]
    [RequireComponent(typeof(PlayableDirector))]
    [AddComponentMenu("Live Capture/Take Recorder")]
    [HelpURL(Documentation.baseURL + "ref-component-take-recorder" + Documentation.endURL)]
    public class TakeRecorder : MonoBehaviour, ITakeRecorderInternal
    {
        static List<TakeRecorder> s_Instances = new List<TakeRecorder>();
        internal static TakeRecorder Main => s_Instances.FirstOrDefault();

        /// <summary>
        /// TakeRecorder executes this event when recording has started or stopped.
        /// </summary>
        public static event Action<TakeRecorder> RecordingStateChanged;

        /// <summary>
        /// TakeRecorder executes this event when playback has started or stopped.
        /// </summary>
        public static event Action<TakeRecorder> PlaybackStateChanged;

        internal static void ClearSubscribers()
        {
            RecordingStateChanged = null;
            PlaybackStateChanged = null;
        }
        
        [SerializeField]
        PlayableDirectorContext m_DefaultContext = new PlayableDirectorContext();

        [SerializeField, OnlyStandardFrameRates]
        FrameRate m_FrameRate = StandardFrameRate.FPS_30_00;

        [SerializeField]
        List<LiveCaptureDevice> m_Devices = new List<LiveCaptureDevice>();

        [SerializeField]
        bool m_Live = true;

        [SerializeField]
        bool m_PlayTakeContent;

        List<LiveCaptureDevice> m_RecordingDevices = new List<LiveCaptureDevice>();
        bool m_Recording;
        double m_RecordingStartTime;
        Playable m_RecordTimer;
        bool m_IsPlayingCached;
        TakeRecorderPlaybackState m_PlaybackState = new TakeRecorderPlaybackState();
        List<ITakeRecorderContextProvider> m_ContextProviders = new List<ITakeRecorderContextProvider>();
        ITakeRecorderContext m_LockedContext;
        ITakeRecorderContext m_RecordContext;
        Texture2D m_Screenshot;

        internal ITakeRecorderContext RecordContext => m_RecordContext;
        internal ITakeRecorderContext PlaybackContext => m_PlaybackState.Context;
        // For testing purposes only.
        internal bool SkipProducingAssets { get; set; }
        internal bool SkipDeviceReadyCheck { get; set; }
        internal bool PlayTakeContent
        {
            get => m_PlayTakeContent;
            set => m_PlayTakeContent = value;
        }

        /// <inheritdoc/>
        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set
            {
                if (!IsRecording())
                {
                    m_FrameRate = value;
                }
            }
        }

        /// <inheritdoc/>
        public IShot Shot => Context;

        internal ITakeRecorderContext Context
        {
            get
            {
                TryGetContext(out var context);
                return context;
            }
        }

        /// <inheritdoc/>
        bool ITakeRecorderInternal.IsEnabled => isActiveAndEnabled;

        /// <inheritdoc/>
        void ITakeRecorderInternal.SetPreviewTime(UnityObject obj, double time)
        {
            if (TryFindContext(obj, out var context))
            {
                context.SetTime(time);
            }
        }

        bool TryFindContext(UnityObject obj, out ITakeRecorderContext context)
        {
            context = null;

            foreach (var contextProvider in m_ContextProviders)
            {
                foreach (var ctx in contextProvider.Contexts)
                {
                    if (ctx.UnityObject == obj)
                    {
                        context = ctx;

                        return true;
                    }
                }
            }

            return false;
        }

        void OnEnable()
        {
            s_Instances.Add(this);

            TakeRecorderUpdateManager.Instance.Register(this);
        }

        void OnDisable()
        {
            s_Instances.Remove(this);

            StopRecording();
            PausePreview();
            DisposeScreenshot();

            TakeRecorderUpdateManager.Instance.Unregister(this);
        }

        void OnValidate()
        {
            m_Devices.RemoveAll(device => !IsDeviceValid(device));

            m_DefaultContext.Director = GetComponent<PlayableDirector>();
            m_DefaultContext.UnityObject = this;
        }

        void Awake()
        {
            OnValidate();
        }

        void Reset()
        {
            OnValidate();

            var devices = GetComponentsInChildren<LiveCaptureDevice>(true)
                .Where(d => IsDeviceValid(d));

            m_Devices.AddRange(devices);
        }

        internal void AddDevice(LiveCaptureDevice device)
        {
            m_Devices.AddUnique(device);
        }

        internal void RemoveDevice(LiveCaptureDevice device)
        {
            m_Devices.Remove(device);
        }

        internal bool ContainsDevice(LiveCaptureDevice device)
        {
            return m_Devices.Contains(device);
        }

        internal void LiveUpdate()
        {
            if (IsLive())
            {
                foreach (var device in m_Devices)
                {
                    if (IsDeviceValid(device)
                        && device.isActiveAndEnabled
                        && device.IsReady())
                    {
                        try
                        {
                            device.LiveUpdate();
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    }
                }
            }

            HandleRecordingEnded();
            PlaybackChangeCheck();
        }

        /// <inheritdoc/>
        public bool IsLive()
        {
            return m_Live;
        }

        /// <inheritdoc/>
        public void SetLive(bool value)
        {
            if (m_Live == value)
            {
                return;
            }

            m_Live = value;

            if (IsLive())
            {
                m_PlaybackState.Stop();
            }
            else
            {
                StopRecording();
            }
        }

        /// <inheritdoc/>
        public bool IsRecording()
        {
            return m_Recording;
        }

        /// <inheritdoc/>
        public void StartRecording()
        {
            if (isActiveAndEnabled && CanStartRecording())
            {
                StartRecordingInternal();
            }
        }

        internal void StartRecordingInternal()
        {
            if (!IsRecording()
                && IsLive()
                && TryGetContext(out m_RecordContext))
            {
                m_Recording = true;
                m_RecordTimer = StartTimer();

                TakeScreenshot();
                RebuildContext();
                PlayPreview();

                m_RecordingStartTime = DateTime.Now.TimeOfDay.TotalSeconds;

                m_RecordingDevices.Clear();

                foreach (var device in m_Devices)
                {
                    if (IsDeviceReadyForRecording(device))
                    {
                        m_RecordingDevices.Add(device);
                        device.StartRecording();
                    }
                }

                NotifyRecordingStateChange();
            }
        }

        /// <inheritdoc/>
        public void StopRecording()
        {
            if (!IsRecording())
            {
                return;
            }

            m_PlaybackState.Stop();
            StopRecordingInternal();
        }

        void StopRecordingInternal()
        {
            if (IsRecording())
            {
                m_Recording = false;

                foreach (var device in m_RecordingDevices)
                {
                    if (IsDeviceValid(device) && device.IsRecording())
                    {
                        device.StopRecording();
                    }
                }

                if (m_RecordContext.IsValid())
                {
                    m_RecordContext.Pause();

                    ProduceTake(m_RecordContext);
                    RebuildContext();
                }

                m_RecordContext = null;
                
                StopTimer(ref m_RecordTimer);
                DisposeScreenshot();
                NotifyRecordingStateChange();
            }
        }

        /// <inheritdoc/>
        public double GetRecordingElapsedTime()
        {
            if (isActiveAndEnabled && IsRecording())
            {
                Debug.Assert(m_RecordTimer.IsValid());

                return m_RecordTimer.GetTime();
            }

            return 0d;
        }

        /// <inheritdoc/>
        public bool IsPreviewPlaying()
        {
            if (isActiveAndEnabled && TryGetContext(out var context))
            {
                return context.IsPlaying();
            }

            return false;
        }

        /// <inheritdoc/>
        public void PlayPreview()
        {
            if (isActiveAndEnabled && !m_PlaybackState.IsPlaying() && TryGetContext(out var context))
            {
                var mode = TakeRecorderPlaybackMode.Context;

                if (PlayTakeContent)
                {
                    mode = TakeRecorderPlaybackMode.Contents;
                }

                if (IsRecording())
                {
                    mode = TakeRecorderPlaybackMode.Recording;
                }

                m_PlaybackState.Play(context, mode);

                PlaybackChangeCheck();
            }
        }

        internal void GoToBeginning()
        {
            if (isActiveAndEnabled && TryGetContext(out var context))
            {
                var time = 0d;

                if (m_PlayTakeContent)
                {
                    var take = context.Take;

                    if (take != null && take.TryGetContentRange(out var start, out var end))
                    {
                        var timeOffset = context.GetTimeOffset();

                        time = start - timeOffset;
                    }
                }

                context.SetTime(time);
            }
        }

        /// <inheritdoc/>
        public void PausePreview()
        {
            if (isActiveAndEnabled && TryGetContext(out var context))
            {
                context.Pause();

                PlaybackChangeCheck();
            }
        }

        /// <inheritdoc/>
        public double GetPreviewDuration()
        {
            if (TryGetContext(out var context))
            {   
                return context.GetDuration();
            }

            return 0d;
        }

        /// <inheritdoc/>
        public double GetPreviewTime()
        {
            if (TryGetContext(out var context))
            {
                return context.GetTime();
            }

            return 0d;
        }

        /// <inheritdoc/>
        public void SetPreviewTime(double time)
        {
            if (isActiveAndEnabled && TryGetContext(out var context))
            {
                context.SetTime(time);

                PlaybackChangeCheck();
            }
        }

        void RebuildContext()
        {
            if (TryGetContext(out var context))
            {
                context.Pause();
                context.Rebuild();
            }
        }

        void ProduceTake(ITakeRecorderContext context)
        {
            if (SkipProducingAssets)
            {
                return;
            }

            Debug.Assert(context != null);

            var contextStartTime = context.GetTimeOffset();
            var contextDuration = context.GetDuration();
            var recordingStartTime = m_PlaybackState.InitialOffset;
            var recordingDuration = m_RecordTimer.GetTime();
            var slate = context.Slate;
            var resolver = context.GetResolver();

            Debug.Assert(resolver != null);

            using var takeBuilder = new TakeBuilder(
                contextStartTime,
                contextDuration,
                recordingStartTime,
                recordingDuration,
                slate.SceneNumber,
                slate.ShotName,
                slate.TakeNumber,
                slate.Description,
                context.Directory,
                DateTime.Now,
                context.IterationBase,
                FrameRate,
                m_Screenshot,
                resolver);

            using (TimelineDisableUndoScope.Create())
            {
                ProduceTake(takeBuilder);
            }

            var timeline = takeBuilder.Take.Timeline;
            if (timeline != null)
            {
                takeBuilder.Take.Duration = timeline.duration;
            }

            slate.TakeNumber++;

            context.Take = takeBuilder.Take;
            context.Slate = slate;

            var director = resolver as PlayableDirector;

            SetDirty(director);
            SetDirty(context.UnityObject);
        }

        void SetDirty(UnityObject obj)
        {
#if UNITY_EDITOR
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
#endif
        }

        void ProduceTake(TakeBuilder takeBuilder)
        {
            foreach (var device in m_RecordingDevices)
            {
                if (IsDeviceValid(device))
                {
                    try
                    {
                        device.Write(takeBuilder);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            takeBuilder.AlignTracks(m_RecordingStartTime);
        }

        void Update()
        {
            foreach (var device in m_Devices)
            {
                if (IsDeviceValid(device) && device.isActiveAndEnabled)
                {
                    try
                    {
                        device.UpdateDevice();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }

            m_PlaybackState.Update();
        }

        bool IsDeviceValid(LiveCaptureDevice device)
        {
            return device != null && device.transform.parent == transform;
        }

        bool IsDeviceReadyForRecording(LiveCaptureDevice device)
        {
            return IsDeviceValid(device) && device.isActiveAndEnabled && device.IsReady();
        }

        bool AreDevicesReadyForRecording(IEnumerable<LiveCaptureDevice> devices)
        {
            if (SkipDeviceReadyCheck)
            {
                return true;
            }

            foreach (var device in devices)
            {
                if (IsDeviceReadyForRecording(device))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool CanStartRecording()
        {
            return AreDevicesReadyForRecording(m_Devices);
        }

        void TakeScreenshot()
        {
            DisposeScreenshot();

            var camera = LiveCaptureUtility.GetTopCamera();

            if (camera != null)
            {
                m_Screenshot = Screenshot.Take(camera);
            }
        }

        void DisposeScreenshot()
        {
            if (m_Screenshot != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_Screenshot);
                }
                else
                {
                    DestroyImmediate(m_Screenshot);
                }
            }
        }

        internal void AddContextProvider(ITakeRecorderContextProvider contextProvider)
        {
            m_ContextProviders.AddUnique(contextProvider);
        }

        internal void RemoveContextProvider(ITakeRecorderContextProvider contextProvider)
        {
            m_ContextProviders.Remove(contextProvider);
        }

        internal bool HasExternalContextProvider()
        {
            return m_ContextProviders.Count > 0;
        }

        internal void LockContext()
        {
            if (TryGetContext(out var context))
            {
                LockContext(context);
            }
        }

        internal void LockContext(ITakeRecorderContext context)
        {
            m_LockedContext = context;
        }

        internal void UnlockContext()
        {
            m_LockedContext = null;
        }

        internal bool IsLocked()
        {
            return m_LockedContext != null && m_LockedContext.IsValid();
        }

        bool TryGetContext(out ITakeRecorderContext context)
        {
            context = default(ITakeRecorderContext);

            if (m_LockedContext != null && m_LockedContext.IsValid())
            {
                context = m_LockedContext;

                return true;
            }

            foreach (var contextProvider in m_ContextProviders)
            {
                var activeContext = contextProvider.GetActiveContext();

                if (activeContext != null)
                {
                    context = activeContext;

                    return true;
                }
            }

            if (m_ContextProviders.Count == 0 && m_DefaultContext.IsValid())
            {
                context = m_DefaultContext;

                return true;
            }

            return false;
        }

        void NotifyRecordingStateChange()
        {
            try
            {
                RecordingStateChanged?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void NotifyPlaybackStateChange()
        {
            try
            {
                PlaybackStateChanged?.Invoke(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void PlaybackChangeCheck()
        {
            var isPlaying = IsPreviewPlaying();

            if (m_IsPlayingCached != isPlaying)
            {
                m_IsPlayingCached = isPlaying;

                NotifyPlaybackStateChange();
            }
        }

        void HandleRecordingEnded()
        {
            if (IsRecording())
            {
                if(!AreDevicesReadyForRecording(m_RecordingDevices))
                {
                    StopRecording();
                }
                else if (m_RecordContext.IsValid())
                {
                    if(m_RecordContext.GetDuration() > 0d && !m_RecordContext.IsPlaying())
                    {
                        StopRecordingInternal();
                    }
                }
                else
                {
                    StopRecordingInternal();
                }
            }
        }

        static Playable StartTimer()
        {
            var graph = PlayableGraph.Create("TakeRecorderTimer");
            var output = ScriptPlayableOutput.Create(graph, "Output");
            var playable = Playable.Create(graph);
            var updateMode = Application.isPlaying
                ? DirectorUpdateMode.GameTime
                : DirectorUpdateMode.UnscaledGameTime;

            output.SetSourcePlayable(playable);
            graph.SetTimeUpdateMode(updateMode);
            graph.Play();

            return playable;
        }

        static void StopTimer(ref Playable playable)
        {
            if (!playable.IsValid())
            {
                return;
            }

            var graph = playable.GetGraph();

            if (graph.IsValid())
            {
                graph.Destroy();
            }

            playable = Playable.Null;
        }
    }
}
