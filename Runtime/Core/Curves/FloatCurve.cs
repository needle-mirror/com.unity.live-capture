using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.LiveCapture
{
    /// <summary>
    /// A type of <see cref="ICurve"/> that stores keyframes of type float.
    /// </summary>
    public class FloatCurve : ICurve<float>
    {
        Sampler m_Sampler = new Sampler();
        AnimationCurve m_Curve = new AnimationCurve();

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
            m_Sampler.Add(new Keyframe((float)time, value));

            Sample();
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return m_Curve.length == 0;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            m_Sampler.Reset();
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

            Sample();
        }

        void Sample()
        {
            while (m_Sampler.MoveNext())
            {
                AddKey(m_Sampler.Current);
            }
        }

        void AddKey(Keyframe keyframe)
        {
            m_Curve.AddKey(keyframe);

            UpdateTangents();
        }

        void UpdateTangents()
        {
            var index = m_Curve.length - 1;

#if UNITY_EDITOR
            AnimationUtility.SetKeyLeftTangentMode(m_Curve, index, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(m_Curve, index, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyBroken(m_Curve, index, false);
#endif
            m_Curve.UpdateTangents(index - 1);
            m_Curve.UpdateTangents(index);
        }
    }
}
