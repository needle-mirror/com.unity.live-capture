using System;
using System.Linq;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A class used to bake <see cref="FaceBlendShapePose"/> keyframes into a take.
    /// </summary>
    class FaceBlendShapeCurves : ICurve<FaceBlendShapePose>
    {
        readonly ICurve<float>[] m_Curves;

        /// <inheritdoc/>
        public string relativePath { get; }

        /// <inheritdoc/>
        public string propertyName { get; }

        /// <inheritdoc/>
        public Type bindingType { get; }

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
        /// Creates a new <see cref="FaceBlendShapePose"/> instance.
        /// </summary>
        /// <param name="relativePath">The path of the game object this curve applies to,
        /// relative to the game object the actor component is attached to.</param>
        /// <param name="propertyName">The name or path to the property that is animated.</param>
        /// <param name="bindingType">The type of component this curve is applied to.</param>
        public FaceBlendShapeCurves(string relativePath, string propertyName, Type bindingType)
        {
            this.relativePath = relativePath;
            this.propertyName = propertyName;
            this.bindingType = bindingType;

            m_Curves = FaceBlendShapePose.shapes
                .Select(shape => new FloatCurve(relativePath, $"{propertyName}.{shape}", bindingType))
                .ToArray();
        }

        /// <inheritdoc/>
        public void AddKey(float time, FaceBlendShapePose value)
        {
            AddKey(time, ref value);
        }

        /// <inheritdoc cref="AddKey(float,Unity.LiveCapture.ARKitFaceCapture.FaceBlendShapePose)"/>
        public void AddKey(float time, ref FaceBlendShapePose value)
        {
            for (var i = 0; i < FaceBlendShapePose.shapeCount; ++i)
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
