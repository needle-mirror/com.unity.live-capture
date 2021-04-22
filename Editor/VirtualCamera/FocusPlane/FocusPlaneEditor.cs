using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if URP_10_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomEditor(typeof(FocusPlane))]
    [CanEditMultipleObjects]
    class FocusPlaneEditor : Editor
    {
#if URP_10_2_OR_NEWER
        static class Contents
        {
            public static string depthTextureNotActiveMessage = $"Camera Depth Texture is not active, this is required for the {nameof(FocusPlane)} to work properly.";
            public static readonly GUIContent activateDepthTextureButtonLabel = new GUIContent($"Activate Camera Depth Texture",
                $"Clicking this button will activate the Camera Depth Texture by modifying your {nameof(UniversalRenderPipelineAsset)}.");
        }
#endif

        SerializedProperty m_CameraProp;
        SerializedProperty m_SettingsProp;

        void OnEnable()
        {
            m_CameraProp = serializedObject.FindProperty("m_Camera");
            m_SettingsProp = serializedObject.FindProperty("m_Settings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CameraProp);
            EditorGUILayout.PropertyField(m_SettingsProp);

            serializedObject.ApplyModifiedProperties();

#if URP_10_2_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset)
            {
                var asset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
                if (!asset.supportsCameraDepthTexture)
                {
                    EditorGUILayout.HelpBox(Contents.depthTextureNotActiveMessage, MessageType.Warning);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(Contents.activateDepthTextureButtonLabel))
                        {
                            asset.supportsCameraDepthTexture = true;
                            Debug.Log($"Activated Camera Depth Texture on {nameof(UniversalRenderPipelineAsset)}.");
                        }
                        GUILayout.FlexibleSpace();
                    }
                }
            }
            RenderFeatureEditor<FocusPlane, VirtualCameraScriptableRenderFeature>.OnInspectorGUI();
#endif
        }
    }
}
