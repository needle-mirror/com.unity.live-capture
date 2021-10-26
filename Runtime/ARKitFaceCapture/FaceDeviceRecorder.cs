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
            new FaceBlendShapeCurves(string.Empty, FaceActor.PropertyNames.BlendShapes, typeof(FaceActor)),
            new Vector3Curve(string.Empty, FaceActor.PropertyNames.HeadPosition, typeof(FaceActor)),
            new EulerCurve(string.Empty, FaceActor.PropertyNames.HeadOrientation, typeof(FaceActor)),
            new EulerCurve(string.Empty, FaceActor.PropertyNames.LeftEyeOrientation, typeof(FaceActor)),
            new EulerCurve(string.Empty, FaceActor.PropertyNames.RightEyeOrientation, typeof(FaceActor)),
            new BooleanCurve(string.Empty, FaceActor.PropertyNames.BlendShapesEnabled, typeof(FaceActor)),
            new BooleanCurve(string.Empty, FaceActor.PropertyNames.HeadPositionEnabled, typeof(FaceActor)),
            new BooleanCurve(string.Empty, FaceActor.PropertyNames.HeadOrientationEnabled, typeof(FaceActor)),
            new BooleanCurve(string.Empty, FaceActor.PropertyNames.EyeOrientationEnabled, typeof(FaceActor)),
        };

        /// <summary>
        /// The time used by the recorder in seconds.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// The data channels to record as enum flags.
        /// </summary>
        public FaceChannelFlags Channels { get; set; }

        /// <summary>
        /// The frame rate to use for recording.
        /// </summary>
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
            if (Channels.HasFlag(FaceChannelFlags.BlendShapes))
            {
                (GetCurve<FaceBlendShapePose>(0) as FaceBlendShapeCurves).AddKey(Time, ref sample.BlendShapes);
            }
            if (Channels.HasFlag(FaceChannelFlags.HeadPosition))
            {
                GetCurve<Vector3>(1).AddKey(Time, sample.HeadPosition);
            }
            if (Channels.HasFlag(FaceChannelFlags.HeadRotation))
            {
                GetCurve<Quaternion>(2).AddKey(Time, sample.HeadOrientation);
            }
            if (Channels.HasFlag(FaceChannelFlags.Eyes))
            {
                GetCurve<Quaternion>(3).AddKey(Time, sample.LeftEyeOrientation);
                GetCurve<Quaternion>(4).AddKey(Time, sample.RightEyeOrientation);
            }

            GetCurve<bool>(5).AddKey(Time, Channels.HasFlag(FaceChannelFlags.BlendShapes));
            GetCurve<bool>(6).AddKey(Time, Channels.HasFlag(FaceChannelFlags.HeadPosition));
            GetCurve<bool>(7).AddKey(Time, Channels.HasFlag(FaceChannelFlags.HeadRotation));
            GetCurve<bool>(8).AddKey(Time, Channels.HasFlag(FaceChannelFlags.Eyes));
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
    }
}
