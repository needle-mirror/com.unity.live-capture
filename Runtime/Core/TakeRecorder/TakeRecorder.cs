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
    [DisallowMultipleComponent]
    [ExcludeFromPreset]
    [RequireComponent(typeof(PlayableDirector))]
    [RequireComponent(typeof(SlateDatabase))]
    [AddComponentMenu("Live Capture/Take Recorder")]
    public class TakeRecorder : MonoBehaviour, ITakeRecorder
    {
        [SerializeField, OnlyStandardFrameRates]
        FrameRate m_FrameRate = StandardFrameRate.FPS_30_00;
        [SerializeField]
        List<LiveCaptureDevice> m_Devices = new List<LiveCaptureDevice>();
        [SerializeField]
        bool m_Live = true;
        PlayableDirector m_Director;
        SlateDatabase m_SlateDatabase;
        PlayableGraph m_DirectorGraph;
        List<LiveCaptureDevice> m_RecordingDevices = new List<LiveCaptureDevice>();
        bool m_Recording;
        PlayableGraph m_PreviewGraph;
        TakeRecorderPlayable m_Playable;

        /// <inheritdoc/>
        public ISlate slate => m_SlateDatabase.slate;

        /// <inheritdoc/>
        public FrameRate frameRate
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

        void OnDisable()
        {
            StopRecordingInternal();
            DestroyPreviewGraph();
            DestroyLiveLink();
        }

        void OnValidate()
        {
            ValidateDevices();
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
            m_Director = GetComponent<PlayableDirector>();
            m_SlateDatabase = GetComponent<SlateDatabase>();
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
            if (isActiveAndEnabled
                && IsLive()
                && !IsRecording())
            {
                m_Recording = true;

                Debug.Assert(slate != null);

                slate.take = slate.iterationBase;

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

                if (m_RecordingDevices.Count > 0)
                {
                    slate.take = ProduceTake();
                    slate.takeNumber++;
#if UNITY_EDITOR
                    if (slate.unityObject != null)
                    {
                        EditorUtility.SetDirty(slate.unityObject);
                    }
#endif
                }
            }
        }

        /// <inheritdoc/>
        public bool IsPreviewPlaying()
        {
            if (m_Playable != null)
            {
                return m_Playable.playing;
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
        public void SetPreviewTime(double time)
        {
            if (isActiveAndEnabled)
            {
                Debug.Assert(slate != null);

                CreatePreviewGraphIfNeeded();

                if (time < 0d)
                {
                    time = 0d;
                }
                else if (time > slate.duration)
                {
                    time = slate.duration;
                }

                m_Playable.SetTime(time);
            }
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
            Debug.Assert(slate != null);

            using (var takeBuilder = new TakeBuilder(
                LiveCaptureSettings.instance.takeNameFormat,
                LiveCaptureSettings.instance.assetNameFormat,
                slate.sceneNumber,
                slate.shotName,
                slate.takeNumber,
                slate.description,
                slate.directory,
                slate.iterationBase,
                frameRate,
                m_Director))
            {
                TimelineUndoUtility.SetUndoEnabled(false);

                ProduceTake(takeBuilder);

                TimelineUndoUtility.SetUndoEnabled(true);

                return takeBuilder.take;
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
            ValidateDevices();

            foreach (var device in m_Devices)
            {
                if (IsDeviceValid(device) && device.isActiveAndEnabled)
                {
                    device.UpdateDevice();
                }
            }

            SetupDirectorGraphIfNeeded();

            if (IsLive() && IsValidAndPaused())
            {
                Evaluate();
            }
        }

        void ValidateDevices()
        {
            m_Devices.RemoveAll(device => !IsDeviceValid(device));
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
            m_Playable.takeRecorder = this;
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
                if (device != null && device.isActiveAndEnabled)
                {
                    device.BuildLiveLink(m_DirectorGraph);
                }
            }
        }

        void DestroyLiveLink()
        {
            if (m_DirectorGraph.IsValid())
            {
                m_Director.RebuildGraph();
            }
        }

        void Evaluate()
        {
            if (m_DirectorGraph.IsValid())
            {
                m_DirectorGraph.Evaluate();
            }
        }

        bool IsValidAndPaused()
        {
            return m_DirectorGraph.IsValid() && !m_DirectorGraph.IsPlaying();
        }
    }
}
