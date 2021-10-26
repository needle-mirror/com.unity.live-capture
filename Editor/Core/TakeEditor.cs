using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;

namespace Unity.LiveCapture.Editor
{
    [CustomEditor(typeof(Take), true)]
    class TakeEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static string Unknown = "Unknown";
            public static string OverrideStr = "Override";
            public static GUIContent FrameRate = EditorGUIUtility.TrTextContent("Frame Rate", "The frame rate used during the recording.");
            public static GUIContent StartTime = EditorGUIUtility.TrTextContent("Start Time", "The timecode of the first frame of the recording.");
            public static GUIContent SceneNumber = EditorGUIUtility.TrTextContent("Scene Number", "The number associated with the scene where the take was captured.");
            public static GUIContent ShotName = EditorGUIUtility.TrTextContent("Shot Name", "The name of the shot where the take was captured.");
            public static GUIContent TakeNumber = EditorGUIUtility.TrTextContent("Take Number", "The number associated with the take.");
            public static GUIContent Description = EditorGUIUtility.TrTextContent("Description", "The description of the shot where the take was captured.");
            public static GUIContent Rating = EditorGUIUtility.TrTextContent("Rating", "The rating of the take.");
            public static GUIContent Screenshot = EditorGUIUtility.TrTextContent("Screenshot", "The screenshot at the beginning of the take.");
            public static GUIContent Metadata = EditorGUIUtility.TrTextContent("Metadata", "Metadata associated with the recorded contents.");
        }

        SerializedProperty m_SceneNumber;
        SerializedProperty m_ShotName;
        SerializedProperty m_TakeNumber;
        SerializedProperty m_Description;
        SerializedProperty m_Rating;
        SerializedProperty m_FrameRate;
        SerializedProperty m_Screenshot;
        SerializedProperty m_MetadataEntries;
        Dictionary<TrackAsset, string> m_NameCache = new Dictionary<TrackAsset, string>();

        void OnEnable()
        {
            m_SceneNumber = serializedObject.FindProperty("m_SceneNumber");
            m_ShotName = serializedObject.FindProperty("m_ShotName");
            m_TakeNumber = serializedObject.FindProperty("m_TakeNumber");
            m_Description = serializedObject.FindProperty("m_Description");
            m_Rating = serializedObject.FindProperty("m_Rating");
            m_FrameRate = serializedObject.FindProperty("m_FrameRate");
            m_Screenshot = serializedObject.FindProperty("m_Screenshot");
            m_MetadataEntries = serializedObject.FindProperty("m_MetadataEntries");

            BuildCache();
        }

        void BuildCache()
        {
            m_NameCache.Clear();

            var bindingEntries = serializedObject.FindProperty("m_Entries");

            for (var i = 0; i < bindingEntries.arraySize; ++i)
            {
                var bindingEntry = bindingEntries.GetArrayElementAtIndex(i);
                var track = bindingEntry.FindPropertyRelative("m_Track");
                var exposedPropertyNameProp = bindingEntry
                    .FindPropertyRelative("m_Binding.m_ExposedReference.exposedName");
                var exposedNameStr = exposedPropertyNameProp.stringValue;

                m_NameCache.Add(
                    track.objectReferenceValue as TrackAsset,
                    exposedNameStr);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var startTimeText = target is Take take
                ? take.StartTimecode.ToString()
                : "";

            EditorGUILayout.PropertyField(m_Rating, Contents.Rating);

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(m_FrameRate, Contents.FrameRate);
                EditorGUILayout.TextField(Contents.StartTime, startTimeText);
                EditorGUILayout.PropertyField(m_SceneNumber, Contents.SceneNumber);
                EditorGUILayout.PropertyField(m_ShotName, Contents.ShotName);
                EditorGUILayout.PropertyField(m_TakeNumber, Contents.TakeNumber);
                EditorGUILayout.PropertyField(m_Description, Contents.Description);
                EditorGUILayout.PropertyField(m_Screenshot, Contents.Screenshot);

                DrawMetadata(m_MetadataEntries);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawMetadata(SerializedProperty metadataEntriesProp)
        {
            metadataEntriesProp.isExpanded = EditorGUILayout.Foldout(metadataEntriesProp.isExpanded, Contents.Metadata);

            if (m_MetadataEntries.isExpanded)
            {
                using var _ = new EditorGUI.IndentLevelScope();

                for (var i = 0; i < metadataEntriesProp.arraySize; ++i)
                {
                    var entryProp = metadataEntriesProp.GetArrayElementAtIndex(i);
                    var trackProp = entryProp.FindPropertyRelative("m_Track");
                    var metadataProp = entryProp.FindPropertyRelative("m_Metadata");
                    var track = trackProp.objectReferenceValue as TrackAsset;
                    var isOverride = false;

                    if (!m_NameCache.TryGetValue(track, out var actorName))
                    {
                        var parent = track.parent as TrackAsset;
                        isOverride = parent is AnimationTrack;

                        if (!m_NameCache.TryGetValue(parent, out actorName))
                        {
                            actorName = Contents.Unknown;
                        }
                    }

                    var overrideStr = string.Empty;

                    if (isOverride)
                    {
                        overrideStr = $" ({Contents.OverrideStr})";
                    }

                    var label = new GUIContent($"{track.name}{overrideStr} - {actorName}");

                    EditorGUILayout.PropertyField(metadataProp, label, true);
                }
            }
        }
    }
}
