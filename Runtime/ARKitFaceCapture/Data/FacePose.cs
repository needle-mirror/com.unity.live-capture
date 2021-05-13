using System;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// The data representing a captured face pose.
    /// </summary>
    [Serializable]
    struct FacePose
    {
        /// <summary>
        /// The neutral pose.
        /// </summary>
        public static FacePose Identity { get; } = new FacePose
        {
            BlendShapes = default,
            HeadPosition = Vector3.zero,
            HeadOrientation = Quaternion.identity,
            LeftEyeOrientation = Quaternion.identity,
            RightEyeOrientation = Quaternion.identity,
        };

        /// <summary>
        /// The blend shapes weights defining the face expression.
        /// </summary>
        public FaceBlendShapePose BlendShapes;

        /// <summary>
        /// The local position of the head transform.
        /// </summary>
        public Vector3 HeadPosition;

        /// <summary>
        /// The orientation of the head transform.
        /// </summary>
        public Quaternion HeadOrientation;

        /// <summary>
        /// The orientation of the left eye transform.
        /// </summary>
        public Quaternion LeftEyeOrientation;

        /// <summary>
        /// The orientation of the right eye transform.
        /// </summary>
        public Quaternion RightEyeOrientation;
    }
}
