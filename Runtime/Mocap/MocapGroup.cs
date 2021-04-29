using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.LiveCapture.Mocap
{
    /// <summary>
    /// A type of <see cref="LiveCaptureDevice"/> that manages a set of <see cref="Unity.LiveCapture.Mocap.MocapDevice{T}"/>.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [CreateDeviceMenuItem("Mocap Group")]
    [HelpURL(Documentation.baseURL + "ref-component-mocap-group" + Documentation.endURL)]
    public class MocapGroup : LiveCaptureDevice
    {
        [SerializeField]
        Animator m_Animator;
        [SerializeField]
        List<LiveCaptureDevice> m_Devices = new List<LiveCaptureDevice>();
        [SerializeField]
        LiveCaptureDevice m_TimeSource;
        MocapRecorder m_Recorder = new MocapRecorder();
        double? m_FirstFrameTime;
        bool m_Recording;

        internal IReadOnlyList<LiveCaptureDevice> Devices => m_Devices;

        /// <summary>
        /// The Animator component this device operates.
        /// </summary>
        public Animator Animator
        {
            get => m_Animator;
            set => m_Animator = value;
        }

        /// <inheritdoc/>
        public override bool IsReady()
        {
            return m_Animator != null;
        }

        /// <inheritdoc/>
        public override bool IsRecording()
        {
            return m_Recording;
        }

        internal MocapRecorder GetRecorder()
        {
            return m_Recorder;
        }

        internal void AddDevice(LiveCaptureDevice device)
        {
            Debug.Assert(device is IMocapDevice);

            if (m_Devices.Contains(device))
                return;

            if (m_TimeSource == null)
                m_TimeSource = device;

            m_Devices.AddUnique(device);
        }

        internal bool RemoveDevice(LiveCaptureDevice device)
        {
            var removed = m_Devices.Remove(device);

            if (m_TimeSource == device)
                m_TimeSource = m_Devices.FirstOrDefault();

            return removed;
        }

        void OnValidate()
        {
            m_Devices.RemoveAll(device => !IsDeviceValid(device));
        }

        bool IsDeviceValid(LiveCaptureDevice device)
        {
            return device is IMocapDevice && device.transform.parent == transform;
        }

        /// <inheritdoc/>
        public override void UpdateDevice()
        {
            m_Recorder.Animator = m_Animator;

            foreach (var device in m_Devices)
            {
                if (device is IMocapDevice source)
                {
                    source.Animator = m_Animator;
                    device.SetTakeRecorderOverride(GetTakeRecorder());

                    if (device.isActiveAndEnabled)
                    {
                        try
                        {
                            device.UpdateDevice();
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    }
                }
            }

            UpdateRecorder();
        }

        /// <inheritdoc/>
        public override void LiveUpdate()
        {
            foreach (var device in m_Devices)
            {
                if (device is IMocapDevice source)
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

            m_Recorder.ApplyFrame();
        }

        /// <inheritdoc/>
        public override void StartRecording()
        {
            if (!m_Recording)
            {
                m_Recording = true;
                m_FirstFrameTime = null;
                m_Recorder.Clear();
                m_Recorder.FrameRate = GetTakeRecorder().FrameRate;

                InvokeRecordingChanged();
            }
        }

        /// <inheritdoc/>
        public override void StopRecording()
        {
            if (IsRecording())
            {
                m_Recording = false;

                InvokeRecordingChanged();
            }
        }

        void InvokeRecordingChanged()
        {
            foreach (var device in m_Devices)
            {
                if (device is IMocapDevice source)
                {
                    source.InvokeRecordingChanged();
                }
            }
        }

        /// <inheritdoc/>
        public override void Write(ITakeBuilder takeBuilder)
        {
            if (m_Animator == null || m_Recorder.IsEmpty())
                return;

            takeBuilder.CreateAnimationTrack(
                m_Animator.name,
                m_Animator,
                m_Recorder.Bake(),
                startTime: m_FirstFrameTime);
        }

        void UpdateRecorder()
        {
            if (IsRecording() && m_TimeSource is IMocapDevice source)
            {
                var time = source.GetCurrentFrameTime();

                m_FirstFrameTime ??= time;
                m_Recorder.Record(time - m_FirstFrameTime.Value);
            }
        }
    }
}
