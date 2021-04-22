using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A class that records <see cref="FacePose"/> samples.
    /// </summary>
    class FaceDeviceRecorder
    {
        ICurve[] m_Curves =
        {
            new FaceBlendShapeCurves(string.Empty, $"m_Pose.{nameof(FacePose.blendShapes)}", typeof(FaceActor)),
            new Vector4Curve(string.Empty, $"m_Pose.{nameof(FacePose.headOrientation)}", typeof(FaceActor)),
            new Vector4Curve(string.Empty, $"m_Pose.{nameof(FacePose.leftEyeOrientation)}", typeof(FaceActor)),
            new Vector4Curve(string.Empty, $"m_Pose.{nameof(FacePose.rightEyeOrientation)}", typeof(FaceActor)),
            new BooleanCurve(string.Empty, "m_BlendShapesEnabled", typeof(FaceActor)),
            new BooleanCurve(string.Empty, "m_HeadOrientationEnabled", typeof(FaceActor)),
            new BooleanCurve(string.Empty, "m_EyeOrientationEnabled", typeof(FaceActor)),
        };

        /// <summary>
        /// The time used by the recorder in seconds.
        /// </summary>
        public float time { get; set; }

        /// <summary>
        /// The data channels to record as enum flags.
        /// </summary>
        public FaceChannelFlags channels { get; set; }

        /// <summary>
        /// The frame rate to use for recording.
        /// </summary>
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
        /// Checks if the recording contains no recorded samples.
        /// </summary>
        /// <returns>
        /// true if the recording contains no samples; otherwise, false.
        /// </returns>
        public bool IsEmpty()
        {
            foreach (var curve in m_Curves)
            {
                if (!curve.IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes all recorded samples.
        /// </summary>
        public void Clear()
        {
            foreach (var curve in m_Curves)
            {
                curve.Clear();
            }
        }

        /// <summary>
        /// Records a <see cref="FacePose"/> sample.
        /// </summary>
        /// <param name="sample">The face pose sample to record</param>
        public void Record(ref FacePose sample)
        {
            if (channels.HasFlag(FaceChannelFlags.BlendShapes))
            {
                (GetCurve<FaceBlendShapePose>(0) as FaceBlendShapeCurves).AddKey(time, ref sample.blendShapes);
            }
            if (channels.HasFlag(FaceChannelFlags.Head))
            {
                GetCurve<Vector4>(1).AddKey(time, ToVector4(sample.headOrientation));
            }
            if (channels.HasFlag(FaceChannelFlags.Eyes))
            {
                GetCurve<Vector4>(2).AddKey(time, ToVector4(sample.leftEyeOrientation));
                GetCurve<Vector4>(3).AddKey(time, ToVector4(sample.rightEyeOrientation));
            }

            GetCurve<bool>(4).AddKey(time, channels.HasFlag(FaceChannelFlags.BlendShapes));
            GetCurve<bool>(5).AddKey(time, channels.HasFlag(FaceChannelFlags.Head));
            GetCurve<bool>(6).AddKey(time, channels.HasFlag(FaceChannelFlags.Eyes));
        }

        /// <summary>
        /// Produces an AnimationClip from the recording.
        /// </summary>
        /// <returns>
        /// An AnimationClip created from the recording.
        /// </returns>
        public AnimationClip Bake()
        {
            var animationClip = new AnimationClip();

            foreach (var curve in m_Curves)
            {
                curve.SetToAnimationClip(animationClip);
            }

            return animationClip;
        }

        ICurve<T> GetCurve<T>(int index)
        {
            return m_Curves[index] as ICurve<T>;
        }

        Vector4 ToVector4(Quaternion q)
        {
            return new Vector4(q.x, q.y, q.z, q.w);
        }
    }
}
