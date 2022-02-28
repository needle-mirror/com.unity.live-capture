using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.LiveCapture.Internal;

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
        [SerializeField, HideInInspector]
        PlayableAsset m_NullPlayableAsset = null;

        [SerializeField]
        PlayableDirectorSlate m_DefaultSlatePlayer = new PlayableDirectorSlate();

        [SerializeField]
        TakePlayer m_TakePlayer = new TakePlayer();

        [SerializeField, OnlyStandardFrameRates]
        FrameRate m_FrameRate = StandardFrameRate.FPS_30_00;

        [SerializeField]
        List<LiveCaptureDevice> m_Devices = new List<LiveCaptureDevice>();

        [SerializeField]
        bool m_Live = true;

        PlayableDirector m_Director;
        List<LiveCaptureDevice> m_RecordingDevices = new List<LiveCaptureDevice>();
        bool m_Recording;
        PlayableGraph m_PreviewGraph;
        TakeRecorderPlayable m_Playable;
        ISlatePlayer m_ExternalSlatePlayer;
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
            return GetEffectiveSlatePlayer().GetActiveSlate();
        }

        /// <inheritdoc/>
        void ITakeRecorderInternal.SetPreviewTime(ISlate slate, double time)
        {
            GetEffectiveSlatePlayer().SetTime(slate, time);
        }

        /// <inheritdoc/>
        bool ITakeRecorderInternal.IsEnabled => isActiveAndEnabled;

        internal int GetSlateCount()
        {
            return GetEffectiveSlatePlayer().GetSlateCount();
        }

        internal ISlate GetSlate(int index)
        {
            return GetEffectiveSlatePlayer().GetSlate(index);
        }

        internal PlayableDirector GetPlayableDirector()
        {
            if (m_Director == null)
            {
                SetupComponents();
            }

            return m_Director;
        }

        void OnEnable()
        {
            TakeRecorderUpdateManager.Instance.Register(this);
        }

        void OnDisable()
        {
            StopRecordingInternal();
            DestroyPreviewGraph();
            DisposeScreenshot();

            TakeRecorderUpdateManager.Instance.Unregister(this);
        }

        void OnValidate()
        {
            m_Devices.RemoveAll(device => !IsDeviceValid(device));
        }

        void Awake()
        {
            SetupComponents();
        }

        void Reset()
        {
            SetupComponents();

            var devices = GetComponentsInChildren<LiveCaptureDevice>(true)
                .Where(d => d.transform.parent == transform);

            m_Devices.AddRange(devices);
        }

        void SetupComponents()
        {
            if (m_Director == null)
            {
                m_Director = GetComponent<PlayableDirector>();
            }

            m_DefaultSlatePlayer.Director = m_Director;
            m_DefaultSlatePlayer.UnityObject = this;

            m_TakePlayer.Director = m_Director;
            m_TakePlayer.FallbackPlayableAsset = m_NullPlayableAsset;
        }

        internal void SetSlatePlayer(ISlatePlayer slatePlayer)
        {
            m_ExternalSlatePlayer = slatePlayer;
        }

        internal void RemoveSlatePlayer(ISlatePlayer slatePlayer)
        {
            if (m_ExternalSlatePlayer == slatePlayer)
            {
                m_ExternalSlatePlayer = null;
            }
        }

        ISlatePlayer GetEffectiveSlatePlayer()
        {
            if (m_ExternalSlatePlayer != null)
            {
                return m_ExternalSlatePlayer;
            }
            else
            {
                return m_DefaultSlatePlayer;
            }
        }

        internal bool IsExternalSlatePlayer()
        {
            return m_ExternalSlatePlayer != null;
        }

        internal void Prepare(ISlatePlayer slatePlayer)
        {
            if (m_ExternalSlatePlayer == slatePlayer)
            {
                var take = default(Take);
                var slate = slatePlayer.GetActiveSlate();

                if (slate != null)
                {
                    take = slate.Take;
                }

                m_TakePlayer.Take = take;
            }
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
            return m_PreviewGraph;
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
                        device.LiveUpdate();
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
            var slate = GetEffectiveSlatePlayer().GetActiveSlate();

            if (isActiveAndEnabled
                && slate != null
                && IsLive()
                && !IsRecording()
                && CanStartRecording())
            {
                TakeScreenshot();

                m_Recording = true;

                Debug.Assert(slate != null);

                slate.Take = slate.IterationBase;

                PlayPreview();

                m_RecordingDevices.Clear();

                foreach (var device in m_Devices)
                {
                    if (IsDeviceReadyForRecording(device))
                    {
                        m_RecordingDevices.Add(device);
                        device.StartRecording();
                    }
                }
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
            else
            {
                StopRecordingInternal();
            }
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

                var slate = GetEffectiveSlatePlayer().GetActiveSlate();

                if (slate != null && m_RecordingDevices.Count > 0)
                {
                    slate.Take = ProduceTake();
                    slate.TakeNumber++;
#if UNITY_EDITOR
                    if (slate.UnityObject != null)
                    {
                        EditorUtility.SetDirty(slate.UnityObject);
                    }
#endif
                }

                DisposeScreenshot();
            }
        }

        /// <inheritdoc/>
        public bool IsPreviewPlaying()
        {
            if (m_Playable != null)
            {
                return m_Playable.Playing;
            }

            return false;
        }

        /// <inheritdoc/>
        public void PlayPreview()
        {
            if (isActiveAndEnabled)
            {
                CreatePreviewGraphIfNeeded();

                PlayableDirectorInternal.ResetFrameTiming();

                m_Playable.Play();
            }
        }

        /// <inheritdoc/>
        public void PausePreview()
        {
            if (isActiveAndEnabled)
            {
                CreatePreviewGraphIfNeeded();

                m_Playable.Stop();
            }
        }

        /// <inheritdoc/>
        public double GetPreviewTime()
        {
            return GetEffectiveSlatePlayer().GetTime();
        }

        /// <inheritdoc/>
        public void SetPreviewTime(double time)
        {
            if (isActiveAndEnabled)
            {
                var slate = GetEffectiveSlatePlayer().GetActiveSlate();

                if (slate != null)
                {
                    CreatePreviewGraphIfNeeded();

                    if (time < 0d)
                    {
                        time = 0d;
                    }
                    else if (time > slate.Duration)
                    {
                        time = slate.Duration;
                    }

                    m_Playable.SetTime(time);
                }
            }
        }

        internal void SetPreviewTimeInternal(double time)
        {
            GetEffectiveSlatePlayer().SetTime(time);
        }

        internal void OnPreviewEnded()
        {
            StopRecordingInternal();

            // In Edit mode we need to force a refresh of the Editor next frame.
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () => EditorApplication.QueuePlayerLoopUpdate();
            }
