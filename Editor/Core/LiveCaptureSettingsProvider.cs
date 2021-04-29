using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.LiveCapture.Editor
{
    class LiveCaptureSettingsProvider : SettingsProvider
    {
        const string k_SettingsMenuPath = "Project/Live Capture";

        static class Contents
        {
            public static readonly GUIContent SettingMenuIcon = EditorGUIUtility.IconContent("_Popup");
            public static readonly GUIContent TakeNameFormatLabel = EditorGUIUtility.TrTextContent("Take Name Format", "The format of the file name of the output take.");
            public static readonly GUIContent AssetNameFormatLabel = EditorGUIUtility.TrTextContent("Asset Name Format", "The format of the file name of the generated assets.");
            public static readonly GUIContent ResetLabel = EditorGUIUtility.TrTextContent("Reset", "Reset to default.");
        }

        SerializedObject m_SerializedObject;
        SerializedProperty m_TakeNameFormatProp;
        SerializedProperty m_AssetNameFormatProp;

        /// <summary>
        /// Open the settings in the Project Settings.
        /// </summary>
        public static void Open()
        {
            SettingsService.OpenProjectSettings(k_SettingsMenuPath);
        }

        public LiveCaptureSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords) {}

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            InitializeWithCurrentSettings();
        }

        public override void OnDeactivate()
        {
        }

        public override void OnTitleBarGUI()
        {
            if (EditorGUILayout.DropdownButton(Contents.SettingMenuIcon, FocusType.Passive, EditorStyles.label))
            {
                var menu = new GenericMenu();
                menu.AddItem(Contents.ResetLabel, false, reset =>
                {
                    LiveCaptureSettings.Instance.Reset();
                    LiveCaptureSettings.Save();
                }, null);
                menu.ShowAsContext();
            }
        }

        public override void OnGUI(string searchContext)
        {
            if (m_SerializedObject == null)
            {
                InitializeWithCurrentSettings();
            }
            else
            {
                m_SerializedObject.Update();
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            using (new SettingsWindowGUIScope())
            {
                EditorGUILayout.PropertyField(m_TakeNameFormatProp, Contents.TakeNameFormatLabel);
                EditorGUILayout.PropertyField(m_AssetNameFormatProp, Contents.AssetNameFormatLabel);

                if (change.changed)
                {
                    m_SerializedObject.ApplyModifiedPropertiesWithoutUndo();
                    LiveCaptureSettings.Save();
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
            return new LiveCaptureSettingsProvider(
                k_SettingsMenuPath,
                SettingsScope.Project,
                GetSearchKeywordsFromSerializedObject(new SerializedObject(LiveCaptureSettings.Instance))
            );
        }

        /// <summary>
        /// Grab the <see cref="LiveCaptureSettings"/> instance and set it up for editing.
        /// </summary>
        void InitializeWithCurrentSettings()
        {
            m_SerializedObject = new SerializedObject(LiveCaptureSettings.Instance);
            m_TakeNameFormatProp = m_SerializedObject.FindProperty("m_TakeNameFormat");
            m_AssetNameFormatProp = m_SerializedObject.FindProperty("m_AssetNameFormat");
        }
    }
}
