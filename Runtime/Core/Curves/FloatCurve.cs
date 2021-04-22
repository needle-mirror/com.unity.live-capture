using System;
using System.Collections.Generic;
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
        public string relativePath { get; }

        /// <inheritdoc/>
        public string propertyName { get; }

        /// <inheritdoc/>
        public Type bindingType { get; }

        /// <inheritdoc/>
        public FrameRate frameRate
        {
            get => m_Sampler.frameRate;
            set => m_Sampler.frameRate = value;
        }

        /// <summary>
        /// The baked animation curve.
        /// </summary>
        internal AnimationCurve animationCurve => m_Curve;

        /// <summary>
        /// Constructs an instance of FloatCurve.
        /// </summary>
        /// <param name="relativePath"> Path to the game object this curve applies to.</param>
        /// <param name="propertyName"> The name or path to the property being animated.</param>
        /// <param name="bindingType"> The class type of the component that is animated.</param>
        public FloatCurve(string relativePath, string propertyName, Type bindingType)
        {
            this.relativePath = relativePath;
            this.propertyName = propertyName;
            this.bindingType = bindingType;
        }

        /// <inheritdoc/>
        public void AddKey(float time, float value)
        {
            m_Sampler.Add(new Keyframe(time, value));

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

            clip.SetCurve(relativePath, bindingType, propertyName, m_Curve);
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
