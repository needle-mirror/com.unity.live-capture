using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomPropertyDrawer(typeof(VirtualCameraTrackMetadata))]
    class VirtualCameraTrackMetadataPropertyDrawer : PropertyDrawer
    {
        static class Contents
        {
            public static string[] channels = new string[]
            {
                EditorGUIUtility.TrTextContent("Position").text,
                EditorGUIUtility.TrTextContent("Rotation").text,
                EditorGUIUtility.TrTextContent("Focal Length").text,
                EditorGUIUtility.TrTextContent("Focus Distance").text,
                EditorGUIUtility.TrTextContent("Aperture").text,
            };
            public static GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        }

        List<string> m_Channels = new List<string>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var rows = 1;
            var rowHeight = EditorGUIUtility.singleLineHeight + 2f;

            if (property.isExpanded)
            {
                var channelsProp = property.FindPropertyRelative("m_Channels");
                var channels = (VirtualCameraChannelFlags)channelsProp.enumValueIndex;

                rows = 7;

                if (channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
                {
                    ++rows;
                }

                if (channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
                {
                    ++rows;
                }

                if (channels.HasFlag(VirtualCameraChannelFlags.Aperture))
                {
                    ++rows;
                }
            }

            return rowHeight * rows;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

            if (property.isExpanded)
            {
                DoGUI(position, property);
            }
        }

        void DoGUI(Rect position, SerializedProperty property)
        {
            using var _ = new EditorGUI.IndentLevelScope();

            var lensProp = property.FindPropertyRelative("m_Lens");
            var channelsProp = property.FindPropertyRelative("m_Channels");
            var cameraBodyProp = property.FindPropertyRelative("m_CameraBody");
            var channels = (VirtualCameraChannelFlags)channelsProp.enumValueIndex;
            var sensorSize = cameraBodyProp.FindPropertyRelative("sensorSize").vector2Value;
            var isoProp = cameraBodyProp.FindPropertyRelative("iso");
            var shutterSpeedProp = cameraBodyProp.FindPropertyRelative("shutterSpeed");
            var sensorSizes = FormatPresetsCache.GetSensorSizes();
            var options = FormatPresetsCache.GetSensorNameContents();
            var index = Array.FindIndex(sensorSizes, (s) => s == sensorSize);

            m_Channels.Clear();

            for (var i = 0; i < 5; ++i)
            {
                if (channels.HasFlag((VirtualCameraChannelFlags)(1 << i)))
                {
                    m_Channels.Add(Contents.channels[i]);
                }
            }

            var channelsStr = m_Channels.Count == 0 ? "None" : string.Join(", ", m_Channels);

            position.y += position.height + 2f;
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(position, new GUIContent("Channels"), GUIContent.none);

            position.y += position.height + 2f;
            position.height = EditorGUIUtility.singleLineHeight * 2f;
            EditorGUI.TextArea(position, channelsStr, Contents.textAreaStyle);

            if (channels.HasFlag(VirtualCameraChannelFlags.FocalLength))
            {
                var prop = lensProp.FindPropertyRelative("focalLength");

                position.y += position.height + 2f;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, prop, LensPropertyDrawer.Contents.focalLength);
            }

            if (channels.HasFlag(VirtualCameraChannelFlags.FocusDistance))
            {
                var prop = lensProp.FindPropertyRelative("focusDistance");

                position.y += position.height + 2f;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, prop, LensPropertyDrawer.Contents.focusDistance);
            }

            if (channels.HasFlag(VirtualCameraChannelFlags.Aperture))
            {
                var prop = lensProp.FindPropertyRelative("aperture");

                position.y += position.height + 2f;
                position.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, prop, LensPropertyDrawer.Contents.aperture);
            }

            position.y += position.height + 2f;
            position.height = EditorGUIUtility.singleLineHeight;

            if (index == -1)
            {
                EditorGUI.Vector2Field(position, SensorSizePropertyDrawer.Contents.sensorSize, sensorSize);
            }
            else
            {
                EditorGUI.LabelField(position, SensorSizePropertyDrawer.Contents.sensorSize, new GUIContent(options[index]));
            }

            position.y += position.height + 2f;
            EditorGUI.PropertyField(position, isoProp, CameraBodyPropertyDrawer.Contents.iso);

            position.y += position.height + 2f;
            EditorGUI.PropertyField(position, shutterSpeedProp, CameraBodyPropertyDrawer.Contents.shutterSpeed);
        }
    }
}
