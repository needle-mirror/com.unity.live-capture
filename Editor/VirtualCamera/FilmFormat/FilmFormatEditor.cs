using System;
using UnityEditor;
using UnityEngine;
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomEditor(typeof(FilmFormat))]
    [CanEditMultipleObjects]
    class FilmFormatEditor : Editor
    {
        static class Contents
        {
            public static readonly GUIContent gateMaskLabel = EditorGUIUtility.TrTextContent("Gate Mask", "Show the gate mask.");
            public static readonly GUIContent cropMaskLabel = EditorGUIUtility.TrTextContent("Crop Mask", "Show the crop mask for the film gate.");
        }

        SerializedProperty m_CameraProp;
        SerializedProperty m_ShowGateMaskProp;
        SerializedProperty m_ShowCropMaskProp;
        SerializedProperty m_CropAspectProp;
        SerializedProperty m_OpacityProp;
        SerializedObject m_EditorSO;

        void OnEnable()
        {
            m_CameraProp = serializedObject.FindProperty("m_Camera");
            m_ShowGateMaskProp = serializedObject.FindProperty("m_ShowGateMask");
            m_ShowCropMaskProp = serializedObject.FindProperty("m_ShowCropMask");
            m_OpacityProp = serializedObject.FindProperty("m_Opacity");
            m_CropAspectProp = serializedObject.FindProperty("m_CropAspect");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CameraProp);
            EditorGUILayout.PropertyField(m_ShowGateMaskProp, Contents.gateMaskLabel);

            // The crop mask may not be shown without the gate mask.
            if (m_ShowGateMaskProp.boolValue)
            {
                EditorGUILayout.PropertyField(m_ShowCropMaskProp, Contents.cropMaskLabel);

                if (m_ShowCropMaskProp.boolValue)
                {
                    EditorGUILayout.PropertyField(m_CropAspectProp);
                }
            }

            EditorGUILayout.PropertyField(m_OpacityProp);

            serializedObject.ApplyModifiedProperties();

#if URP_10_2_OR_NEWER
            RenderFeatureEditor<FilmFormat, VirtualCameraScriptableRenderFeature>.OnInspectorGUI();
#endif
        }
    }
}
