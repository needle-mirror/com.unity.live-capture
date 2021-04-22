using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.LiveCapture.VirtualCamera
{
    class VideoServerSettingsProvider : SettingsProvider
    {
        const string k_SettingsMenuPath = "Preferences/Live Capture/Virtual Camera/Video Server";

        static class Contents
        {
            public static readonly GUIContent settingMenuIcon = EditorGUIUtility.IconContent("_Popup");
            public static readonly GUIContent resetLabel = EditorGUIUtility.TrTextContent("Reset", "Reset to default.");
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_ResolutionScale;
        SerializedProperty m_FrameRateProp;
        SerializedProperty m_QualityProp;
        SerializedProperty m_PrioritizeLatencyProp;

        /// <summary>
        /// Open the settings in the User Preferences.
        /// </summary>
        public static void Open()
        {
            SettingsService.OpenProjectSettings(k_SettingsMenuPath);
        }

        public VideoServerSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) {}

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SerializedObject = new SerializedObject(VideoServerSettings.instance);
            m_ResolutionScale = m_SerializedObject.FindProperty("m_ResolutionScale");
            m_FrameRateProp = m_SerializedObject.FindProperty("m_FrameRate");
            m_QualityProp = m_SerializedObject.FindProperty("m_Quality");
            m_PrioritizeLatencyProp = m_SerializedObject.FindProperty("m_PrioritizeLatency");
        }

        public override void OnDeactivate()
        {
        }

        public override void OnTitleBarGUI()
        {
            m_SerializedObject.Update();

            if (EditorGUILayout.DropdownButton(Contents.settingMenuIcon, FocusType.Passive, EditorStyles.label))
            {
                var menu = new GenericMenu();
                menu.AddItem(Contents.resetLabel, false, reset =>
                {
                    VideoServerSettings.instance.Reset();
                    VideoServerSettings.Save();
                }, null);
                menu.ShowAsContext();
            }
        }

        public override void OnGUI(string searchContext)
        {
            m_SerializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            using (new SettingsWindowGUIScope())
            {
                EditorGUILayout.PropertyField(m_ResolutionScale);
                EditorGUILayout.PropertyField(m_FrameRateProp);
                EditorGUILayout.PropertyField(m_QualityProp);
                EditorGUILayout.PropertyField(m_PrioritizeLatencyProp);

                if (change.changed)
                {
                    m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
                    VideoServerSettings.Save();
                }
            }
        }

        public override void OnFooterBarGUI()
        {
        }

        public override void OnInspectorUpdate()
        {
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new VideoServerSettingsProvider(
                k_SettingsMenuPath,
                SettingsScope.User,
                GetSearchKeywordsFromSerializedObject(new SerializedObject(VideoServerSettings.instance))
            );
        }
    }
}
