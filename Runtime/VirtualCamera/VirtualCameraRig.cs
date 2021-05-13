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
        /// The default state of a rig settings.
        /// </summary>
        public static VirtualCameraRigSettings Identity => new VirtualCameraRigSettings
        {
            PositionLock = PositionAxis.None,
            RotationLock = RotationAxis.None,
            MotionScale = Vector3.one,
        };

        /// <summary>
        /// The <see cref="PositionAxis"/> that won't move.
        /// </summary>
        public PositionAxis PositionLock;

        /// <summary>
        /// Rotation Axis constraint settings.
        /// </summary>
        public RotationAxis RotationLock;

        /// <summary>
        /// Force dutch rotation to be zero.
        /// </summary>
        public bool ZeroDutch;

        /// <summary>
        /// Rebasing set to true indicates that the Virtual Camera is frozen in virtual space allowing the user
        /// to change position in real world space.
        /// </summary>
        public bool Rebasing;

        /// <summary>
        /// Scale of the movement for each axis. A scale of (1, 1, 1) means that the virtual camera position will
        /// Match the device position in real world.
        /// </summary>
        public Vector3 MotionScale;

        /// <summary>
        /// The angle around the x-axis to offset the local camera rotation.
        /// </summary>
        public float ErgonomicTilt;
    }

    /// <summary>
    /// Struct that contains the state of the <see cref="VirtualCameraRig"/>
    /// </summary>
    [Serializable]
    struct VirtualCameraRigState
    {
        /// <summary>
        /// The default VirtualCameraRigState.
        /// </summary>
        public static VirtualCameraRigState Identity => new VirtualCameraRigState
        {
            Pose = Pose.identity,
            Origin = Pose.identity,
            LocalPose = Pose.identity,
            LastInput = Pose.identity,
            RebaseOffset = Quaternion.identity,
            ErgonomicTilt = 0,
        };

        /// <summary>
        /// The calculated global <see cref="UnityEngine.Pose"/> of the camera.
        /// </summary>
        [ReadOnly, Tooltip("The calculated global pose of the camera.")]
        public Pose Pose;

        /// <summary>
        /// The global offset in world space representing the origin of the virtual camera.
        /// </summary>
        [Tooltip("The point of origin of the camera.")]
        public Pose Origin;

        /// <summary>
        /// The calculated local <see cref="UnityEngine.Pose"/> of the camera.
        /// </summary>
        [Tooltip("The pose relative to the origin.")]
        public Pose LocalPose;

        /// <summary>
        /// The input sample of the previous calculation.
        /// </summary>
        [HideInInspector]
        public Pose LastInput;

        /// <summary>
        /// Offset relative to the pose input. Generated during a rebase.
        /// </summary>
        [HideInInspector]
        public Quaternion RebaseOffset;

        /// <summary>
        /// The current ergonomic tilt value of the state.
        /// </summary>
        [HideInInspector]
        public float ErgonomicTilt;
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
            if (settings.Rebasing)
                state.Rebase(input);
            else
            {
                var deltaPosition = input.position - state.LastInput.position;
                var axis = (Axis)settings.PositionLock;

                // Make delta position local space
                deltaPosition = Quaternion.Inverse(input.rotation) * deltaPosition;
                // Remove locked axis contribution
                deltaPosition = Mask(deltaPosition, axis.HasFlag(Axis.X), axis.HasFlag(Axis.Y), axis.HasFlag(Axis.Z));
                // Motion scale
                deltaPosition = Vector3.Scale(settings.MotionScale, deltaPosition);

                var lastErgonomicTilt = Quaternion.Euler(state.ErgonomicTilt, 0f, 0f);
                var ergonomicTilt = Quaternion.Euler(settings.ErgonomicTilt, 0f, 0f);

                // Compensate for rebase offset and ergonomic tilt
                var targetEuler = (Quaternion.Inverse(state.RebaseOffset) * input.rotation).eulerAngles;
                var currentEuler = (state.LocalPose.rotation * Quaternion.Inverse(lastErgonomicTilt)).eulerAngles;

                // Use the current euler value if the rotation axis is locked
                axis = (Axis)settings.RotationLock;

                if (axis.HasFlag(Axis.X))
                    targetEuler.x = currentEuler.x;

                if (axis.HasFlag(Axis.Y))
                {
                    state.Rebase(input);
                    targetEuler.y = currentEuler.y;
                }

                if (axis.HasFlag(Axis.Z))
                    targetEuler.z = settings.ZeroDutch ? 0f : currentEuler.z;

                var targetRotation = Quaternion.Euler(targetEuler);
                state.LocalPose.rotation = targetRotation * ergonomicTilt;
                state.LocalPose.position += targetRotation * deltaPosition;
                state.ErgonomicTilt = settings.ErgonomicTilt;
            }

            state.Pose = new Pose(
                state.Origin.position + state.Origin.rotation * state.LocalPose.position,
                state.Origin.rotation * state.LocalPose.rotation);
            state.LastInput = input;
        }

        /// <summary>
        /// Rebase the Y rotation of the camera rig
        /// </summary>
        /// <param name="state">The current state of the rig</param>
        /// <param name="input">The input to rebase</param>
        public static void Rebase(this ref VirtualCameraRigState state, Pose input)
        {
            var localRotationY = Quaternion.Euler(0f, state.LocalPose.rotation.eulerAngles.y, 0f);
            var inputRotationY = Quaternion.Euler(0f, input.rotation.eulerAngles.y, 0f);
            state.RebaseOffset = Quaternion.Inverse(localRotationY) * inputRotationY;
        }

        /// <summary>
        /// Set the <see cref="VirtualCameraRigState"/> of the rig.
        /// </summary>
        /// <param name="state">The current state to be refreshed</param>
        /// <param name = "settings" > The settings that will be applied to refresh the state</param>
        public static void Refresh(this ref VirtualCameraRigState state, VirtualCameraRigSettings settings)
        {
            var lastErgonomicTilt = Quaternion.Euler(state.ErgonomicTilt, 0f, 0f);
            var ergonomicTilt = Quaternion.Euler(settings.ErgonomicTilt, 0f, 0f);
            state.LocalPose.rotation *= ergonomicTilt * Quaternion.Inverse(lastErgonomicTilt);
            // updating the pose relative to the origin and the localPose
            var pose = new Pose(
                state.Origin.position + state.Origin.rotation * state.LocalPose.position,
                state.Origin.rotation * state.LocalPose.rotation);
            state.Pose = pose;
            state.ErgonomicTilt = settings.ErgonomicTilt;
        }

        /// <summary>
        /// Set the localPose from a Pose which represents the world position of the camera.
        /// </summary>
        /// <param name="worldCoordinates">World position used to evaluate the localPose</param>
        public static void WorldToLocal(this ref VirtualCameraRigState state, Pose worldCoordinates)
        {
            var originRotation = state.Origin.rotation;
            var originInv = Matrix4x4.TRS(state.Origin.position, originRotation, Vector3.one).inverse;
            state.LocalPose.position = originInv.MultiplyPoint3x4(worldCoordinates.position);
            state.LocalPose.rotation = Quaternion.Inverse(originRotation) * worldCoordinates.rotation;
            state.Pose.position = worldCoordinates.position;
            state.Pose.rotation = worldCoordinates.rotation;
        }

        /// <summary>
        /// Reset the local pose to the origin and update the rebase offset of the rotation to the new values.
        /// </summary>
        public static void Reset(this ref VirtualCameraRigState state)
        {
            state.LocalPose.position = Vector3.zero;
            state.LocalPose.rotation = Quaternion.identity;
            state.RebaseOffset = Quaternion.Euler(0f, state.LastInput.rotation.eulerAngles.y, 0f);
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
            deltaTime = Mathf.Max(0f, deltaTime);

            var deltaSpeed = speed * deltaTime;

            // Transpose the Vector and the Speed to local space
            var up = (pedestalSpace == Space.Self) ? state.LocalPose.up : state.Origin.up;
            state.LocalPose.position +=
                state.LocalPose.forward * vector.z * deltaSpeed.z +
                state.LocalPose.right * vector.x * deltaSpeed.x +
                up * vector.y * deltaSpeed.y;

            state.Refresh(settings);
        }

        static Vector3 Mask(Vector3 vector, bool x, bool y, bool z)
        {
            return Vector3.Scale(vector, new Vector3(x ? 0f : 1f, y ? 0f : 1f, z ? 0f : 1f));
        }
    }
}
