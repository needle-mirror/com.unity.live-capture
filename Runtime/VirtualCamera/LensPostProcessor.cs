using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    class LensPostProcessor
    {
        const float k_MaxSpeed = float.MaxValue;
        Lens m_Lens;
        float m_FocusDistanceVelocity;
        float m_FocalLengthVelocity;
        float m_ApertureVelocity;
        bool m_ReticlePositionChanged;

        static readonly FrameRate k_KeyframeBufferFrameRate = StandardFrameRate.FPS_60_00;
        TimedDataBuffer<float> m_FocusDistanceKeyframes = new TimedDataBuffer<float>(k_KeyframeBufferFrameRate);
        TimedDataBuffer<float> m_FocalLengthKeyframes = new TimedDataBuffer<float>(k_KeyframeBufferFrameRate);
        TimedDataBuffer<float> m_ApertureKeyframes = new TimedDataBuffer<float>(k_KeyframeBufferFrameRate);

        public int BufferSize
        {
            get => m_FocalLengthKeyframes.Capacity;
            set
            {
                m_FocalLengthKeyframes.SetCapacity(value);
                m_FocusDistanceKeyframes.SetCapacity(value);
                m_ApertureKeyframes.SetCapacity(value);
            }
        }

        public FrameRate SamplingFrameRate { get; set; } = StandardFrameRate.FPS_60_00;
        public Func<Lens, Lens> ValidationFunction { get; set; }
        public Func<float, bool, (bool updated, float distance)> UpdateFocusRigFunction { get; set; }
        public double CurrentTime { get; private set; }
        public float FocusDistanceDamping { get; set; }
        public float FocalLengthDamping { get; set; }
        public float ApertureDamping { get; set; }
        public float FocusDistanceTarget { get; private set; }

        public void MarkReticlePositionChanged()
        {
            m_ReticlePositionChanged = true;
        }

        public void Reset(Lens initialLens, double startingTime)
        {
            m_Lens = initialLens;
            CurrentTime = startingTime;
            m_FocalLengthKeyframes.Clear();
            m_FocalLengthVelocity = 0;
            m_ApertureKeyframes.Clear();
            m_ApertureVelocity = 0;
            m_FocusDistanceKeyframes.Clear();
            m_FocusDistanceVelocity = 0;
        }

        public void AddFocusDistanceKeyframe(double time, float value)
        {
            m_FocusDistanceKeyframes.Add(time, value);
        }

        public void AddFocalLengthKeyframe(double time, float value)
        {
            m_FocalLengthKeyframes.Add(time, value);
        }

        public void AddApertureKeyframe(double time, float value)
        {
            m_ApertureKeyframes.Add(time, value);
        }

        public void AddLensKeyframe(double time, Lens lens)
        {
            AddApertureKeyframe(time, lens.Aperture);
            AddFocalLengthKeyframe(time, lens.FocalLength);
            AddFocusDistanceKeyframe(time, lens.FocusDistance);
        }

        public IEnumerable<(double time, Lens lens)> ProcessTo(double time)
        {
            var deltaTime = (float)SamplingFrameRate.FrameInterval;

            while (CurrentTime <= time)
            {
                var frameTime = FrameTime.FromSeconds(k_KeyframeBufferFrameRate, CurrentTime);
                var newLens = m_Lens;
                bool hasKeyFrame = false;

                if (UpdateFocusRigFunction != null)
                {
                    var(focusUpdated, autoFocusTarget) = UpdateFocusRigFunction(
                        m_Lens.FocusDistance, m_ReticlePositionChanged);
                    if (focusUpdated)
                    {
                        m_FocusDistanceKeyframes.Add(CurrentTime, autoFocusTarget);
                    }

                    m_ReticlePositionChanged = false;
                }

                // Damp
                if (m_FocusDistanceKeyframes.GetLatest(frameTime) is {} focusDistanceTarget)
                {
                    FocusDistanceTarget = focusDistanceTarget;
                    newLens.FocusDistance = Mathf.SmoothDamp(
                        newLens.FocusDistance,
                        focusDistanceTarget, ref m_FocusDistanceVelocity,
                        FocusDistanceDamping, k_MaxSpeed, deltaTime);
                    hasKeyFrame = true;
                }
                if (m_FocalLengthKeyframes.GetLatest(frameTime) is {} focalLengthTarget)
                {
                    newLens.FocalLength = Mathf.SmoothDamp(
                        newLens.FocalLength,
                        focalLengthTarget, ref m_FocalLengthVelocity,
                        FocalLengthDamping, k_MaxSpeed, deltaTime);
                    hasKeyFrame = true;
                }
                if (m_ApertureKeyframes.GetLatest(frameTime) is {} apertureTarget)
                {
                    newLens.Aperture = Mathf.SmoothDamp(
                        newLens.Aperture,
                        apertureTarget, ref m_ApertureVelocity,
                        ApertureDamping, k_MaxSpeed, deltaTime);
                    hasKeyFrame = true;
                }

                // Validate
                if (hasKeyFrame)
                {
                    newLens = ValidationFunction?.Invoke(newLens) ?? newLens;
                    yield return (CurrentTime, newLens);
                }

                CurrentTime += SamplingFrameRate.FrameInterval;
                m_Lens = newLens;
            }
        }
    }
}
