using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Struct that stores the data of a sensor preset
    /// </summary>
    [Serializable]
    public struct SensorPreset
    {
        /// <summary>
        /// Name of the preset
        /// </summary>
        public string name;
        /// <summary>
        /// Size of the sensor in the preset
        /// </summary>
        public Vector2 sensorSize;
    }

    /// <summary>
    /// Struct that stores the data of an aspect ratio preset
    /// </summary>
    [Serializable]
    public struct AspectRatioPreset
    {
        /// <summary>
        /// Name of the preset
        /// </summary>
        public string name;
        /// <summary>
        /// Aspect ratio of the preset
        /// </summary>
        public float aspectRatio;
    }

    /// <summary>
    /// Asset that stores lists of presets for sensor sizes and aspect ratios
    /// </summary>
    [CreateAssetMenu(fileName = "Format Presets", menuName = "Live Capture/Virtual Camera/Format Presets", order = 1)]
    public class FormatPresets : ScriptableObject
    {
        [SerializeField]
        List<SensorPreset> m_SensorPresets = new List<SensorPreset>();

        [SerializeField]
        List<AspectRatioPreset> m_AspectRatioPresets = new List<AspectRatioPreset>();

        /// <summary>
        /// The array of sensor size presets
        /// </summary>
        public SensorPreset[] sensorPresets { get => m_SensorPresets.ToArray(); }

        /// <summary>
        /// The array of aspect ratio presets
        /// </summary>
        public AspectRatioPreset[] aspectRatioPresets { get => m_AspectRatioPresets.ToArray(); }
    }
}
