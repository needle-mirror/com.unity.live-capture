using System.Collections.Generic;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Lens data comparer that ignores the dynamic parameters of the lens: focal length, focus distance
    /// and aperture.
    /// </summary>
    public class LensSettingsEqualityComparer : IEqualityComparer<Lens>
    {
        /// <summary>
        /// The default comparer.
        /// </summary>
        public static readonly LensSettingsEqualityComparer Default = new LensSettingsEqualityComparer();

        /// <inheritdoc/>
        public bool Equals(Lens x, Lens y)
        {
            return x.focalLengthRange == y.focalLengthRange
                && x.focusDistanceRange == y.focusDistanceRange
                && x.apertureRange == y.apertureRange
                && x.lensShift == y.lensShift
                && x.bladeCount == y.bladeCount
                && x.curvature == y.curvature
                && x.barrelClipping == y.barrelClipping
                && x.anamorphism == y.anamorphism;
        }

        /// <inheritdoc/>
        public int GetHashCode(Lens lens)
        {
            unchecked
            {
                var hashCode = lens.focalLengthRange.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.focusDistanceRange.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.apertureRange.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.lensShift.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.bladeCount.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.curvature.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.barrelClipping.GetHashCode();
                hashCode = (hashCode * 397) ^ lens.anamorphism.GetHashCode();
                return hashCode;
            }
        }
    }
}
