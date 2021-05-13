using System;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera.Editor
{
    [CustomEditor(typeof(FrameLines))]
    [CanEditMultipleObjects]
    class FrameLinesEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly GUIContent GateMaskOpacityLabel = EditorGUIUtility.TrTextContent("Gate Mask Opacity", "The opacity of the film gate mask.");
            public static readonly GUIContent AspectRatioLabel = EditorGUIUtility.TrTextContent("Aspect Ratio", "The aspect ratio of the crop.");
            public static readonly GUIContent ShowAspectRatioLabel = EditorGUIUtility.TrTextContent("Render Aspect Ratio", "Whether or not to show the crop aspect ratio.");
            public static readonly GUIContent AspectModeLabel = EditorGUIUtility.TrTextContent("Line", "The render mode of the crop lines.");
            public static readonly GUIContent AspectLineColorLabel = EditorGUIUtility.TrTextContent("Line Color", "The color of the crop lines.");
            public static readonly GUIContent AspectLineWidthLabel = EditorGUIUtility.TrTextContent("Line Width", "The width of the crop lines.");
            public static readonly GUIContent AspectFillOpacityLabel = EditorGUIUtility.TrTextContent("Fill Opacity", "The opacity of the crop mask.");
            public static readonly GUIContent ShowCenterMarkerLabel = EditorGUIUtility.TrTextContent("Render Center Marker", "Whether or not to show the center marker.");
            public static readonly GUIContent CenterMarkerLabel = EditorGUIUtility.TrTextContent("Center Marker", "The type of the center marker.");
        }

        SerializedProperty m_SettingsProp;
        SerializedProperty m_ShowAspectRatioProp;
        SerializedProperty m_ShowCenterMarkerProp;
        SerializedProperty m_GateMaskOpacityProp;
        SerializedProperty m_AspectRatioProp;
        SerializedProperty m_AspectModeProp;
        SerializedProperty m_AspectLineColorProp;
        SerializedProperty m_AspectLineWidthProp;
        SerializedProperty m_AspectFillOpacityProp;
        SerializedProperty m_CenterMarkerProp;

        void OnEnable()
        {
            m_SettingsProp = serializedObject.FindProperty("m_Settings");
            m_ShowAspectRatioProp = m_SettingsProp.FindPropertyRelative("RenderAspectRatio");
            m_ShowCenterMarkerProp = m_SettingsProp.FindPropertyRelative("RenderCenterMarker");
            m_GateMaskOpacityProp = m_SettingsProp.FindPropertyRelative("GateMaskOpacity");
            m_AspectRatioProp = m_SettingsProp.FindPropertyRelative("AspectRatio");
            m_AspectModeProp = m_SettingsProp.FindPropertyRelative("AspectMode");
            m_AspectLineColorProp = m_SettingsProp.FindPropertyRelative("AspectLineColor");
            m_AspectLineWidthProp = m_SettingsProp.FindPropertyRelative("AspectLineWidth");
            m_AspectFillOpacityProp = m_SettingsProp.FindPropertyRelative("AspectFillOpacity");
            m_CenterMarkerProp = m_SettingsProp.FindPropertyRelative("CenterMarker");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_GateMaskOpacityProp, Contents.GateMaskOpacityLabel);

                EditorGUILayout.PropertyField(m_ShowAspectRatioProp, Contents.ShowAspectRatioLabel);

                var showAspectRatio = m_ShowAspectRatioProp.boolValue;
                using (new EditorGUI.DisabledScope(!showAspectRatio))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(m_AspectRatioProp, Contents.AspectRatioLabel);
                        EditorGUILayout.PropertyField(m_AspectModeProp, Contents.AspectModeLabel);
                        EditorGUILayout.PropertyField(m_AspectLineColorProp, Contents.AspectLineColorLabel);
                        EditorGUILayout.PropertyField(m_AspectLineWidthProp, Contents.AspectLineWidthLabel);

                        EditorGUILayout.PropertyField(m_AspectFillOpacityProp, Contents.AspectFillOpacityLabel);
                    }
                }

                EditorGUILayout.PropertyField(m_ShowCenterMarkerProp, Contents.ShowCenterMarkerLabel);

                var showCenterMarker = m_ShowCenterMarkerProp.boolValue;
                using (new EditorGUI.DisabledScope(!showCenterMarker))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(m_CenterMarkerProp, Contents.CenterMarkerLabel);
                    }
                }

                if (change.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

#if URP_10_2_OR_NEWER
            RenderFeatureEditor<FrameLines, VirtualCameraScriptableRenderFeature>.OnInspectorGUI();
#endif
        }
    }
}