#endif
        }

        Take ProduceTake()
        {
            var slate = GetEffectiveSlatePlayer().GetActiveSlate();

            Debug.Assert(slate != null);

            using var takeBuilder = new TakeBuilder(
                slate.Duration,
                slate.SceneNumber,
                slate.ShotName,
                slate.TakeNumber,
                slate.Description,
                slate.Directory,
                DateTime.Now,
                slate.IterationBase,
                FrameRate,
                m_Screenshot,
                m_Director);

            TimelineUndoUtility.SetUndoEnabled(false);

            ProduceTake(takeBuilder);

            TimelineUndoUtility.SetUndoEnabled(true);

            return takeBuilder.Take;
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
            takeBuilder.AlignTracksByStartTimes();
        }

        void Update()
        {
            if (m_ExternalSlatePlayer == null)
            {
                m_TakePlayer.Take = m_DefaultSlatePlayer.Take;
            }

            m_TakePlayer.Update();

            foreach (var device in m_Devices)
            {
                if (IsDeviceValid(device) && device.isActiveAndEnabled)
                {
                    device.UpdateDevice();
                }
            }

            if (IsRecording() && !AreDevicesReadyForRecording(m_RecordingDevices))
            {
                StopRecording();
            }
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

        void CreatePreviewGraphIfNeeded()
        {
            if (m_PreviewGraph.IsValid())
            {
                return;
            }

            m_PreviewGraph = PlayableGraph.Create("Recording Graph");
            m_PreviewGraph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);

            var playable = ScriptPlayable<TakeRecorderPlayable>.Create(m_PreviewGraph);
            var output = ScriptPlayableOutput.Create(m_PreviewGraph, "Recording Output");

            output.SetSourcePlayable(playable);

            m_Playable = playable.GetBehaviour();
            m_Playable.TakeRecorder = this;
        }

        void DestroyPreviewGraph()
        {
            if (m_PreviewGraph.IsValid())
            {
                m_PreviewGraph.Destroy();
            }
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
    }
}
