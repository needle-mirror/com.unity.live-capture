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
        /// The channel used for the orientation of the head.
        /// </summary>
        [Description("Record head orientation.")]
        Head = 1 << 1,

        /// <summary>
        /// The channel used for the orientations of the eyes.
        /// </summary>
        [Description("Record eye orientation.")]
        Eyes = 1 << 2,

        /// <summary>
        /// The flags used to indicate all channels.
        /// </summary>
        [Description("Record all supported channels.")]
        All = ~0,
    }

    /// <summary>
    /// Handle for a <see cref="Quaternion"/> property on an object in the AnimationStream.
    /// </summary>
    struct RotationStreamHandle
    {
        PropertyStreamHandle m_RotationX;
        PropertyStreamHandle m_RotationY;
        PropertyStreamHandle m_RotationZ;
        PropertyStreamHandle m_RotationW;

        /// <summary>
        /// Creates a new <see cref="RotationStreamHandle"/> instance.
        /// </summary>
        /// <param name="animator">The Animator instance that calls this method.</param>
        /// <param name="transform">The Transform to target.</param>
        /// <param name="type">The Component type.</param>
        /// <param name="property">The property to bind.</param>
        public RotationStreamHandle(Animator animator, Transform transform, Type type, string property)
        {
            m_RotationX = animator.BindStreamProperty(transform, type, $"{property}.x");
            m_RotationY = animator.BindStreamProperty(transform, type, $"{property}.y");
            m_RotationZ = animator.BindStreamProperty(transform, type, $"{property}.z");
            m_RotationW = animator.BindStreamProperty(transform, type, $"{property}.w");
        }

        /// <summary>
        /// Sets the quaternion property value into a stream.
        /// </summary>
        /// <param name="stream">The AnimationStream that holds the animated values.</param>
        /// <param name="value">The new quaternion property value.</param>
        public void SetRotation(AnimationStream stream, Quaternion value)
        {
            m_RotationX.SetFloat(stream, value.x);
            m_RotationY.SetFloat(stream, value.y);
            m_RotationZ.SetFloat(stream, value.z);
            m_RotationW.SetFloat(stream, value.w);
        }
    }

    /// <summary>
    /// A job that can apply selected channels from a take to a face actor.
    /// </summary>
    struct FaceLiveLinkJob : IAnimationJob
    {
        public FaceChannelFlags enabledChannels;
        public FacePose facePose;

        public NativeArray<PropertyStreamHandle> faceBlendShapesHandles;
        public RotationStreamHandle headPoseHandle;
        public RotationStreamHandle leftEyeRotationHandle;
        public RotationStreamHandle rightEyeRotationHandle;
        public PropertyStreamHandle blendShapesEnabledHandle;
        public PropertyStreamHandle headOrientationEnabledHandle;
        public PropertyStreamHandle eyeOrientationEnabledHandle;

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        /// <inheritdoc/>
        public void ProcessAnimation(AnimationStream stream)
        {
            if (enabledChannels.HasFlag(FaceChannelFlags.BlendShapes))
            {
                for (var i = 0; i < faceBlendShapesHandles.Length; i++)
                {
                    faceBlendShapesHandles[i].SetFloat(stream, facePose.blendShapes[i]);
                }
            }
            if (enabledChannels.HasFlag(FaceChannelFlags.Head))
            {
                headPoseHandle.SetRotation(stream, facePose.headOrientation);
            }
            if (enabledChannels.HasFlag(FaceChannelFlags.Eyes))
            {
                leftEyeRotationHandle.SetRotation(stream, facePose.leftEyeOrientation);
                rightEyeRotationHandle.SetRotation(stream, facePose.rightEyeOrientation);
            }

            blendShapesEnabledHandle.SetBool(stream, enabledChannels.HasFlag(FaceChannelFlags.BlendShapes));
            headOrientationEnabledHandle.SetBool(stream, enabledChannels.HasFlag(FaceChannelFlags.Head));
            eyeOrientationEnabledHandle.SetBool(stream, enabledChannels.HasFlag(FaceChannelFlags.Eyes));
        }
    }

    [Serializable]
    class FaceLiveLink : LiveLink<FaceLiveLinkJob>, IDisposable
    {
        /// <summary>
        /// Sets the channels that the live link will output to.
        /// </summary>
        [EnumFlagButtonGroup(100f)]
        public FaceChannelFlags channels = FaceChannelFlags.All;

        /// <summary>
        /// The pose to apply using the live link.
        /// </summary>
        public FacePose pose { get; set; }

        NativeArray<PropertyStreamHandle> m_FaceBlendShapesHandles;

        public void Initialize()
        {
            if (!m_FaceBlendShapesHandles.IsCreated)
            {
                m_FaceBlendShapesHandles = new NativeArray<PropertyStreamHandle>(FaceBlendShapePose.shapeCount, Allocator.Persistent);
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
                var path = $"m_Pose.{nameof(FacePose.blendShapes)}.{FaceBlendShapePose.shapes[i]}";
                m_FaceBlendShapesHandles[i] = animator.BindStreamProperty(transform, typeof(FaceActor), path);
            }

            return new FaceLiveLinkJob
            {
                faceBlendShapesHandles = m_FaceBlendShapesHandles,
                headPoseHandle = new RotationStreamHandle(animator, transform, typeof(FaceActor), $"m_Pose.{nameof(FacePose.headOrientation)}"),
                leftEyeRotationHandle = new RotationStreamHandle(animator, transform, typeof(FaceActor), $"m_Pose.{nameof(FacePose.leftEyeOrientation)}"),
                rightEyeRotationHandle = new RotationStreamHandle(animator, transform, typeof(FaceActor), $"m_Pose.{nameof(FacePose.rightEyeOrientation)}"),
                blendShapesEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), $"m_BlendShapesEnabled"),
                headOrientationEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), $"m_HeadOrientationEnabled"),
                eyeOrientationEnabledHandle = animator.BindStreamProperty(transform, typeof(FaceActor), $"m_EyeOrientationEnabled"),
            };
        }

        /// <inheritdoc/>
        protected override FaceLiveLinkJob Update(FaceLiveLinkJob data)
        {
            data.enabledChannels = channels;
            data.facePose = pose;
            return data;
        }
    }
}
