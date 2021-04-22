using System;
using UnityEngine;
#if UNITY_EDITOR
using Unity.LiveCapture.Internal;
#endif

namespace Unity.LiveCapture
{
    /// <summary>
    /// A type of <see cref="ICurve"/> that stores keyframes of type <see cref="Quaternion"/> as Euler angles.
    /// </summary>
    public class EulerCurve : ICurve<Quaternion>
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

        bool m_First = true;
        Vector3 m_LastEuler;

        /// <summary>
        /// Creates a new <see cref="EulerCurve"/> instance.
        /// </summary>
        /// <param name="relativePath">The path of the game object this curve applies to,
        /// relative to the game object the actor component is attached to.</param>
        /// <param name="propertyName">The name or path to the property that is animated.</param>
        /// <param name="bindingType">The type of component this curve is applied to.</param>
        public EulerCurve(string relativePath, string propertyName, Type bindingType)
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
        public void AddKey(float time, Quaternion value)
        {
            var euler = value.eulerAngles;

            if (m_First)
            {
                m_LastEuler = euler;
                m_First = false;
            }

            euler = MathUtility.ClosestEuler(value, m_LastEuler);
            m_LastEuler = euler;

            for (var i = 0; i < m_Curves.Length; ++i)
            {
                m_Curves[i].AddKey(time, euler[i]);
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
            m_First = true;

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
