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
    /// <remarks>
    /// This class provides the devices with the PlayableDirector's playable graph, to allow them to modify
    /// the animation stream and make actors in the scene go live.
    /// </remarks>
    [ExecuteAlways]
    [DefaultExecutionOrder(-10)]
    [DisallowMultipleComponent]
    [ExcludeFromPreset]
    [RequireComponent(typeof(PlayableDirector))]
    [AddComponentMenu("Live Capture/Take Recorder")]
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
        PlayableGraph m_DirectorGraph;
        List<LiveCaptureDevice> m_RecordingDevices = new List<LiveCaptureDevice>();
        bool m_Recording;
        PlayableGraph m_PreviewGraph;
        TakeRecorderPlayable m_Playable;
        ISlatePlayer m_ExternalSlatePlayer;
        bool m_IsEvaluatingExternally;
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

        void OnDisable()
        {
            StopRecordingInternal();
            DestroyPreviewGraph();
            DestroyLiveLink();
            DisposeScreenshot();
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
                m_IsEvaluatingExternally = true;
            }
        }

        internal void AddDevice(LiveCaptureDevice device)
        {
            if (m_Devices.AddUnique(device))
            {
                BuildLiveLink(device);
            }
        }

        internal void RemoveDevice(LiveCaptureDevice device)
        {
            if (m_Devices.Remove(device))
            {
                device.BuildLiveLink(default(PlayableGraph));
            }
        }

        internal bool ContainsDevice(LiveCaptureDevice device)
        {
            return m_Devices.Contains(device);
        }

        internal PlayableGraph GetPreviewGraph()
        {
            return m_PreviewGraph;
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

            Rebuild();

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
                && !IsRecording())
            {
                TakeScreenshot();

                m_Recording = true;

                Debug.Assert(slate != null);

                slate.Take = slate.IterationBase;

                PlayPreview();

                m_RecordingDevices.Clear();

                foreach (var device in m_Devices)
                {
                    if (device != null && device.IsLive())
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
                    if (device != null && device.IsRecording())
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

        /// <summary>
        /// Rebuilds the PlayableGraph and prepares the devices.
        /// </summary>
        public void Rebuild()
        {
            if (isActiveAndEnabled)
            {
                m_Director.RebuildGraph();

                SetupDirectorGraphIfNeeded();
            }
        }

        Take ProduceTake()
        {
            var slate = GetEffectiveSlatePlayer().GetActiveSlate();

            Debug.Assert(slate != null);

            using (var takeBuilder = new TakeBuilder(
                slate.Duration,
                slate.SceneNumber,
                slate.ShotName,
                slate.TakeNumber,
                slate.Description,
                slate.Directory,
                slate.IterationBase,
                FrameRate,
                m_Screenshot,
                m_Director))
            {
                TimelineUndoUtility.SetUndoEnabled(false);

                ProduceTake(takeBuilder);

                TimelineUndoUtility.SetUndoEnabled(true);

                return takeBuilder.Take;
            }
        }

        void ProduceTake(ITakeBuilder takeBuilder)
        {
            foreach (var device in m_RecordingDevices)
            {
                if (device != null)
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
        }

        void LateUpdate()
        {
            SetupDirectorGraphIfNeeded();

            EvaluateIfNeeded();

            m_IsEvaluatingExternally = false;
        }

        bool IsDeviceValid(LiveCaptureDevice device)
        {
            return device != null && device.transform.parent == transform;
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

        void SetupDirectorGraphIfNeeded()
        {
            var graph = m_Director.playableGraph;

            if (!graph.IsValid())
            {
                m_Director.RebuildGraph();

                graph = m_Director.playableGraph;
            }

            if (!m_DirectorGraph.Equals(graph))
            {
                m_IsEvaluatingExternally = false;
                m_Director.Pause();
                m_DirectorGraph = graph;

                BuildLiveLink();
            }
        }

        void BuildLiveLink()
        {
            if (!IsLive())
            {
                return;
            }

            foreach (var device in m_Devices)
            {
                BuildLiveLink(device);
            }
        }

        void BuildLiveLink(LiveCaptureDevice device)
        {
            if (device != null && device.isActiveAndEnabled && IsLive())
            {
                device.BuildLiveLink(m_DirectorGraph);
            }
        }

        void DestroyLiveLink()
        {
            if (m_DirectorGraph.IsValid())
            {
                m_Director.RebuildGraph();
            }
        }

        void EvaluateIfNeeded()
        {
            if (!m_IsEvaluatingExternally
                && IsLive()
                && m_DirectorGraph.IsValid()
                && !m_DirectorGraph.IsPlaying())
            {
                m_DirectorGraph.Evaluate();
            }
        }

        void TakeScreenshot()
        {
            DisposeScreenshot();

            var camera = Camera.main;

            if (camera == null && Camera.allCamerasCount > 0)
            {
                camera = Camera.allCameras[0];
            }

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
