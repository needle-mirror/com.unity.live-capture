using System;
using Unity.LiveCapture.CompanionApp;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomEditor(typeof(VirtualCameraDevice))]
    class VirtualCameraDeviceEditor : CompanionAppDeviceEditor<IVirtualCameraClient>
    {
        static class Contents
        {
            public static GUIContent lensLabel = EditorGUIUtility.TrTextContent("Lens");
            public static GUIContent lensPreset = EditorGUIUtility.TrTextContent("Lens Preset");
        }

        static readonly string[] s_ExcludeProperties = { "m_Script", "m_Actor", "m_Rig", "m_LiveLink", "m_Lens", "m_LensPreset" };

        VirtualCameraDevice m_Device;
        SerializedProperty m_LiveLinkChannels;
        SerializedProperty m_LensPreset;
        SerializedProperty m_Lens;
        SerializedProperty m_FocalLengthProp;
        SerializedProperty m_FocalLengthRangeProp;
        SerializedProperty m_FocusDistanceProp;
        SerializedProperty m_FocusDistanceRangeProp;
        SerializedProperty m_ApertureProp;
        SerializedProperty m_ApertureRangeProp;
        SerializedProperty m_LensShiftProp;
        SerializedProperty m_BladeCountProp;
        SerializedProperty m_CurvatureProp;
        SerializedProperty m_BarrelClippingProp;
        SerializedProperty m_AnamorphismProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Device = target as VirtualCameraDevice;

            m_LiveLinkChannels = serializedObject.FindProperty("m_LiveLink.channels");
            m_LensPreset = serializedObject.FindProperty("m_LensPreset");
            m_Lens = serializedObject.FindProperty("m_Lens");

            m_FocalLengthProp = m_Lens.FindPropertyRelative("focalLength");
            m_FocalLengthRangeProp = m_Lens.FindPropertyRelative("focalLengthRange");
            m_FocusDistanceProp = m_Lens.FindPropertyRelative("focusDistance");
            m_FocusDistanceRangeProp = m_Lens.FindPropertyRelative("focusDistanceRange");
            m_ApertureProp = m_Lens.FindPropertyRelative("aperture");
            m_ApertureRangeProp = m_Lens.FindPropertyRelative("apertureRange");
            m_LensShiftProp = m_Lens.FindPropertyRelative("lensShift");
            m_BladeCountProp = m_Lens.FindPropertyRelative("bladeCount");
            m_CurvatureProp = m_Lens.FindPropertyRelative("curvature");
            m_BarrelClippingProp = m_Lens.FindPropertyRelative("barrelClipping");
            m_AnamorphismProp = m_Lens.FindPropertyRelative("anamorphism");
        }

        protected override void OnDeviceGUI()
        {
            DoClientGUI();
            DoActorGUI(m_Device.actor, (actor) => m_Device.actor = actor);

            serializedObject.Update();

            DoLiveLinkChannelsGUI(m_LiveLinkChannels);
            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            DoLensGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void DoLensGUI()
        {
            EditorGUILayout.LabelField(Contents.lensLabel, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            using var check = new EditorGUI.ChangeCheckScope();
            m_LensPreset.objectReferenceValue = EditorGUILayout.ObjectField(Contents.lensPreset, m_LensPreset.objectReferenceValue, typeof(LensPreset), false);
            EditorGUI.indentLevel--;

            if (check.changed && m_LensPreset.objectReferenceValue != null)
            {
                var preset = ((LensPreset)m_LensPreset.objectReferenceValue).lens;

                m_FocalLengthProp.floatValue = preset.focalLength;
                m_FocalLengthRangeProp.vector2Value = preset.focalLengthRange;
                m_FocusDistanceProp.floatValue = preset.focusDistance;
                m_FocusDistanceRangeProp.vector2Value = preset.focusDistanceRange;
                m_ApertureProp.floatValue = preset.aperture;
                m_ApertureRangeProp.vector2Value = preset.apertureRange;
                m_LensShiftProp.vector2Value = preset.lensShift;
                m_BladeCountProp.intValue = preset.bladeCount;
                m_CurvatureProp.vector2Value = preset.curvature;
                m_BarrelClippingProp.floatValue = preset.barrelClipping;
                m_AnamorphismProp.floatValue = preset.anamorphism;
            }

            EditorGUILayout.PropertyField(m_Lens);
        }
    }
}
