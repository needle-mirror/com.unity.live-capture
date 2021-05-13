using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Contains all the parameters needed to configure a physical camera lens.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Lens : IEquatable<Lens>
    {
        const float k_DefaultFocalLength = 50f;
        const float k_DefaultFocusDistance = 1f;
        const float k_DefaultAperture = 5.6f;

        /// <summary>
        /// The default lens values.
        /// </summary>
        public static readonly Lens DefaultParams = new Lens
        {
            FocalLength = k_DefaultFocalLength,
            FocusDistance = k_DefaultFocusDistance,
            Aperture = k_DefaultAperture,
        };

        /// <summary>
        /// The focal length in millimeters.
        /// </summary>
        public float FocalLength;

        /// <summary>
        /// The focus distance in meters.
        /// </summary>
        public float FocusDistance;

        /// <summary>
        /// The ratio of the f-stop or f-number aperture. The smaller the value is, the shallower the depth of field is
        /// and more light reaches the sensor.
        /// </summary>
        public float Aperture;

        /// <inheritdoc/>
        public bool Equals(Lens other)
        {
            return FocalLength == other.FocalLength
                && FocusDistance == other.FocusDistance
                && Aperture == other.Aperture;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Lens.
        /// </summary>
        /// <param name="obj">The object to compare with the current Lens.</param>
        /// <returns>
        /// true if the specified object is equal to the current Lens; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is Lens other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for the Lens.
        /// </summary>
        /// <returns>
        /// The hash value generated for this Lens.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FocalLength.GetHashCode();
                hashCode = (hashCode * 397) ^ FocusDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ Aperture.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the two specified Lens are equal.
        /// </summary>
        /// <param name="a">The first Lens.</param>
        /// <param name="b">The second Lens.</param>
        /// <returns>
        /// true if the specified Lens are equal; otherwise, false.
        /// </returns>
        public static bool operator==(Lens a, Lens b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether the two specified Lens are different.
        /// </summary>
        /// <param name="a">The first Lens.</param>
        /// <param name="b">The second Lens.</param>
        /// <returns>
        /// true if the specified Lenses are different; otherwise, false.
        /// </returns>
        public static bool operator!=(Lens a, Lens b)
        {
            return !(a == b);
        }
    }

    /// <summary>
    /// A class that contains extension methods for <see cref="Lens"/>.
    /// </summary>
    static class LensExtensions
    {
        /// <summary>
        /// Validates each parameter by setting them into their valid range.
        /// </summary>
        public static void Validate(this ref Lens lens, LensIntrinsics intrinsics)
        {
            lens.FocalLength = intrinsics.ValidateFocalLength(lens.FocalLength);
            lens.FocusDistance = intrinsics.ValidateFocusDistance(lens.FocusDistance);
            lens.Aperture = intrinsics.ValidateAperture(lens.Aperture);
        }
    }
}
