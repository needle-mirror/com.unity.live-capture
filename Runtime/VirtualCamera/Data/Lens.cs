using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Contains the minimum and maximum values for each camera lens parameter.
    /// </summary>
    public static class LensParameterBounds
    {
        /// <summary>
        /// The focal length range limits.
        /// </summary>
        public static readonly Vector2 focalLength = new Vector2(4f, 1200f);

        /// <summary>
        /// The focus distance range limits.
        /// </summary>
        public static readonly Vector2 focusDistance = new Vector2(0.01f, 100f);

        /// <summary>
        /// The minimal focus distance range width.
        /// </summary>
        public static readonly float minFocusDistanceRange = 1e-2f;

        /// <summary>
        /// The aperture range limits.
        /// </summary>
        public static readonly Vector2 aperture = new Vector2(1f, 32f);

        /// <summary>
        /// The blade count range limits.
        /// </summary>
        public static readonly Vector2Int bladeCount = new Vector2Int(3, 11);

        /// <summary>
        /// The curvature range limits.
        /// </summary>
        public static readonly Vector2 curvature = new Vector2(1f, 32f);

        /// <summary>
        /// The barrel clipping range limits.
        /// </summary>
        public static readonly Vector2 barrelClipping = new Vector2(0f, 1f);

        /// <summary>
        /// The anamorphism range limits.
        /// </summary>
        public static readonly Vector2 anamorphism = new Vector2(-1f, 1f);
    }

    /// <summary>
    /// Contains all the parameters needed to model a physical camera lens.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Lens : IEquatable<Lens>
    {
        const float k_DefaultFocalLength = 50f;
        static readonly Vector2 k_DefaultFocalLengthRange = new Vector2(18f, 75f);
        const float k_DefaultFocusDistance = 1f;
        static readonly Vector2 k_DefaultFocusDistanceRange = new Vector2(0.3f, 10f);
        const float k_DefaultAperture = 5.6f;
        static readonly Vector2 k_DefaultApertureRange = new Vector2(1f, 16f);
        const int k_DefaultBladeCount = 5;
        const float k_DefaultMinCurvature = 2f;
        const float k_DefaultMaxCurvature = 11f;
        const float k_DefaultBarrelClipping = 0.25f;
        const float k_DefaultAnamorphism = 0f;

        /// <summary>
        /// The default Lens.
        /// </summary>
        public static readonly Lens defaultParams = new Lens
        {
            focalLength = k_DefaultFocalLength,
            focalLengthRange = k_DefaultFocalLengthRange,
            focusDistance = k_DefaultFocusDistance,
            focusDistanceRange = k_DefaultFocusDistanceRange,
            aperture = k_DefaultAperture,
            apertureRange = k_DefaultApertureRange,
            lensShift = Vector2.zero,
            bladeCount = k_DefaultBladeCount,
            curvature = new Vector2(k_DefaultMinCurvature, k_DefaultMaxCurvature),
            barrelClipping = k_DefaultBarrelClipping,
            anamorphism = k_DefaultAnamorphism,
        };

        /// <summary>
        /// Focal length in millimeters.
        /// </summary>
        public float focalLength;

        /// <summary>
        /// Range of the focal length of the lens.
        /// </summary>
        public Vector2 focalLengthRange;

        /// <summary>
        /// Focus distance in meters.
        /// </summary>
        public float focusDistance;

        /// <summary>
        /// Range of the focus distance of the lens.
        /// </summary>
        public Vector2 focusDistanceRange;

        /// <summary>
        /// Aperture in mm.
        /// The ratio of the f-stop or f-number aperture. The smaller the value is, the shallower the depth of field is
        /// and more light reaches the sensor.
        /// </summary>
        public float aperture;

        /// <summary>
        /// Range of the aperture of the lens.
        /// </summary>
        public Vector2 apertureRange;

        /// <summary>
        /// The horizontal and vertical shift from the center. Values are multiples of the sensor size; for example,
        /// a shift of 0.5 along the X axis offsets the sensor by half its horizontal size. You can use lens shifts to
        /// correct distortion that occurs when the Camera is at an angle to the subject (for example, converging parallel
        /// lines). Shift the lens along either axis to make the Camera frustum oblique.
        /// </summary>
        public Vector2 lensShift;

        /// <summary>
        /// Number of diaphragm blades the Camera uses to form the aperture.
        /// </summary>
        public int bladeCount;

        /// <summary>
        /// Maps an aperture range to blade curvature. Aperture blades become more visible on bokeh at higher aperture
        /// values. Tweak this range to define how the bokeh looks at a given aperture. The minimum value results in
        /// fully-curved, perfectly-circular bokeh, and the maximum value results in fully-shaped bokeh with visible
        /// aperture blades.
        /// </summary>
        public Vector2 curvature;

        /// <summary>
        /// The strength of the “cat eye” effect. You can see this effect on bokeh as a result of lens shadowing
        /// (distortion along the edges of the frame).
        /// </summary>
        public float barrelClipping;

        /// <summary>
        /// Stretch the sensor to simulate an anamorphic look. Positive values distort the Camera vertically, negative
        /// will distort the Camera horizontally.
        /// </summary>
        public float anamorphism;

        /// <inheritdoc/>
        public bool Equals(Lens other)
        {
            return focalLength == other.focalLength
                && focalLengthRange == other.focalLengthRange
                && focusDistance == other.focusDistance
                && focusDistanceRange == other.focusDistanceRange
                && aperture == other.aperture
                && apertureRange == other.apertureRange
                && lensShift == other.lensShift
                && bladeCount == other.bladeCount
                && curvature == other.curvature
                && barrelClipping == other.barrelClipping
                && anamorphism == other.anamorphism;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Lens.
        /// </summary>
        /// <param name="obj"> The object to compare with the current Lens.</param>
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
                var hashCode = focalLength.GetHashCode();
                hashCode = (hashCode * 397) ^ focalLengthRange.GetHashCode();
                hashCode = (hashCode * 397) ^ focusDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ focusDistanceRange.GetHashCode();
                hashCode = (hashCode * 397) ^ aperture.GetHashCode();
                hashCode = (hashCode * 397) ^ apertureRange.GetHashCode();
                hashCode = (hashCode * 397) ^ lensShift.GetHashCode();
                hashCode = (hashCode * 397) ^ bladeCount.GetHashCode();
                hashCode = (hashCode * 397) ^ curvature.GetHashCode();
                hashCode = (hashCode * 397) ^ barrelClipping.GetHashCode();
                hashCode = (hashCode * 397) ^ anamorphism.GetHashCode();
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
        public static void Validate(this ref Lens lens)
        {
            lens.ValidateFocalLengthRange();
            lens.ValidateFocalLength();
            lens.ValidateFocusDistanceRange();
            lens.ValidateFocusDistance();
            lens.ValidateApertureRange();
            lens.ValidateAperture();
            lens.ValidateBladeCount();
            lens.ValidateCurvature();
            lens.ValidateBarrelClipping();
            lens.ValidateAnamorphism();
        }

        /// <summary>
        /// Validates <see cref="Lens.focalLength"/> by setting it into its valid range.
        /// </summary>
        public static void ValidateFocalLength(this ref Lens lens)
        {
            lens.focalLength = Mathf.Clamp(
                lens.focalLength,
                lens.focalLengthRange.x,
                lens.focalLengthRange.y);
        }

        /// <summary>
        /// Validates <see cref="Lens.focalLengthRange"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateFocalLengthRange(this ref Lens lens)
        {
            var range = lens.focalLengthRange;

            range.x = Mathf.Clamp(
                range.x,
                LensParameterBounds.focalLength.x,
                LensParameterBounds.focalLength.y);

            range.y = Mathf.Clamp(
                range.y,
                LensParameterBounds.focalLength.x,
                LensParameterBounds.focalLength.y);

            range.x = Mathf.Min(range.x, range.y);
            range.y = Mathf.Max(range.x, range.y);

            lens.focalLengthRange = range;
        }

        /// <summary>
        /// Validates <see cref="Lens.focusDistance"/> by setting it into its valid range.
        /// </summary>
        public static void ValidateFocusDistance(this ref Lens lens)
        {
            lens.focusDistance = Mathf.Clamp(
                lens.focusDistance,
                lens.focusDistanceRange.x,
                lens.focusDistanceRange.y);
        }

        /// <summary>
        /// Validates <see cref="Lens.focusDistanceRange"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateFocusDistanceRange(this ref Lens lens)
        {
            var range = lens.focusDistanceRange;
            range.x = Mathf.Clamp(
                range.x,
                LensParameterBounds.focusDistance.x,
                LensParameterBounds.focusDistance.y);

            range.y = Mathf.Clamp(
                range.y,
                LensParameterBounds.focusDistance.x,
                LensParameterBounds.focusDistance.y);

            range.y = Mathf.Max(range.x, range.y);

            // Range should have a minimal width.
            if (Mathf.Approximately(range.x, range.y))
            {
                range.x -= LensParameterBounds.minFocusDistanceRange;
                range.y += LensParameterBounds.minFocusDistanceRange;
                range.x = Mathf.Clamp(
                    range.x,
                    LensParameterBounds.focusDistance.x,
                    LensParameterBounds.focusDistance.y);
                range.y = Mathf.Clamp(
                    range.y,
                    LensParameterBounds.focusDistance.x,
                    LensParameterBounds.focusDistance.y);
            }

            lens.focusDistanceRange = range;
        }

        /// <summary>
        /// Validates <see cref="Lens.aperture"/> by setting it into its valid range.
        /// </summary>
        public static void ValidateAperture(this ref Lens lens)
        {
            lens.aperture = Mathf.Clamp(
                lens.aperture,
                lens.apertureRange.x,
                lens.apertureRange.y);
        }

        /// <summary>
        /// Validates <see cref="Lens.apertureRange"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateApertureRange(this ref Lens lens)
        {
            var range = lens.apertureRange;

            range.x = Mathf.Clamp(
                range.x,
                LensParameterBounds.aperture.x,
                LensParameterBounds.aperture.y);

            range.y = Mathf.Clamp(
                range.y,
                LensParameterBounds.aperture.x,
                LensParameterBounds.aperture.y);

            range.x = Mathf.Min(range.x, range.y);
            range.y = Mathf.Max(range.x, range.y);

            lens.apertureRange = range;
        }

        /// <summary>
        /// Validates <see cref="Lens.bladeCount"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateBladeCount(this ref Lens lens)
        {
            lens.bladeCount = Mathf.Clamp(
                lens.bladeCount,
                LensParameterBounds.bladeCount.x,
                LensParameterBounds.bladeCount.y);
        }

        /// <summary>
        /// Validates <see cref="Lens.curvature"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateCurvature(this ref Lens lens)
        {
            var curvature = lens.curvature;

            curvature.x = Mathf.Clamp(
                curvature.x,
                LensParameterBounds.curvature.x,
                LensParameterBounds.curvature.y);

            curvature.y = Mathf.Clamp(
                curvature.y,
                LensParameterBounds.curvature.x,
                LensParameterBounds.curvature.y);

            lens.curvature = curvature;
        }

        /// <summary>
        /// Validates <see cref="Lens.barrelClipping"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateBarrelClipping(this ref Lens lens)
        {
            lens.barrelClipping = Mathf.Clamp(
                lens.barrelClipping,
                LensParameterBounds.barrelClipping.x,
                LensParameterBounds.barrelClipping.y);
        }

        /// <summary>
        /// Validates <see cref="Lens.anamorphism"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateAnamorphism(this ref Lens lens)
        {
            lens.anamorphism = Mathf.Clamp(
                lens.anamorphism,
                LensParameterBounds.anamorphism.x,
                LensParameterBounds.anamorphism.y);
        }
    }
}
