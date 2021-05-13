using System;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [Serializable]
    struct FrameLinesSettings : IEquatable<FrameLinesSettings>
    {
        public enum Mode
        {
            None,
            Box,
            Corner
        }

        public enum Marker
        {
            Cross,
            Dot
        }

        public bool RenderAspectRatio;

        public bool RenderCenterMarker;

        [Range(0, 1)]
        public float GateMaskOpacity;

        [AspectRatio]
        public float AspectRatio;

        public Mode AspectMode;

        public Color AspectLineColor;

        [Range(1, 10)]
        public float AspectLineWidth;

        [Range(0, 1)]
        public float AspectFillOpacity;

        public Marker CenterMarker;

        public static FrameLinesSettings GetDefault()
        {
            return new FrameLinesSettings
            {
                RenderAspectRatio = true,
                RenderCenterMarker = true,
                GateMaskOpacity = .5f,
                AspectRatio = Settings.k_DefaultAspectRatio,
                AspectMode = Mode.Corner,
                AspectLineColor = Color.cyan,
                AspectLineWidth = 3,
                AspectFillOpacity = .2f,
                CenterMarker = Marker.Cross,
            };
        }

        public void Validate()
        {
            GateMaskOpacity = Mathf.Clamp01(GateMaskOpacity);
            AspectFillOpacity = Mathf.Clamp01(AspectFillOpacity);
            AspectLineWidth = Mathf.Clamp(AspectLineWidth, 1, 10);
            AspectRatio = Mathf.Max(Settings.k_MinAspectRatio, AspectRatio);
        }

        public bool Equals(FrameLinesSettings other)
        {
            return RenderAspectRatio == other.RenderAspectRatio
                && RenderCenterMarker == other.RenderCenterMarker
                && GateMaskOpacity == other.GateMaskOpacity
                && AspectRatio == other.AspectRatio
                && AspectMode == other.AspectMode
                && AspectLineColor == other.AspectLineColor
                && AspectLineWidth == other.AspectLineWidth
                && AspectFillOpacity == other.AspectFillOpacity
                && CenterMarker == other.CenterMarker;
        }

        public override bool Equals(object obj)
        {
            return obj is FrameLinesSettings other && Equals(other);
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
                var hashCode = RenderAspectRatio.GetHashCode();
                hashCode = (hashCode * 397) ^ RenderCenterMarker.GetHashCode();
                hashCode = (hashCode * 397) ^ GateMaskOpacity.GetHashCode();
                hashCode = (hashCode * 397) ^ AspectRatio.GetHashCode();
                hashCode = (hashCode * 397) ^ AspectMode.GetHashCode();
                hashCode = (hashCode * 397) ^ AspectLineColor.GetHashCode();
                hashCode = (hashCode * 397) ^ AspectLineWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ AspectFillOpacity.GetHashCode();
                hashCode = (hashCode * 397) ^ CenterMarker.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(FrameLinesSettings a, FrameLinesSettings b)
        {
            return a.Equals(b);
        }

        public static bool operator!=(FrameLinesSettings a, FrameLinesSettings b)
        {
            return !(a == b);
        }
    }
}
