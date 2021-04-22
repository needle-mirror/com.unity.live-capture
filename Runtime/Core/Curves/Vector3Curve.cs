using System;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A type of <see cref="ICurve"/> that stores keyframes of type <see cref="Vector3"/>.
    /// </summary>
    public class Vector3Curve : ICurve<Vector3>
    {
        readonly ICurve<float>[] m_Curves;

        /// <inheritdoc/>
        public string relativePath { get; private set; }

        /// <inheritdoc/>
        public string propertyName { get; private set; }

        /// <inheritdoc/>
        public Type bindingType { get; private set; }

        /// <inheritdoc/>
        public FrameRate frameRate
        {
            get => m_Curves[0].frameRate;
            set
            {
                foreach (var curve in m_Curves)
                {
                    curve.frameRate = value;
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
            this.relativePath = relativePath;
            this.propertyName = propertyName;
            this.bindingType = bindingType;

            m_Curves = new[]
            {
                new FloatCurve(relativePath, $"{propertyName}.x", bindingType),
                new FloatCurve(relativePath, $"{propertyName}.y", bindingType),
                new FloatCurve(relativePath, $"{propertyName}.z", bindingType),
            };
        }

        /// <inheritdoc/>
        public void AddKey(float time, Vector3 value)
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
