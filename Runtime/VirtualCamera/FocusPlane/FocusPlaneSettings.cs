using System;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    static class ShaderIDs
    {
        public static readonly int _CameraDepthThreshold = Shader.PropertyToID("_CameraDepthThreshold");
        public static readonly int _Color = Shader.PropertyToID("_Color");
        public static readonly int _GridResolution = Shader.PropertyToID("_GridResolution");
        public static readonly int _GridLineWidth = Shader.PropertyToID("_GridLineWidth");
        public static readonly int _IntersectionLineWidth = Shader.PropertyToID("_IntersectionLineWidth");
        public static readonly int _BackgroundOpacity = Shader.PropertyToID("_BackgroundOpacity");
    }

    /// <summary>
    /// A struct that holds the focus plane rendering settings.
    /// </summary>
    [Serializable]
    struct FocusPlaneSettings : IEquatable<FocusPlaneSettings>
    {
        [Tooltip("The camera space depth at which the focus plane will be rendered.")]
        public float cameraDepthThreshold;
        [Tooltip("Color of the focus plane.")]
        public Color color;
        [Tooltip("Resolution of the focus plane's grid.")]
        public int gridResolution;
        [Tooltip("Width of the focus plane's grid outlines.")]
        public float gridLineWidth;
        [Tooltip("Width of the intersection of the scene geometry and the focus plane.")]
        public float intersectionLineWidth;
        [Tooltip("Opacity of the focus plane's background.")]
        public float backgroundOpacity;

        /// <summary>
        /// Configures material uniforms based on rendering settings.
        /// </summary>
        /// <param name="material">The material used to render the focus plane.</param>
        public void Apply(Material material)
        {
            material.SetFloat(ShaderIDs._CameraDepthThreshold, cameraDepthThreshold);
            material.SetColor(ShaderIDs._Color, color);
            material.SetInt(ShaderIDs._GridResolution, gridResolution);
            material.SetFloat(ShaderIDs._GridLineWidth, gridLineWidth);
            material.SetFloat(ShaderIDs._IntersectionLineWidth, intersectionLineWidth);
            material.SetFloat(ShaderIDs._BackgroundOpacity, backgroundOpacity);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = cameraDepthThreshold.GetHashCode();
                hashCode = (hashCode * 397) ^ color.GetHashCode();
                hashCode = (hashCode * 397) ^ gridResolution.GetHashCode();
                hashCode = (hashCode * 397) ^ gridLineWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ intersectionLineWidth.GetHashCode();
                hashCode = (hashCode * 397) ^ backgroundOpacity.GetHashCode();
                return hashCode;
            }
        }

        public bool Equals(FocusPlaneSettings other)
        {
            return
                cameraDepthThreshold == other.cameraDepthThreshold &&
                color == other.color &&
                gridResolution == other.gridResolution &&
                gridLineWidth == other.gridLineWidth &&
                intersectionLineWidth == other.intersectionLineWidth &&
                backgroundOpacity == other.backgroundOpacity;
        }

        public override bool Equals(object obj)
        {
            return obj is Lens other && Equals(other);
        }

        public static bool operator==(FocusPlaneSettings lhs, FocusPlaneSettings rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(FocusPlaneSettings lhs, FocusPlaneSettings rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Returns default rendering settings.
        /// </summary>
        /// <remarks>
        /// Provides users with arbitrary default settings for rendering the focus plane.
        /// </remarks>
        public static FocusPlaneSettings GetDefault()
        {
            return new FocusPlaneSettings
            {
                cameraDepthThreshold = 5,
                color = new Color(0.269f, 0.546f, 1, 1),
                backgroundOpacity = 0.1f,
                gridLineWidth = 0.1f,
                gridResolution = 16,
                intersectionLineWidth = 0.1f
            };
        }
    }
}
