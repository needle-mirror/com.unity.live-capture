using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// The face capture data that can be recorded in a take and played back.
    /// </summary>
    [Flags]
    enum FaceChannelFlags
    {
        /// <summary>
        /// The flags used to indicate no channels.
        /// </summary>
        [Description("No channel recorded.")]
        None = 0,

        /// <summary>
        /// The channel used for the blend shape pose of the face.
        /// </summary>
        [Description("Record face blend shapes.")]
        BlendShapes = 1 << 0,

        /// <summary>
        /// The channel used for the position of the head.
        /// </summary>
        [Description("Record head position.")]
        HeadPosition = 1 << 1,

        /// <summary>
        /// The channel used for the orientation of the head.
        /// </summary>
        [Description("Record head orientation.")]
        HeadRotation = 1 << 2,

        /// <summary>
        /// The channel used for the orientations of the eyes.
        /// </summary>
        [Description("Record eye orientation.")]
        Eyes = 1 << 3,

        /// <summary>
        /// The flags used to indicate all channels.
        /// </summary>
        [Description("Record all supported channels.")]
        All = ~0,
    }

    /// <summary>
    /// Handle for a rotation property on an object in the AnimationStream.
    /// </summary>
    struct Vector3StreamHandle
    {
        PropertyStreamHandle m_X;
        PropertyStreamHandle m_Y;
        PropertyStreamHandle m_Z;

        /// <summary>
        /// Creates a new <see cref="Vector3StreamHandle"/> instance.
        /// </summary>
        /// <param name="animator">The Animator instance that calls this method.</param>
        /// <param name="transform">The Transform to target.</param>
        /// <param name="type">The Component type.</param>
        /// <param name="property">The property to bind.</param>
        public Vector3StreamHandle(Animator animator, Transform transform, Type type, string property)
        {
            m_X = animator.BindStreamProperty(transform, type, $"{property}.x");
            m_Y = animator.BindStreamProperty(transform, type, $"{property}.y");
            m_Z = animator.BindStreamProperty(transform, type, $"{property}.z");
        }

        /// <summary>
        /// Sets the position property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new rotation property value.</param>
        public void SetValue(AnimationStream stream, Vector3 value)
        {
            m_X.SetFloat(stream, value.x);
            m_Y.SetFloat(stream, value.y);
            m_Z.SetFloat(stream, value.z);
        }

        /// <summary>
        /// Sets the quaternion property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new rotation property value.</param>
        public void SetValue(AnimationStream stream, Quaternion value)
        {
            var euler = value.eulerAngles;
            m_X.SetFloat(stream, euler.x);
            m_Y.SetFloat(stream, euler.y);
            m_Z.SetFloat(stream, euler.z);
        }
    }

    /// <summary>
    /// A job that can apply selected channels from a take to a face actor.
    /// </summary>
    struct FaceLiveLinkJob : IAnimationJob
    {
        public FaceChannelFlags EnabledChannels;
        public FacePose Pose;

        public NativeArray<PropertyStreamHandle> BlendShapesHandles;
        public Vector3StreamHandle HeadPositionHandle;
        public Vector3StreamHandle HeadOrientationHandle;
        public Vector3StreamHandle LeftEyeOrientationHandle;
        public Vector3StreamHandle RightEyeOrientationHandle;
        public PropertyStreamHandle BlendShapesEnabledHandle;
        public PropertyStreamHandle HeadPositionEnabledHandle;
        public PropertyStreamHandle HeadOrientationEnabledHandle;
        public PropertyStreamHandle EyeOrientationEnabledHandle;

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        /// <inheritdoc/>
        public void ProcessAnimation(AnimationStream stream)
        {
            if (EnabledChannels.HasFlag(FaceChannelFlags.BlendShapes))
            {
                for (var i = 0; i < BlendShapesHandles.Length; i++)
                {
                    BlendShapesHandles[i].SetFloat(stream, Pose.BlendShapes[i]);
                }
            }
            if (EnabledChannels.HasFlag(FaceChannelFlags.HeadPosition))
            {
                HeadPositionHandle.SetValue(stream, Pose.HeadPosition);
            }
            if (EnabledChannels.HasFlag(FaceChannelFlags.HeadRotation))
            {
                HeadOrientationHandle.SetValue(stream, Pose.HeadOrientation);
            }
            if (EnabledChannels.HasFlag(FaceChannelFlags.Eyes))
            {
                LeftEyeOrientationHandle.SetValue(stream, Pose.LeftEyeOrientation);
                RightEyeOrientationHandle.SetValue(stream, Pose.RightEyeOrientation);
            }

            BlendShapesEnabledHandle.SetBool(stream, EnabledChannels.HasFlag(FaceChannelFlags.BlendShapes));
            HeadPositionEnabledHandle.SetBool(stream, EnabledChannels.HasFlag(FaceChannelFlags.HeadPosition));
            HeadOrientationEnabledHandle.SetBool(stream, EnabledChannels.HasFlag(FaceChannelFlags.HeadRotation));
            EyeOrientationEnabledHandle.SetBool(stream, EnabledChannels.HasFlag(FaceChannelFlags.Eyes));
        }
    }

    [Serializable]
    class FaceLiveLink : LiveLink<FaceLiveLinkJob>, IDisposable
    {
        /// <summary>
        /// Sets the channels that the live link will output to.
        /// </summary>
        [EnumFlagButtonGroup(100f)]
        public FaceChannelFlags Channels = FaceChannelFlags.All;

        /// <summary>
        /// The pose to apply using the live link.
        /// </summary>
        public FacePose Pose { get; set; }

        NativeArray<PropertyStreamHandle> m_FaceBlendShapesHandles;

        public void Initialize()
        {
            if (!m_FaceBlendShapesHandles.IsCreated)
            {
                m_FaceBlendShapesHandles = new NativeArray<PropertyStreamHandle>(FaceBlendShapePose.ShapeCount, Allocator.Persistent);
            }
        }

        public void Dispose()
        {
            if (m_FaceBlendShapesHandles.IsCreated)
            {
                m_FaceBlendShapesHandles.Dispose();
                m_FaceBlendShapesHandles = default;
            }
        }

        /// <inheritdoc/>
        protected override FaceLiveLinkJob CreateAnimationJob(Animator animator)
        {
            if (animator == null)
                throw new ArgumentNullException(nameof(animator));

            var transform = animator.transform;

            for (var i = 0; i < m_FaceBlendShapesHandles.Length; i++)
            {
                m_FaceBlendShapesHandles[i] = animator.BindStreamProperty(transform, typeof(FaceActor), $"{FaceActor.PropertyNames.BlendShapes}.{FaceBlendShapePose.Shapes[i]}");
            }

            return new FaceLiveLinkJob
            {
                BlendShapesHandles = m_FaceBlendShapesHandles,
                HeadPositionHandle = new Vector3StreamHandle(animator, transform, typeof(FaceActor), FaceActor.PropertyNames.HeadPosition),
                HeadOrientationHandle = new Vector3StreamHandle(animator, transform, typeof(FaceActor), FaceActor.PropertyNames.HeadOrientation),
                LeftEyeOrientationHandle = new Vector3StreamHandle(animator, transform, typeof(FaceActor), FaceActor.PropertyNames.LeftEyeOrientation),
                RightEyeOrientationHandle = new Vector3StreamHandle(animator, transform, typeof(FaceActor), FaceActor.PropertyNames.RightEyeOrientation),
                BlendShapesEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), FaceActor.PropertyNames.BlendShapesEnabled),
                HeadPositionEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), FaceActor.PropertyNames.HeadPositionEnabled),
                HeadOrientationEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), FaceActor.PropertyNames.HeadOrientationEnabled),
                EyeOrientationEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), FaceActor.PropertyNames.EyeOrientationEnabled),
            };
        }

        /// <inheritdoc/>
        protected override FaceLiveLinkJob Update(FaceLiveLinkJob data)
        {
            data.EnabledChannels = Channels;
            data.Pose = Pose;
            return data;
        }
    }
}
