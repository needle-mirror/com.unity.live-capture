using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The techniques which can be used to determine focus distance for a virtual camera.
    /// </summary>
    public enum FocusMode : byte
    {
        /// <summary>
        /// Depth of Field is disabled.
        /// </summary>
        [Description("Depth of Field is disabled.")]
        Disabled = 0,
        /// <summary>
        /// Focus distance is manually set by tapping the screen or manipulating the dial.
        /// </summary>
        [Description("Focus distance is manually set by tapping the screen or manipulating the dial.")]
        Manual = 1,
        /// <summary>
        /// Focus adjusts to keep in focus the 3D point under a movable screen-space reticle.
        /// </summary>
        [Description("Focus adjusts to keep in focus the 3D point under a movable screen-space reticle.")]
        Auto = 2,
        /// <summary>
        /// Focus adjusts to match a scene object's distance to the camera.
        /// </summary>
        [Description("Focus adjusts to match a scene object's distance to the camera.")]
        Spatial = 3,
    }

    /// <summary>
    /// A class containing extension method(s) for <see cref="FocusMode"/>.
    /// </summary>
    public static class FocusModeExtensions
    {
        /// <summary>
        /// Provides a user-friendly description of a Focus Mode value.
        /// </summary>
        /// <param name="mode">The focus mode.</param>
        /// <returns>The description string.</returns>
        public static string GetDescription(this FocusMode mode)
        {
            switch (mode)
            {
                case FocusMode.Disabled:
                    return "DoF Disabled";
                case FocusMode.Manual:
                    return "Manual";
                case FocusMode.Auto:
                    return "Auto";
                case FocusMode.Spatial:
                    return "Spatial";
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// A struct that transports the state of a virtual camera over the network.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraState : IEquatable<CameraState>
    {
        /// <summary>
        /// The default CameraState.
        /// </summary>
        public static readonly CameraState defaultData = new CameraState
        {
            damping = Damping.Default,
            rebasing = false,
            motionScale = Vector3.one,
            ergonomicTilt = 0,
            joystickSpeed = Vector3.one,
            pedestalSpace = Space.World,
        };

        /// <summary>
        /// Settings to configure the delay and smooth the motion of the position and rotation of the camera.
        /// </summary>
        [Tooltip("Settings to configure the delay and smooth the motion of the position and rotation of the camera.")]
        public Damping damping;

        /// <summary>
        /// Position Axis constraint settings.
        /// </summary>
        [Tooltip("Position Axis constraint settings.")]
        [EnumFlagButtonGroup(60f)]
        public PositionAxis positionLock;

        /// <summary>
        /// Rotation Axis constraint settings.
        /// </summary>
        [Tooltip("Rotation Axis constraint settings.")]
        [EnumFlagButtonGroup(60f)]
        public RotationAxis rotationLock;

        /// <summary>
        /// Force dutch rotation to be zero.
        /// </summary>
        [Tooltip("Force dutch rotation to be zero.")]
        public bool zeroDutch;

        /// <summary>
        /// The angle around the x-axis to offset the local camera rotation.
        /// </summary>
        [Tooltip("The angle around the x-axis to offset the local camera rotation.")]
        public float ergonomicTilt;

        /// <summary>
        /// Rebasing set to true indicates that the Virtual Camera is frozen in virtual space allowing the user
        /// to change position in real world space.
        /// </summary>
        [HideInInspector]
        public bool rebasing;

        /// <summary>
        /// Scale of the movement for each axis. A scale of (1, 1, 1) means that the virtual camera position will match the device position in real world.
        /// </summary>
        [Tooltip("Scale of the movement for each axis. A scale of (1, 1, 1) means that the virtual camera position will match the device position in real world.")]
        public Vector3 motionScale;

        /// <summary>
        /// Scaling applied to joystick motion.
        /// A scale of (1, 2, 1) means the joystick will translate the Y axis twice faster than the X and Z axis.
        /// </summary>
        [Tooltip("Scaling applied to joystick motion, A scale of (1, 2, 1) means the joystick will translate the Y axis twice faster than the X and Z axis.")]
        public Vector3 joystickSpeed;

        /// <summary>
        /// The space on which the joystick is moving.
        /// World space will translate the rig pedestal relative to the world axis.
        /// Self space will translate the rig relative to the camera's look direction.
        /// </summary>
        [Tooltip("The space on which the joystick is moving. \n" +
            "World space will translate the rig pedestal relative to the world axis. \n" +
            "Self space will translate the rig relative to the camera's look direction. \n")]
        [EnumButtonGroup(60f)]
        public Space pedestalSpace;

        /// <summary>
        /// Mode that determines the focus behavior.
        /// </summary>
        [EnumButtonGroup(60f)]
        public FocusMode focusMode;

        /// <summary>
        /// Position of the focus reticle.
        /// </summary>
        [Reticle]
        public Vector2 reticlePosition;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"(damping {damping}, rotationLock {rotationLock}, positionLock {positionLock}, " +
                $"ergonomicTilt {ergonomicTilt}, rebasing {rebasing}, motionScale {motionScale}, " +
                $"focusMode {focusMode}, joystickSpeed {joystickSpeed}, pedestalSpace {pedestalSpace}, " +
                $"zeroDutch {zeroDutch}, reticlePosition {reticlePosition})";
        }

        /// <inheritdoc/>
        public bool Equals(CameraState other)
        {
            return damping == other.damping
                && rotationLock == other.rotationLock
                && positionLock == other.positionLock
                && ergonomicTilt == other.ergonomicTilt
                && rebasing == other.rebasing
                && motionScale == other.motionScale
                && focusMode == other.focusMode
                && joystickSpeed == other.joystickSpeed
                && pedestalSpace == other.pedestalSpace
                && zeroDutch == other.zeroDutch
                && reticlePosition == other.reticlePosition;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current CameraState.
        /// </summary>
        /// <param name="obj">The object to compare with the current CameraState.</param>
        /// <returns>
        /// true if the specified object is equal to the current CameraState; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is CameraState other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for the CameraState.
        /// </summary>
        /// <returns>
        /// The hash value generated for this CameraState.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = damping.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)rotationLock;
                hashCode = (hashCode * 397) ^ (int)positionLock;
                hashCode = (hashCode * 397) ^ ergonomicTilt.GetHashCode();
                hashCode = (hashCode * 397) ^ rebasing.GetHashCode();
                hashCode = (hashCode * 397) ^ motionScale.GetHashCode();
                hashCode = (hashCode * 397) ^ focusMode.GetHashCode();
                hashCode = (hashCode * 397) ^ joystickSpeed.GetHashCode();
                hashCode = (hashCode * 397) ^ pedestalSpace.GetHashCode();
                hashCode = (hashCode * 397) ^ zeroDutch.GetHashCode();
                hashCode = (hashCode * 397) ^ reticlePosition.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the two specified CameraState are equal.
        /// </summary>
        /// <param name="a">The first CameraState.</param>
        /// <param name="b">The second CameraState.</param>
        /// <returns>
        /// true if the specified CameraState are equal; otherwise, false.
        /// </returns>
        public static bool operator==(CameraState a, CameraState b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether the two specified CameraState are different.
        /// </summary>
        /// <param name="a">The first CameraState.</param>
        /// <param name="b">The second CameraState.</param>
        /// <returns>
        /// true if the specified CameraState are different; otherwise, false.
        /// </returns>
        public static bool operator!=(CameraState a, CameraState b)
        {
            return !(a == b);
        }
    }
}
