using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Contains the minimum and maximum values for each camera body parameter.
    /// </summary>
    static class CameraBodyParameterBounds
    {
        public static readonly Vector2 sensorSize = new Vector2(1f, 205f);
        public static readonly Vector2Int iso = new Vector2Int(1, 10000);
        public static readonly Vector2 shutterSpeed = new Vector2(0f, 10f);
    }

    /// <summary>
    /// Contains all the parameters needed to model a physical camera body.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CameraBody : IEquatable<CameraBody>
    {
        static readonly Vector2 k_DefaultSensorSize = new Vector2(24.89f, 18.66f);
        static readonly int k_DefaultIso = 200;
        static readonly float k_DefaultShutterSpeed = 0.005f;

        /// <summary>
        /// The default CameraBody.
        /// </summary>
        public static readonly CameraBody defaultParams = new CameraBody
        {
            sensorSize = k_DefaultSensorSize,
            iso = k_DefaultIso,
            shutterSpeed = k_DefaultShutterSpeed,
        };

        /// <summary>
        /// Set the size, in millimeters, of the real-world camera sensor.
        /// </summary>
        [SensorSize]
        public Vector2 sensorSize;

        /// <summary>
        /// Set the sensibility of the real-world camera sensor. Higher values increase the Camera's sensitivity to
        /// light and result in faster exposure times.
        /// </summary>
        public int iso;

        /// <summary>
        /// Sets the exposure time, in seconds for the camera. Lower values result in less exposed pictures.
        /// </summary>
        public float shutterSpeed;

        /// <inheritdoc/>
        public bool Equals(CameraBody other)
        {
            return sensorSize == other.sensorSize
                && iso == other.iso
                && shutterSpeed == other.shutterSpeed;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current CameraBody.
        /// </summary>
        /// <param name="obj">The object to compare with the current CameraBody.</param>
        /// <returns>
        /// true if the specified object is equal to the current CameraBody; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is CameraBody other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for the CameraBody.
        /// </summary>
        /// <returns>
        /// The hash value generated for this CameraBody.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = sensorSize.GetHashCode();
                hashCode = (hashCode * 397) ^ iso.GetHashCode();
                hashCode = (hashCode * 397) ^ shutterSpeed.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the two specified CameraBody are equal.
        /// </summary>
        /// <param name="a">The first CameraBody.</param>
        /// <param name="b">The second CameraBody.</param>
        /// <returns>
        /// true if the specified CameraBody are equal; otherwise, false.
        /// </returns>
        public static bool operator==(CameraBody a, CameraBody b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Determines whether the two specified CameraBody are different.
        /// </summary>
        /// <param name="a">The first CameraBody.</param>
        /// <param name="b">The second CameraBody.</param>
        /// <returns>
        /// true if the specified CameraBody are different; otherwise, false.
        /// </returns>
        public static bool operator!=(CameraBody a, CameraBody b)
        {
            return !(a == b);
        }
    }

    /// <summary>
    /// A class that contains extension methods for <see cref="CameraBody"/>.
    /// </summary>
    static class CameraBodyExtensions
    {
        /// <summary>
        /// Validates each parameter by setting them into their valid range.
        /// </summary>
        public static void Validate(this ref CameraBody cameraParams)
        {
            cameraParams.ValidateSensorSize();
            cameraParams.ValidateIso();
            cameraParams.ValidateShutterSpeed();
        }

        /// <summary>
        /// Validates <see cref="CameraBody.sensorSize"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateSensorSize(this ref CameraBody cameraParams)
        {
            var sensorSize = cameraParams.sensorSize;

            sensorSize.x = Mathf.Clamp(
                sensorSize.x,
                CameraBodyParameterBounds.sensorSize.x,
                CameraBodyParameterBounds.sensorSize.y);

            sensorSize.y = Mathf.Clamp(
                sensorSize.y,
                CameraBodyParameterBounds.sensorSize.x,
                CameraBodyParameterBounds.sensorSize.y);

            cameraParams.sensorSize = sensorSize;
        }

        /// <summary>
        /// Validates <see cref="CameraBody.iso"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateIso(this ref CameraBody cameraParams)
        {
            cameraParams.iso = Mathf.Clamp(
                cameraParams.iso,
                CameraBodyParameterBounds.iso.x,
                CameraBodyParameterBounds.iso.y);
        }

        /// <summary>
        /// Validates <see cref="CameraBody.shutterSpeed"/> by setting it into its valid bounds.
        /// </summary>
        public static void ValidateShutterSpeed(this ref CameraBody cameraParams)
        {
            cameraParams.shutterSpeed = Mathf.Clamp(
                cameraParams.shutterSpeed,
                CameraBodyParameterBounds.shutterSpeed.x,
                CameraBodyParameterBounds.shutterSpeed.y);
        }
    }
}
