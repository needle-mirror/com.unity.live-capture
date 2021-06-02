using System;
using UnityEngine;
using UnityEngine.Animations;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The virtual camera data that can be recorded in a take and played back.
    /// </summary>
    [Flags]
    enum VirtualCameraChannelFlags : int
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
        public TransformStreamHandle TransformHandle;
        public PropertyStreamHandle FocalLengthHandle;
        public PropertyStreamHandle FocalLengthRangeMinHandle;
        public PropertyStreamHandle FocalLengthRangeMaxHandle;
        public PropertyStreamHandle FocusDistanceHandle;
        public PropertyStreamHandle CloseFocusDistanceHandle;
        public PropertyStreamHandle ApertureHandle;
        public PropertyStreamHandle ApertureRangeMinHandle;
        public PropertyStreamHandle ApertureRangeMaxHandle;
        public PropertyStreamHandle LensShiftXHandle;
        public PropertyStreamHandle LensShiftYHandle;
        public PropertyStreamHandle BladeCountHandle;
        public PropertyStreamHandle CurvatureXHandle;
        public PropertyStreamHandle CurvatureYHandle;
        public PropertyStreamHandle BarrelClippingHandle;
        public PropertyStreamHandle AnamorphismHandle;
        public PropertyStreamHandle SensorSizeXHandle;
        public PropertyStreamHandle SensorSizeYHandle;
        public PropertyStreamHandle IsoHandle;
        public PropertyStreamHandle ShutterSpeedHandle;
        public PropertyStreamHandle DepthOfFieldEnabledHandle;
        public PropertyStreamHandle CropAspectHandle;

        public VirtualCameraChannelFlags Channels;
        public Vector3 Position;
        public Quaternion Rotation;
        public Lens Lens;
        public LensIntrinsics LensIntrinsics;
        public CameraBody CameraBody;
        public bool DepthOfFieldEnabled;
        public float CropAspect;

        public void ProcessRootMotion(AnimationStream stream)
        {
            if (Channels.HasFlag(VirtualCameraChannelFlags.Position))
            {
                TransformHandle.SetLocalPosition(stream, Position);
            }

            if (Channels.HasFlag(VirtualCameraChannelFlags.Rotation))
            {
                TransformHandle.SetLocalRotation(stream, Rotation);
            }
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            if (Channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
            {
                FocalLengthHandle.SetFloat(stream, Lens.FocalLength);
                FocalLengthRangeMinHandle.SetFloat(stream, LensIntrinsics.FocalLengthRange.x);
                FocalLengthRangeMaxHandle.SetFloat(stream, LensIntrinsics.FocalLengthRange.y);
            }

            if (Channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                DepthOfFieldEnabledHandle.SetBool(stream, DepthOfFieldEnabled);
                FocusDistanceHandle.SetFloat(stream, Lens.FocusDistance);
                CloseFocusDistanceHandle.SetFloat(stream, LensIntrinsics.CloseFocusDistance);
            }

            if (Channels.HasFlag(VirtualCameraChannelFlags.Aperture))
            {
                ApertureHandle.SetFloat(stream, Lens.Aperture);
                ApertureRangeMinHandle.SetFloat(stream, LensIntrinsics.ApertureRange.x);
                ApertureRangeMaxHandle.SetFloat(stream, LensIntrinsics.ApertureRange.y);
            }

            LensShiftXHandle.SetFloat(stream, LensIntrinsics.LensShift.x);
            LensShiftYHandle.SetFloat(stream, LensIntrinsics.LensShift.y);
            BladeCountHandle.SetInt(stream, LensIntrinsics.BladeCount);
            CurvatureXHandle.SetFloat(stream, LensIntrinsics.Curvature.x);
            CurvatureYHandle.SetFloat(stream, LensIntrinsics.Curvature.y);
            BarrelClippingHandle.SetFloat(stream, LensIntrinsics.BarrelClipping);
            AnamorphismHandle.SetFloat(stream, LensIntrinsics.Anamorphism);
            SensorSizeXHandle.SetFloat(stream, CameraBody.SensorSize.x);
            SensorSizeYHandle.SetFloat(stream, CameraBody.SensorSize.y);
            IsoHandle.SetInt(stream, CameraBody.Iso);
            ShutterSpeedHandle.SetFloat(stream, CameraBody.ShutterSpeed);
            CropAspectHandle.SetFloat(stream, CropAspect);
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
        public VirtualCameraChannelFlags Channels = VirtualCameraChannelFlags.All;

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public Lens Lens { get; set; }
        public LensIntrinsics LensIntrinsics { get; set; }
        public CameraBody CameraBody { get; set; }
        public bool DepthOfFieldEnabled { get; set; }
        public float CropAspect { get; set; }

        /// <inheritdoc/>
        protected override VirtualCameraLiveLinkJob CreateAnimationJob(Animator animator)
        {
            if (animator == null)
                throw new ArgumentException($"Trying to create a {nameof(VirtualCameraLiveLink)} using a null {nameof(Animator)}");

            var transform = animator.transform;
            var animationJob = new VirtualCameraLiveLinkJob();

            animationJob.Position = Position;
            animationJob.Rotation = Rotation;
            animationJob.Lens = Lens;
            animationJob.CameraBody = CameraBody;
            animationJob.DepthOfFieldEnabled = DepthOfFieldEnabled;

            animationJob.TransformHandle = animator.BindStreamTransform(transform);

            animationJob.FocalLengthHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.m_FocalLength");
            animationJob.FocalLengthRangeMinHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_FocalLengthRange.x");
            animationJob.FocalLengthRangeMaxHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_FocalLengthRange.y");

            animationJob.FocusDistanceHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.m_FocusDistance");
            animationJob.CloseFocusDistanceHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_CloseFocusDistance");

            animationJob.ApertureHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_Lens.m_Aperture");
            animationJob.ApertureRangeMinHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_ApertureRange.x");
            animationJob.ApertureRangeMaxHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_ApertureRange.y");

            animationJob.LensShiftXHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_LensShift.x");
            animationJob.LensShiftYHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_LensShift.y");
            animationJob.BladeCountHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_BladeCount");
            animationJob.CurvatureXHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_Curvature.x");
            animationJob.CurvatureYHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_Curvature.y");
            animationJob.BarrelClippingHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_BarrelClipping");
            animationJob.AnamorphismHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_LensIntrinsics.m_Anamorphism");
            animationJob.SensorSizeXHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.m_SensorSize.x");
            animationJob.SensorSizeYHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.m_SensorSize.y");
            animationJob.IsoHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.m_Iso");
            animationJob.ShutterSpeedHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CameraBody.m_ShutterSpeed");

            animationJob.DepthOfFieldEnabledHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_DepthOfField");

            animationJob.CropAspectHandle = animator.BindStreamProperty(transform, typeof(VirtualCameraActor), "m_CropAspect");

            return animationJob;
        }

        /// <inheritdoc/>
        protected override VirtualCameraLiveLinkJob Update(VirtualCameraLiveLinkJob data)
        {
            data.Channels = Channels;
            data.Position = Position;
            data.Rotation = Rotation;
            data.Lens = Lens;
            data.LensIntrinsics = LensIntrinsics;
            data.CameraBody = CameraBody;
            data.DepthOfFieldEnabled = DepthOfFieldEnabled;
            data.CropAspect = CropAspect;

            return data;
        }
    }
}
