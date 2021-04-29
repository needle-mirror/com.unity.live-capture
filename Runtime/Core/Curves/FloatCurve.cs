using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A type of <see cref="ICurve"/> that stores keyframes of type float.
    /// </summary>
    public class FloatCurve : ICurve<float>, IReduceableCurve
    {
        FloatSampler m_Sampler = new FloatSampler();
        FloatTangentUpdater m_TangentUpdater = new FloatTangentUpdater();
        FloatKeyframeReducer m_Reducer = new FloatKeyframeReducer();
        AnimationCurve m_Curve = new AnimationCurve();

        /// <inheritdoc/>
        public float MaxError
        {
            get => m_Reducer.MaxError;
            set => m_Reducer.MaxError = value;
        }

        /// <inheritdoc/>
        public string RelativePath { get; }

        /// <inheritdoc/>
        public string PropertyName { get; }

        /// <inheritdoc/>
        public Type BindingType { get; }

        /// <inheritdoc/>
        public FrameRate FrameRate
        {
            get => m_Sampler.FrameRate;
            set => m_Sampler.FrameRate = value;
        }

        /// <summary>
        /// The baked animation curve.
        /// </summary>
        internal AnimationCurve AnimationCurve => m_Curve;

        /// <summary>
        /// Constructs an instance of FloatCurve.
        /// </summary>
        /// <param name="relativePath"> Path to the game object this curve applies to.</param>
        /// <param name="propertyName"> The name or path to the property being animated.</param>
        /// <param name="bindingType"> The class type of the component that is animated.</param>
        public FloatCurve(string relativePath, string propertyName, Type bindingType)
        {
            RelativePath = relativePath;
            PropertyName = propertyName;
            BindingType = bindingType;
        }

        /// <inheritdoc/>
        public void AddKey(double time, float value)
        {
            m_Sampler.Add((float)time, value);

            Sample();
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return m_TangentUpdater.IsEmpty()
                && m_Reducer.IsEmpty()
                && m_Curve.length == 0;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            m_Sampler.Reset();
            m_TangentUpdater.Reset();
            m_Reducer.Reset();
            m_Curve = new AnimationCurve();
        }

        /// <inheritdoc/>
        public void SetToAnimationClip(AnimationClip clip)
        {
            Flush();

            if (m_Curve.length == 0)
            {
                return;
            }

            clip.SetCurve(RelativePath, BindingType, PropertyName, m_Curve);
        }

        void Flush()
        {
            m_Sampler.Flush();
            m_TangentUpdater.Flush();
            m_Reducer.Flush();

            Sample();
        }

        void Sample()
        {
            while (m_Sampler.MoveNext())
            {
                var sample = m_Sampler.Current;

                m_TangentUpdater.Add(new Keyframe(sample.Time, sample.Value));
            }

            while (m_TangentUpdater.MoveNext())
            {
                m_Reducer.Add(m_TangentUpdater.Current);
            }

            while (m_Reducer.MoveNext())
            {
                AddKey(m_Curve, m_Reducer.Current);
            }
        }

        internal static void AddKey(AnimationCurve curve, Keyframe keyframe)
        {
            curve.AddKey(keyframe);
        }
    }
}
