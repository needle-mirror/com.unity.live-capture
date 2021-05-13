using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture
{
    class Sampler : IEnumerator<Keyframe>
    {
        enum State
        {
            BeginSampling,
            Sampling,
            EndSampling
        }

        const int k_MaxFramesBetweenSamples = 5;

        Queue<Keyframe> m_Samples = new Queue<Keyframe>();
        Keyframe m_LastSample;
        State m_State = State.BeginSampling;
        bool m_Flushing;
        FrameRate m_FrameRate = StandardFrameRate.FPS_30_00;
        FrameTime m_CurrentFrameTime;

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

        public FrameTime FrameTime => m_CurrentFrameTime;

        public Keyframe Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Reset()
        {
            m_Samples.Clear();
            m_CurrentFrameTime = default(FrameTime);
            m_State = State.BeginSampling;
            m_Flushing = false;
        }

        public void Add(Keyframe sample)
        {
            m_Samples.Enqueue(sample);
        }

        public void Flush()
        {
            m_Flushing = true;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (m_Samples.Count == 0)
            {
                m_Flushing = false;

                return false;
            }

            if (m_Flushing)
            {
                m_State = State.BeginSampling;
            }

            if (m_State == State.BeginSampling)
            {
                m_State = State.Sampling;
                m_LastSample = m_Samples.Dequeue();
                m_CurrentFrameTime = FrameTime.FromSeconds(m_FrameRate, m_LastSample.time);
                Current = m_LastSample;
            }
            else if (m_State == State.EndSampling)
            {
                if (!TryGetNextValidSample(out var nextSample))
                {
                    return false;
                }

                var nextFrameTime = FrameTime.FromSeconds(m_FrameRate, nextSample.time);

                m_CurrentFrameTime = new FrameTime(nextFrameTime.FrameNumber - 1);

                Current = new Keyframe()
                {
                    time = (float)m_CurrentFrameTime.ToSeconds(m_FrameRate),
                    value = Current.value
                };

                m_State = State.BeginSampling;
            }
            else if (m_State == State.Sampling)
            {
                if (!TryGetNextValidSample(out var nextSample))
                {
                    return false;
                }

                var nextSampleFrameTime = FrameTime.FromSeconds(m_FrameRate, nextSample.time);
                var nextFrameTime = new FrameTime(m_CurrentFrameTime.FrameNumber + 1);

                while (nextFrameTime > nextSampleFrameTime)
                {
                    m_LastSample = m_Samples.Dequeue();

                    if (!TryGetNextValidSample(out nextSample))
                    {
                        return false;
                    }

                    nextSampleFrameTime = FrameTime.FromSeconds(m_FrameRate, nextSample.time);
                }

                var maxFramesCheck = (nextSampleFrameTime - m_CurrentFrameTime) >= new FrameTime(k_MaxFramesBetweenSamples);

                if (maxFramesCheck)
                {
                    Current = m_LastSample;
                    m_State = State.EndSampling;
                }
                else
                {
                    var prevTime = m_LastSample.time;
                    var nextTime = (float)nextFrameTime.ToSeconds(m_FrameRate);
                    var t = (nextTime - prevTime) / (nextSample.time - prevTime);
                    var a = m_LastSample.value;
                    var b = nextSample.value;
                    var value = Mathf.Lerp(a, b, t);

                    Current = new Keyframe(nextTime, value);
                    m_CurrentFrameTime++;
                }
            }

            return true;
        }

        bool TryGetNextValidSample(out Keyframe sample)
        {
            if (!TryPeek(out sample))
            {
                return false;
            }

            while (sample.time <= m_LastSample.time)
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
