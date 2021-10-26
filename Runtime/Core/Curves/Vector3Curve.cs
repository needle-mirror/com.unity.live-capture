using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A type of <see cref="ICurve"/> that stores keyframes of type <see cref="Vector3"/>.
    /// </summary>
    public class Vector3Curve : ICurve<Vector3>
    {
        readonly ICurve<float>[] m_Curves;

        /// <inheritdoc/>
        public string RelativePath { get; }

        /// <inheritdoc/>
        public string PropertyName { get; }

        /// <inheritdoc/>
        public Type BindingType { get; }

        /// <inheritdoc/>
        public FrameRate FrameRate
        {
            get => m_Curves[0].FrameRate;
            set
            {
                foreach (var curve in m_Curves)
                {
                    curve.FrameRate = value;
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Vector3Curve"/> instance.
        /// </summary>
        /// <param name="relativePath">The path of the game object this curve applies to,
        /// relative to the game object the actor component is attached to.</param>
        /// <param name="propertyName">The name or path to the property that is animated.</param>
        /// <param name="bindingType">The type of component this curve is applied to.</param>
        public Vector3Curve(string relativePath, string propertyName, Type bindingType)
        {
            RelativePath = relativePath;
            PropertyName = propertyName;
            BindingType = bindingType;

            m_Curves = new[]
            {
                new FloatCurve(relativePath, $"{propertyName}.x", bindingType),
                new FloatCurve(relativePath, $"{propertyName}.y", bindingType),
                new FloatCurve(relativePath, $"{propertyName}.z", bindingType),
            };
        }

        /// <inheritdoc/>
        public void AddKey(double time, Vector3 value)
        {
            for (var i = 0; i < m_Curves.Length; ++i)
            {
                m_Curves[i].AddKey(time, value[i]);
            }
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return m_Curves[0].IsEmpty();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var curve in m_Curves)
            {
                curve.Clear();
            }
        }

        /// <inheritdoc/>
        public void SetToAnimationClip(AnimationClip clip)
        {
            foreach (var curve in m_Curves)
            {
                curve.SetToAnimationClip(clip);
            }
        }
    }
}
