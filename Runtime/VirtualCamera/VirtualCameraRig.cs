using System;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera.Rigs
{
    /// <summary>
    /// Struct that contains the configuration parameters of the <see cref="VirtualCameraRig"/>.
    /// </summary>
    [Serializable]
    struct VirtualCameraRigSettings
    {
        /// <summary>
        /// The <see cref="PositionAxis"/> that won't move.
        /// </summary>
        public PositionAxis positionLock;

        /// <summary>
        /// Rotation Axis constraint settings.
        /// </summary>
        public RotationAxis rotationLock;

        /// <summary>
        /// Force dutch rotation to be zero.
        /// </summary>
        public bool zeroDutch;

        /// <summary>
        /// Rebasing set to true indicates that the Virtual Camera is frozen in virtual space allowing the user
        /// to change position in real world space.
        /// </summary>
        public bool rebasing;

        /// <summary>
        /// Scale of the movement for each axis. A scale of (1, 1, 1) means that the virtual camera position will
        /// Match the device position in real world.
        /// </summary>
        public Vector3 motionScale;

        /// <summary>
        /// The angle around the x-axis to offset the local camera rotation.
        /// </summary>
        public float ergonomicTilt;

        /// <summary>
        /// The default state of a rig settings.
        /// </summary>
        public static VirtualCameraRigSettings identity => new VirtualCameraRigSettings()
        {
            motionScale = Vector3.one,
            positionLock = PositionAxis.None,
            rotationLock = RotationAxis.None
        };
    }

    /// <summary>
    /// Struct that contains the state of the <see cref="VirtualCameraRig"/>
    /// </summary>
    [Serializable]
    struct VirtualCameraRigState
    {
        /// <summary>
        /// The calculated global <see cref="Pose"/> of the camera.
        /// </summary>
        [ReadOnly, Tooltip("The calculated global pose of the camera.")]
        public Pose pose;

        /// <summary>
        /// The global offset in world space representing the origin of the virtual camera.
        /// </summary>
        [Tooltip("The point of origin of the camera.")]
        public Pose origin;

        /// <summary>
        /// The calculated local <see cref="Pose"/> of the camera.
        /// </summary>
        [Tooltip("The pose relative to the origin.")]
        public Pose localPose;

        /// <summary>
        /// The input sample of the previous calculation.
        /// </summary>
        [HideInInspector]
        public Pose lastInput;

        /// <summary>
        /// Offset relative to the pose input. Generated during a rebase.
        /// </summary>
        [HideInInspector]
        public Quaternion rebaseOffset;

        /// <summary>
        /// The current ergonomic tilt value of the state.
        /// </summary>
        [HideInInspector]
        public float ergonomicTilt;

        /// <summary>
        /// Identity initialized VirtualCameraRigState.
        /// </summary>
        public static VirtualCameraRigState identity => new VirtualCameraRigState()
        {
            lastInput = Pose.identity,
            rebaseOffset = Quaternion.identity,
            localPose = Pose.identity,
            pose = Pose.identity,
            origin = Pose.identity
        };
    }

    /// <summary>
    /// Static class that contains the data needed for calculating the final <see cref="Pose"/> of a virtual camera.
    /// VirtualCameraRig implements rebasing, position and rotation axis lock and global offset.
    /// </summary>
    static class VirtualCameraRig
    {
        /// <summary>
        /// Updates the <see cref="VirtualCameraRigState"/> of the rig from a new pose sample.
        /// </summary>
        /// <param name="input">The pose sample to follow</param>
        /// <param name="settings">The settings that will be applied while updating the state</param>
        public static void Update(this ref VirtualCameraRigState state, Pose input, VirtualCameraRigSettings settings)
        {
            if (settings.rebasing)
                state.Rebase(input);
            else
            {
                var deltaPosition = input.position - state.lastInput.position;
                var axis = (Axis)settings.positionLock;

                // Make delta position local space
                deltaPosition = Quaternion.Inverse(input.rotation) * deltaPosition;
                // Remove locked axis contribution
                deltaPosition = Mask(deltaPosition, axis.HasFlag(Axis.X), axis.HasFlag(Axis.Y), axis.HasFlag(Axis.Z));
                // Motion scale
                deltaPosition = Vector3.Scale(settings.motionScale, deltaPosition);

                var lastErgonomicTilt = Quaternion.Euler(state.ergonomicTilt, 0f, 0f);
                var ergonomicTilt = Quaternion.Euler(settings.ergonomicTilt, 0f, 0f);

                // Compensate for rebase offset and ergonomic tilt
                var targetEuler = (Quaternion.Inverse(state.rebaseOffset) * input.rotation).eulerAngles;
                var currentEuler = (state.localPose.rotation * Quaternion.Inverse(lastErgonomicTilt)).eulerAngles;

                // Use the current euler value if the rotation axis is locked
                axis = (Axis)settings.rotationLock;

                if (axis.HasFlag(Axis.X))
                    targetEuler.x = currentEuler.x;

                if (axis.HasFlag(Axis.Y))
                {
                    state.Rebase(input);
                    targetEuler.y = currentEuler.y;
                }

                if (axis.HasFlag(Axis.Z))
                    targetEuler.z = settings.zeroDutch ? 0f : currentEuler.z;

                var targetRotation = Quaternion.Euler(targetEuler);
                state.localPose.rotation = targetRotation * ergonomicTilt;
                state.localPose.position += targetRotation * deltaPosition;
                state.ergonomicTilt = settings.ergonomicTilt;
            }

            state.pose = new Pose(
                state.origin.position + state.origin.rotation * state.localPose.position,
                state.origin.rotation * state.localPose.rotation);
            state.lastInput = input;
        }

        /// <summary>
        /// Rebase the Y rotation of the camera rig
        /// </summary>
        /// <param name="state">The current state of the rig</param>
        /// <param name="input">The input to rebase</param>
        public static void Rebase(this ref VirtualCameraRigState state, Pose input)
        {
            var localRotationY = Quaternion.Euler(0f, state.localPose.rotation.eulerAngles.y, 0f);
            var inputRotationY = Quaternion.Euler(0f, input.rotation.eulerAngles.y, 0f);
            state.rebaseOffset = Quaternion.Inverse(localRotationY) * inputRotationY;
        }

        /// <summary>
        /// Set the <see cref="VirtualCameraRigState"/> of the rig.
        /// </summary>
        /// <param name="state">The current state to be refreshed</param>
        /// <param name = "settings" > The settings that will be applied to refresh the state</param>
        public static void Refresh(this ref VirtualCameraRigState state, VirtualCameraRigSettings settings)
        {
            var lastErgonomicTilt = Quaternion.Euler(state.ergonomicTilt, 0f, 0f);
            var ergonomicTilt = Quaternion.Euler(settings.ergonomicTilt, 0f, 0f);
            state.localPose.rotation *= ergonomicTilt * Quaternion.Inverse(lastErgonomicTilt);
            // updating the pose relative to the origin and the localPose
            var pose = new Pose(
                state.origin.position + state.origin.rotation * state.localPose.position,
                state.origin.rotation * state.localPose.rotation);
            state.pose = pose;
            state.ergonomicTilt = settings.ergonomicTilt;
        }

        /// <summary>
        /// Set the localPose from a Pose which represents the world position of the camera.
        /// </summary>
        /// <param name="worldCoordinates">World position used to evaluate the localPose</param>
        public static void WorldToLocal(this ref VirtualCameraRigState state, Pose worldCoordinates)
        {
            var originRotation = state.origin.rotation;
            var originInv = Matrix4x4.TRS(state.origin.position, originRotation, Vector3.one).inverse;
            state.localPose.position = originInv.MultiplyPoint3x4(worldCoordinates.position);
            state.localPose.rotation = Quaternion.Inverse(originRotation) * worldCoordinates.rotation;
            state.pose.position = worldCoordinates.position;
            state.pose.rotation = worldCoordinates.rotation;
        }

        /// <summary>
        /// Reset the local pose to the origin and update the rebase offset of the rotation to the new values.
        /// </summary>
        public static void Reset(this ref VirtualCameraRigState state)
        {
            state.localPose.position = Vector3.zero;
            state.localPose.rotation = Quaternion.identity;
            state.rebaseOffset = Quaternion.Euler(0f, state.lastInput.rotation.eulerAngles.y, 0f);
        }

        /// <summary>
        /// Translate the pose without taking into account the constraints.
        /// </summary>
        /// <param name="vector">The direction of the translation. The direction is also affected by the space parameter</param>
        /// <param name="deltaTime">The time step in seconds</param>
        /// <param name="speed">Scaled speed along x, y and z directions</param>
        /// <param name="pedestalSpace">Should the translation on the Y axis happen relative to world or local space</param>
        /// <param name = "settings" > The settings that will be applied while translating the state</param>
        public static void Translate(this ref VirtualCameraRigState state, Vector3 vector, float deltaTime, Vector3 speed, Space pedestalSpace, VirtualCameraRigSettings settings)
        {
            var deltaSpeed = speed * deltaTime;

            // Transpose the Vector and the Speed to local space
            var up = (pedestalSpace == Space.Self) ? state.localPose.up : state.origin.up;
            state.localPose.position +=
                state.localPose.forward * vector.z * deltaSpeed.z +
                state.localPose.right * vector.x * deltaSpeed.x +
                up * vector.y * deltaSpeed.y;

            state.Refresh(settings);
        }

        static Vector3 Mask(Vector3 vector, bool x, bool y, bool z)
        {
            return Vector3.Scale(vector, new Vector3(x ? 0f : 1f, y ? 0f : 1f, z ? 0f : 1f));
        }
    }
}
