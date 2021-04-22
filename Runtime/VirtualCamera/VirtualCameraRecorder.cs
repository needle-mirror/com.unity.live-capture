using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    class VirtualCameraRecorder
    {
        ICurve[] m_Curves =
        {
            new Vector3Curve(string.Empty, "m_LocalPosition", typeof(Transform)),
            new EulerCurve(string.Empty, "m_LocalEuler", typeof(Transform)),
            new FloatCurve(string.Empty, "m_Lens.focalLength", typeof(VirtualCameraActor)),
            new FloatCurve(string.Empty, "m_Lens.focusDistance", typeof(VirtualCameraActor)),
            new FloatCurve(string.Empty, "m_Lens.aperture", typeof(VirtualCameraActor)),
            new BooleanCurve(string.Empty, "m_DepthOfField", typeof(VirtualCameraActor))
        };

        /// <summary>
        /// The time used by the recorder in seconds.
        /// </summary>
        public float time { get; set; }

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
        /// The data channels to record as enum flags.
        /// </summary>
        public VirtualCameraChannelFlags channels { get; set; }

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
        /// Records a position sample.
        /// </summary>
        /// <param name="sample">The position sample to record</param>
        public void RecordPosition(Vector3 sample)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                GetCurve<Vector3>(0).AddKey(time, sample);
            }
        }

        /// <summary>
        /// Records a rotation sample.
        /// </summary>
        /// <param name="sample">The rotation sample to record</param>
        public void RecordRotation(Quaternion sample)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                GetCurve<Quaternion>(1).AddKey(time, sample);
            }
        }

        /// <summary>
        /// Records a focal length sample.
        /// </summary>
        /// <param name="sample">The focal length sample to record</param>
        public void RecordFocalLength(float sample)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
            {
                GetCurve<float>(2).AddKey(time, sample);
            }
        }

        /// <summary>
        /// Records a focus distance sample.
        /// </summary>
        /// <param name="sample">The focus distance sample to record</param>
        public void RecordFocusDistance(float sample)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                GetCurve<float>(3).AddKey(time, sample);
            }
        }

        /// <summary>
        /// Records an aperture sample.
        /// </summary>
        /// <param name="sample">The aperture sample to record</param>
        public void RecordAperture(float sample)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.Aperture))
            {
                GetCurve<float>(4).AddKey(time, sample);
            }
        }

        /// <summary>
        /// Records the enabled state of the depth of field.
        /// </summary>
        /// <param name="sample">The enabled state to record</param>
        public void RecordEnableDepthOfField(bool sample)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                GetCurve<bool>(5).AddKey(time, sample);
            }
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
