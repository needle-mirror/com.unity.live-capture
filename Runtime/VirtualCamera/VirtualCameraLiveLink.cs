using System;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The virtual camera data that can be recorded in a take and played back.
    /// </summary>
    [Flags]
    enum VirtualCameraChannelFlags
    {
        [Description("No channel recorded.")]
        None = 0,

        [Description("Record position.")]
        Position = 1 << 0,

        [Description("Record rotation.")]
        Rotation = 1 << 1,

        [Description("Record focal length.")]
        FocalLength = 1 << 2,

        [Description("Record focus distance.")]
        FocusDistance = 1 << 3,

        [Description("Record aperture.")]
        Aperture = 1 << 4,

        [Description("Record all supported channels.")]
        All = ~0
    }

    /// <summary>
    /// A job that can apply selected channels from a take to a virtual camera actor.
    /// </summary>
    struct VirtualCameraLiveLinkJob : IAnimationJob
    {
        public TransformStreamHandle transformHandle;
        public PropertyStreamHandle focalLengthHandle;
        public PropertyStreamHandle focalLengthRangeMinHandle;
        public PropertyStreamHandle focalLengthRangeMaxHandle;
        public PropertyStreamHandle focusDistanceHandle;
        public PropertyStreamHandle focusDistanceRangeMinHandle;
        public PropertyStreamHandle focusDistanceRangeMaxHandle;
        public PropertyStreamHandle apertureHandle;
        public PropertyStreamHandle apertureRangeMinHandle;
        public PropertyStreamHandle apertureRangeMaxHandle;
        public PropertyStreamHandle lensShiftXHandle;
        public PropertyStreamHandle lensShiftYHandle;
        public PropertyStreamHandle bladeCountHandle;
        public PropertyStreamHandle curvatureXHandle;
        public PropertyStreamHandle curvatureYHandle;
        public PropertyStreamHandle barrelClippingHandle;
        public PropertyStreamHandle anamorphismHandle;
        public PropertyStreamHandle sensorSizeXHandle;
        public PropertyStreamHandle sensorSizeYHandle;
        public PropertyStreamHandle isoHandle;
        public PropertyStreamHandle shutterSpeedHandle;
        public PropertyStreamHandle depthOfFieldEnabledHandle;
        public VirtualCameraChannelFlags channels;
        public Vector3 position;
        public Quaternion rotation;
        public Lens lens;
        public CameraBody cameraBody;
        public bool depthOfFieldEnabled;

        public void ProcessRootMotion(AnimationStream stream)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                transformHandle.SetLocalPosition(stream, position);
            }

            if (channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                transformHandle.SetLocalRotation(stream, rotation);
            }
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            if (channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
            {
                focalLengthHandle.SetFloat(stream, lens.focalLength);
                focalLengthRangeMinHandle.SetFloat(stream, lens.focalLengthRange.x);
                focalLengthRangeMaxHandle.SetFloat(stream, lens.focalLengthRange.y);
            }

            if (channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                depthOfFieldEnabledHandle.SetBool(stream, depthOfFieldEnabled);
                focusDistanceHandle.SetFloat(stream, lens.focusDistance);
                focusDistanceRangeMinHandle.SetFloat(stream, lens.focusDistanceRange.x);
                focusDistanceRangeMaxHandle.SetFloat(stream, lens.focusDistanceRange.y);
            }

            if (channels.HasFlag(VirtualCameraChannelFlags.Aperture))
            {
                apertureHandle.SetFloat(stream, lens.aperture);
                apertureRangeMinHandle.SetFloat(stream, lens.apertureRange.x);
                apertureRangeMaxHandle.SetFloat(stream, lens.apertureRange.y);
            }

            lensShiftXHandle.SetFloat(stream, lens.lensShift.x);
            lensShiftYHandle.SetFloat(stream, lens.lensShift.y);
            bladeCountHandle.SetInt(stream, lens.bladeCount);
            curvatureXHandle.SetFloat(stream, lens.curvature.x);
            curvatureYHandle.SetFloat(stream, lens.curvature.y);
            barrelClippingHandle.SetFloat(stream, lens.barrelClipping);
            anamorphismHandle.SetFloat(stream, lens.anamorphism);
            sensorSizeXHandle.SetFloat(stream, cameraBody.sensorSize.x);
            sensorSizeYHandle.SetFloat(stream, cameraBody.sensorSize.y);
            isoHandle.SetInt(stream, cameraBody.iso);
            shutterSpeedHandle.SetFloat(stream, cameraBody.shutterSpeed);
        }
    }

    /// <summary>
    /// The live link used by <see cref="VirtualCameraDevice"/>.
    /// </summary>
    [Serializable]
    class VirtualCameraLiveLink : LiveLink<VirtualCameraLiveLinkJob>
    {
        /// <summary>
        /// Sets the channels that the live link will output to.
        /// </summary>
        [EnumFlagButtonGroup(100f)]
        public VirtualCameraChannelFlags channels = VirtualCameraChannelFlags.All;

        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public Lens lens { get; set; }
        public CameraBody cameraBody { get; set; }
        public bool depthOfFieldEnabled { get; set; }

        /// <inheritdoc/>
        protected override VirtualCameraLiveLinkJob CreateAnimationJob(Animator animator)
        {
            if (animator == null)
                throw new ArgumentException($"Trying to create a {nameof(VirtualCameraLiveLink)} using a null {nameof(Animator)}");

            var transform = animator.transform;
            var animationJob = new VirtualCameraLiveLinkJob();

            animationJob.position = position;
            animationJob.rotation = rotation;
            animationJob.lens = lens;
            animationJob.cameraBody = cameraBody;
            animationJob.depthOfFieldEnabled = depthOfFieldEnabled;

            animationJob.transformHandle = animator.BindStreamTransform(transform);

            animationJob.focalLengthHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.focalLength");
            animationJob.focalLengthRangeMinHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.focalLengthRange.x");
            animationJob.focalLengthRangeMaxHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.focalLengthRange.y");

            animationJob.focusDistanceHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.focusDistance");
            animationJob.focusDistanceRangeMinHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.focusDistanceRange.x");
            animationJob.focusDistanceRangeMaxHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.focusDistanceRange.y");

            animationJob.apertureHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.aperture");
            animationJob.apertureRangeMinHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.apertureRange.x");
            animationJob.apertureRangeMaxHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.apertureRange.y");

            animationJob.lensShiftXHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.lensShift.x");
            animationJob.lensShiftYHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.lensShift.y");
            animationJob.bladeCountHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.bladeCount");
            animationJob.curvatureXHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.curvature.x");
            animationJob.curvatureYHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.curvature.y");
            animationJob.barrelClippingHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.barrelClipping");
            animationJob.anamorphismHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.anamorphism");
            animationJob.sensorSizeXHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.sensorSize.x");
            animationJob.sensorSizeYHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.sensorSize.y");
            animationJob.isoHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.iso");
            animationJob.shutterSpeedHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.shutterSpeed");

            animationJob.depthOfFieldEnabledHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_DepthOfField");

            return animationJob;
        }

        /// <inheritdoc/>
        protected override VirtualCameraLiveLinkJob Update(VirtualCameraLiveLinkJob data)
        {
            data.channels = channels;
            data.position = position;
            data.rotation = rotation;
            data.lens = lens;
            data.cameraBody = cameraBody;
            data.depthOfFieldEnabled = depthOfFieldEnabled;

            return data;
        }
    }
}
