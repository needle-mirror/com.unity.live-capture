using System;
using System.Linq;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A class used to bake <see cref="FaceBlendShapePose"/> keyframes into a take.
    /// </summary>
    class FaceBlendShapeCurves : ICurve<FaceBlendShapePose>, IReduceableCurve
    {
        readonly FloatCurve[] m_Curves;

        /// <inheritdoc/>
        public float MaxError
        {
            get => m_Curves[0].MaxError;
            set
            {
                foreach (var curve in m_Curves)
                {
                    curve.MaxError = value;
                }
            }
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
        /// Creates a new <see cref="FaceBlendShapePose"/> instance.
        /// </summary>
        /// <param name="relativePath">The path of the game object this curve applies to,
        /// relative to the game object the actor component is attached to.</param>
        /// <param name="propertyName">The name or path to the property that is animated.</param>
        /// <param name="bindingType">The type of component this curve is applied to.</param>
        public FaceBlendShapeCurves(string relativePath, string propertyName, Type bindingType)
        {
            RelativePath = relativePath;
            PropertyName = propertyName;
            BindingType = bindingType;

            m_Curves = FaceBlendShapePose.Shapes
                .Select(shape => new FloatCurve(relativePath, $"{propertyName}.{shape}", bindingType))
                .ToArray();
        }

        /// <inheritdoc/>
        public void AddKey(double time, FaceBlendShapePose value)
        {
            AddKey(time, ref value);
        }

        /// <inheritdoc cref="AddKey(double,Unity.LiveCapture.ARKitFaceCapture.FaceBlendShapePose)"/>
        public void AddKey(double time, ref FaceBlendShapePose value)
        {
            for (var i = 0; i < FaceBlendShapePose.ShapeCount; ++i)
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
