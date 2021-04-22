using System;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomPropertyDrawer(typeof(SensorSizeAttribute))]
    class SensorSizePropertyDrawer : PropertyDrawer
    {
        internal static class Contents
        {
            public static GUIContent sensorPreset =
                EditorGUIUtility.TrTextContent("Sensor Preset", $"Select the sensor size from default presets or " +
                    $"custom presets defined in {nameof(FormatPresets)} assets in the project.");
            public static GUIContent sensorSize =
                EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            DoPresetSelector(position, property);

            position.y += EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(position, property, Contents.sensorSize);
        }

        void DoPresetSelector(Rect position, SerializedProperty property)
        {
            var sensorSize = property.vector2Value;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var sensorSizes = FormatPresetsCache.GetSensorSizes();
                var index = Array.FindIndex(sensorSizes, (s) => s == sensorSize);
                var options = FormatPresetsCache.GetSensorNameContents();

                if (index == -1)
                {
                    options = FormatPresetsCache.GetSensorNameContentsWithCustom();
                    index = sensorSizes.Length;
                }

                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                index = EditorGUI.Popup(position, Contents.sensorPreset, index, options);

                if (check.changed && index >= 0 && index < sensorSizes.Length)
                {
                    property.vector2Value = sensorSizes[index];
                }
            }
        }
    }
}
