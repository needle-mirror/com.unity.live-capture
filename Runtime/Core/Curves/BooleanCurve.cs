using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A type of <see cref="ICurve"/> that stores keyframes of type bool.
    /// </summary>
    public class BooleanCurve : ICurve<bool>
    {
        readonly FloatCurve m_Curve;

        /// <inheritdoc/>
        public string RelativePath { get; }

        /// <inheritdoc/>
        public string PropertyName { get; }

        /// <inheritdoc/>
        public Type BindingType { get; }

        /// <inheritdoc/>
        public FrameRate FrameRate
        {
            get => m_Curve.FrameRate;
            set => m_Curve.FrameRate = value;
        }

        int m_FrameNumber;

        /// <summary>
        /// Creates a new <see cref="BooleanCurve"/> instance.
        /// </summary>
        /// <param name="relativePath">The path of the game object this curve applies to,
        /// relative to the game object the actor component is attached to.</param>
        /// <param name="propertyName">The name or path to the property that is animated.</param>
        /// <param name="bindingType">The type of component this curve is applied to.</param>
        public BooleanCurve(string relativePath, string propertyName, Type bindingType)
        {
            RelativePath = relativePath;
            PropertyName = propertyName;
            BindingType = bindingType;

            m_Curve = new FloatCurve(relativePath, propertyName, bindingType);
        }

        /// <inheritdoc/>
        public void AddKey(double time, bool value)
        {
            m_Curve.AddKey(time, value ? 1f : 0f);

            MakeConstant();
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return m_Curve.IsEmpty();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            m_Curve.Clear();
            m_FrameNumber = 0;
        }

        /// <inheritdoc/>
        public void SetToAnimationClip(AnimationClip clip)
        {
            m_Curve.SetToAnimationClip(clip);
        }

        void MakeConstant()
        {
            var frameCount = m_Curve.AnimationCurve.length;

            for (var i = m_FrameNumber; i < frameCount; ++i)
            {
                var keyframe = m_Curve.AnimationCurve[i];

                keyframe.inTangent = float.PositiveInfinity;
                keyframe.outTangent = float.PositiveInfinity;

                m_Curve.AnimationCurve.MoveKey(i, keyframe);

                ++m_FrameNumber;
            }
        }
    }
}
