using System;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// The data representing a captured face pose.
    /// </summary>
    [Serializable]
    public struct FacePose
    {
        /// <summary>
        /// The neutral pose.
        /// </summary>
        public static FacePose identity { get; } = new FacePose
        {
            blendShapes = default,
            headOrientation = Quaternion.identity,
            leftEyeOrientation = Quaternion.identity,
            rightEyeOrientation = Quaternion.identity
        };

        /// <summary>
        /// The blend shapes weights defining the face expression.
        /// </summary>
        public FaceBlendShapePose blendShapes;

        /// <summary>
        /// The orientation of the head transform.
        /// </summary>
        public Quaternion headOrientation;

        /// <summary>
        /// The orientation of the left eye transform.
        /// </summary>
        public Quaternion leftEyeOrientation;

        /// <summary>
        /// The orientation of the right eye transform.
        /// </summary>
        public Quaternion rightEyeOrientation;
    }
}
