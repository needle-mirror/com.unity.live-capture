using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Contains the the Damping data.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Damping : IEquatable<Damping>
    {
        /// <summary>
        /// Enable or disable the damping.
        /// </summary>
        [Tooltip("Enable or disable damping.")]
        [MarshalAs(UnmanagedType.U1)]
        public bool enabled;

        /// <summary>
        /// Time in seconds for the camera to reach reach the target position.
        /// </summary>
        /// <remarks>
        /// Values should not be negative. Typical values would range between 0 and 3.
        /// </remarks>
        [Tooltip("Time in seconds for the camera to reach reach the target position.")]
        public Vector3 body;

        /// <summary>
        /// Time in seconds for the camera to catch up with the target rotation.
        /// </summary>
        /// <remarks>
        /// Values should not be negative. Typical values would range between 0 and 3.
        /// </remarks>
        [Tooltip("Time in seconds for the camera to catch up with the target rotation.")]
        public float aim;

        /// <summary>
        /// A rig that applies no damping.
        /// </summary>
        public static readonly Damping Default = new Damping()
        {
            enabled = false,
            body = Vector3.one,
            aim = 1
        };

        /// <summary>
        /// Determines whether the two specified Damping are equal.
        /// </summary>
        /// <param name="a"> The first Damping.</param>
        /// <param name="b"> The second Damping.</param>
        /// <returns>
        /// true if the specified Damping are equal; otherwise, false.
        /// </returns>
        public static bool operator==(Damping a, Damping b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether the two specified Damping are different.
        /// </summary>
        /// <param name="a"> The first Damping.</param>
        /// <param name="b"> The second Damping.</param>
        /// <returns>
        /// true if the specified Damping are different; otherwise, false.
        /// </returns>
        public static bool operator!=(Damping a, Damping b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Damping.
        /// </summary>
        /// <param name="obj"> The object to compare with the current Damping.</param>
        /// <returns>
        /// true if the specified object is equal to the current Damping; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is Damping damping && Equals(damping);
        }

        /// <inheritdoc/>
        public bool Equals(Damping other)
        {
            return enabled == other.enabled &&
                body == other.body &&
                aim == other.aim;
        }

        /// <summary>
        /// Gets the hash code for the Damping.
        /// </summary>
        /// <returns>
        /// The hash value generated for this Damping.
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = -273304878;
            hashCode = hashCode * -1521134295 + enabled.GetHashCode();
            hashCode = hashCode * -1521134295 + body.GetHashCode();
            hashCode = hashCode * -1521134295 + aim.GetHashCode();
            return hashCode;
        }
    }
}
