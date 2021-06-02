using System;
using System.Collections;
using System.Collections.Generic;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A sampler allowing for the generation of smooth damped animation based on a collection of samples.
    /// </summary>
    class SmoothDampSampler : IEnumerator<Keyframe>
    {
        const float k_MaxSpeed = float.MaxValue;

        TimestampTracker m_TimeStampTracker = new TimestampTracker();
        Queue<Keyframe> m_Samples = new Queue<Keyframe>();
        FrameRate m_FrameRate = StandardFrameRate.FPS_30_00;
        FrameTime m_CurrentFrameTime;
        float m_Target;
        float m_LastSampleTime;
        float m_CurrentVelocity;
        float m_SmoothTime;
        bool m_Initialized;

        public float SmoothTime
        {
            set => m_SmoothTime = Mathf.Max(0, value);
        }

        public FrameRate FrameRate
        {
            get => m_FrameRate;
            set
            {
                if (m_FrameRate != value)
                {
                    m_CurrentFrameTime = FrameTime.Remap(m_CurrentFrameTime, m_FrameRate, value);
                    m_FrameRate = value;
                }
            }
        }

        public float Time
        {
            get => m_TimeStampTracker.Time;
            set => m_TimeStampTracker.Time = value;
        }

        public Keyframe Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Reset()
        {
            m_Initialized = false;
            m_CurrentFrameTime = default;
            m_CurrentVelocity = 0;
            m_TimeStampTracker.Reset();
            m_Samples.Clear();
        }

        public void Add(Keyframe sample, bool isTimestamp = false)
        {
            if (isTimestamp)
            {
                // Enqueue the sample with local time.
                m_TimeStampTracker.SetTimestamp(sample.time);
                m_Samples.Enqueue(new Keyframe(m_TimeStampTracker.LocalTime, sample.value));
            }
            else
            {
                m_Samples.Enqueue(sample);
            }
        }

        public void Initialize(float target)
        {
            m_Initialized = true;

            // Initialize to prevent the damped value from ramping up from zero.
            m_Target = target;
            Current = new Keyframe(0, target);
            m_CurrentFrameTime = FrameTime.FromSeconds(m_FrameRate, Time);
            m_LastSampleTime = -1;
        }

        public void Dispose() {}

        public bool MoveNext()
        {
            Assert.IsTrue(m_Initialized);

            // Do not evaluate samples beyond the current time.
            if (m_CurrentFrameTime.ToSeconds(m_FrameRate) > Time)
            {
                return false;
            }

            // Try and update target based on pending samples.
            if (TryGetNextValidSample(out var sample))
            {
                var frameTime = FrameTime.FromSeconds(m_FrameRate, sample.time);

                if (frameTime <= m_CurrentFrameTime)
                {
                    m_LastSampleTime = sample.time;
                    m_Target = sample.value;
                    m_Samples.Dequeue();
                }
            }

            var value = Mathf.SmoothDamp(
                Current.value, m_Target,
                ref m_CurrentVelocity, m_SmoothTime, k_MaxSpeed, (float)m_FrameRate.FrameInterval);

            Current = new Keyframe
            {
                time = (float)m_CurrentFrameTime.ToSeconds(m_FrameRate),
                value = value
            };

            m_CurrentFrameTime++;

            return true;
        }

        bool TryGetNextValidSample(out Keyframe sample)
        {
            if (!TryPeek(out sample))
            {
                return false;
            }

            // A sample is valid if it is more recent than the last one returned.
            // Since we do not sort the queue, we want to skip out-of-order samples.
            while (sample.time <= m_LastSampleTime)
            {
                m_Samples.Dequeue();

                if (!TryPeek(out sample))
                {
                    return false;
                }
            }

            return true;
        }

        bool TryPeek(out Keyframe sample)
        {
            sample = default;

            if (m_Samples.Count > 0)
            {
                sample = m_Samples.Peek();
                return true;
            }

            return false;
        }
    }
}
