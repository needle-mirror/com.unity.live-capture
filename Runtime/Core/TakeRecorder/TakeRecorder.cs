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
        
        [SerializeField]
        PlayableDirectorContext m_DefaultContext = new PlayableDirectorContext();

        [SerializeField, OnlyStandardFrameRates]
        FrameRate m_FrameRate = StandardFrameRate.FPS_30_00;

        [SerializeField]
        List<LiveCaptureDevice> m_Devices = new List<LiveCaptureDevice>();

        [SerializeField]
        bool m_Live = true;

        List<LiveCaptureDevice> m_RecordingDevices = new List<LiveCaptureDevice>();
        bool m_Recording;
        bool m_PreviewEnded;
        double? m_RequestedPreviewTime;
        double m_RecordingStartTime;
        TakeRecorderPlayer m_Player;
        List<ITakeRecorderContextProvider> m_ContextProviders = new List<ITakeRecorderContextProvider>();
        ITakeRecorderContext m_LockedContext;
        ITakeRecorderContext m_PlaybackContext;
        Texture2D m_Screenshot;

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
        public ISlate GetActiveSlate()
        {
            if (TryGetContext(out var context))
            {
                return context.GetSlate();
            }

            return null;
        }

        /// <inheritdoc/>
        void ITakeRecorderInternal.SetPreviewTime(ISlate slate, double time)
        {
            
        }

        /// <inheritdoc/>
        bool ITakeRecorderInternal.IsEnabled => isActiveAndEnabled;

        /// <inheritdoc/>
        void ITakeRecorderInternal.Prepare()
        {
            Prepare();
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
            HandlePreviewEnded();
            m_Player.Destroy();
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

        internal PlayableGraph GetPreviewGraph()
        {
            return m_Player.Graph;
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
                SetPreviewTime(0d);
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
                && TryGetContext(out var context))
            {
                m_Recording = true;

                TakeScreenshot();
                Prepare();
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

            if (IsPreviewPlaying())
            {
                SetPreviewTime(0d);
            }

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

                if (TryGetContext(out var context))
                {
                    ProduceTake(context);
                    Prepare();
                }

                DisposeScreenshot();
                NotifyRecordingStateChange();
            }
        }

        /// <inheritdoc/>
        public bool IsPreviewPlaying()
        {
            if (m_Player.IsValid())
            {
                return m_Player.IsPlaying;
            }

            return false;
        }

        /// <inheritdoc/>
        public void PlayPreview()
        {
            if (isActiveAndEnabled && TryGetContext(out m_PlaybackContext))
            {
                CreatePlayerIfNeeded();

                var time = GetPreviewTime();
                var duration = GetPreviewDuration();

                m_Player.Play(time, duration);

                HandlePreviewTimeRequest();
                NotifyPlaybackStateChange();
            }
        }

        /// <inheritdoc/>
        public void PausePreview()
        {
            if (isActiveAndEnabled && IsPreviewPlaying())
            {
                Debug.Assert(m_Player.IsValid());

                m_Player.Pause();

                HandlePreviewTimeRequest();
                HandlePreviewEnded();
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
            if (isActiveAndEnabled)
            {
                CreatePlayerIfNeeded();

                m_Player.SetTime(time, GetPreviewDuration());

                HandlePreviewTimeRequest();
            }
        }

        void Prepare()
        {
            if (TryGetContext(out var context))
            {
                context.Prepare(IsRecording());
            }
        }

        internal void SetPreviewTimeInternal(double time)
        {
            m_RequestedPreviewTime = time;
        }

        internal void OnPreviewEnded()
        {
            // We can't call StopRecordingInternal here as it would trigger:
            // "A PlayableGraph is being directly or indirectly evaluated recursively."
            // Calling it in the next update instead.
            m_PreviewEnded = true;
        }

        internal void HandlePreviewTimeRequest()
        {
            if (!m_RequestedPreviewTime.HasValue)
                return;

            var time = m_RequestedPreviewTime.Value;

            if (TryGetContext(out var context))
            {
                context.SetTime(time);

                Callbacks.InvokeSeekOccurred();
            }

            m_RequestedPreviewTime = null;
        }

        void HandlePreviewEnded()
        {
            if (!m_PreviewEnded)
                return;

            m_PreviewEnded = false;
            m_PlaybackContext = null;

            StopRecordingInternal();
            NotifyPlaybackStateChange();
        }

        void ProduceTake(ITakeRecorderContext context)
        {
            Debug.Assert(context != null);

            var timeOffset = context.GetTimeOffset();
            var duration = context.GetDuration();
            var slate = context.GetSlate();
            var resolver = context.GetResolver();

            Debug.Assert(slate != null);
            Debug.Assert(resolver != null);

            using var takeBuilder = new TakeBuilder(
                timeOffset,
                duration,
                slate.SceneNumber,
                slate.ShotName,
                slate.TakeNumber,
                slate.Description,
                slate.Directory,
                DateTime.Now,
                slate.IterationBase,
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

            var director = resolver as PlayableDirector;

            if (director != null)
                slate.ClearSceneBindings(director);

            slate.TakeNumber++;
            slate.Take = takeBuilder.Take;

            if (director != null)
                slate.SetSceneBindings(director);

            SetDirty(director);
            SetDirty(slate.UnityObject);
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
            takeBuilder.AlignTracksByStartTimes(m_RecordingStartTime);
        }

        void Update()
        {
            HandlePreviewTimeRequest();
            HandlePreviewEnded();

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

            if (IsRecording() && !AreDevicesReadyForRecording(m_RecordingDevices))
            {
                StopRecording();
            }
        }

        void LateUpdate()
        {
            HandlePreviewTimeRequest();
            HandlePreviewEnded();
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

        void CreatePlayerIfNeeded()
        {
            if (m_Player.IsValid())
            {
                return;
            }

            m_Player = TakeRecorderPlayer.Create(this);
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

        internal ITakeRecorderContext GetContext()
        {
            if (TryGetContext(out var context))
            {
                return context;
            }

            return null;
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

            if (m_PlaybackContext != null && m_PlaybackContext.IsValid())
            {
                context = m_PlaybackContext;

                return true;
            }

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

            if (m_ContextProviders.Count == 0)
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
    }
}
