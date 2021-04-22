using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    [CustomEditor(typeof(Take), true)]
    class TakeEditor : Editor
    {
        static class Contents
        {
            public static string unknown = "Unknow";
            public static string overrideStr = "Override";
            public static GUIContent frameRate = EditorGUIUtility.TrTextContent("Frame Rate", "The frame rate used during the recording.");
            public static GUIContent sceneNumber = EditorGUIUtility.TrTextContent("Scene Number", "The number associated with the scene where the take was captured.");
            public static GUIContent shotName = EditorGUIUtility.TrTextContent("Shot Name", "The name of the shot where the take was captured.");
            public static GUIContent takeNumber = EditorGUIUtility.TrTextContent("Take Number", "The number associated with the take.");
            public static GUIContent description = EditorGUIUtility.TrTextContent("Description", "The description of the shot where the take was captured.");
            public static GUIContent metadata = EditorGUIUtility.TrTextContent("Metadata", "Metadata associated with the recorded contents.");
        }

        SerializedProperty m_SceneNumberProp;
        SerializedProperty m_ShotNameProp;
        SerializedProperty m_TakeNumberProp;
        SerializedProperty m_DescriptionProp;
        SerializedProperty m_FrameRateProp;
        SerializedProperty m_MetadataEntriesProp;
        Dictionary<TrackAsset, string> m_NameCache = new Dictionary<TrackAsset, string>();

        void OnEnable()
        {
            m_SceneNumberProp = serializedObject.FindProperty("m_SceneNumber");
            m_ShotNameProp = serializedObject.FindProperty("m_ShotName");
            m_TakeNumberProp = serializedObject.FindProperty("m_TakeNumber");
            m_DescriptionProp = serializedObject.FindProperty("m_Description");
            m_FrameRateProp = serializedObject.FindProperty("m_FrameRate");
            m_MetadataEntriesProp = serializedObject.FindProperty("m_MetadataEntries");

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

            using (new EditorGUI.DisabledGroupScope(true))
            {
                EditorGUILayout.PropertyField(m_FrameRateProp, Contents.frameRate);
                EditorGUILayout.PropertyField(m_SceneNumberProp, Contents.sceneNumber);
                EditorGUILayout.PropertyField(m_ShotNameProp, Contents.shotName);
                EditorGUILayout.PropertyField(m_TakeNumberProp, Contents.takeNumber);
                EditorGUILayout.PropertyField(m_DescriptionProp, Contents.description);

                DrawMetadata(m_MetadataEntriesProp);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawMetadata(SerializedProperty metadataEntriesProp)
        {
            metadataEntriesProp.isExpanded = EditorGUILayout.Foldout(metadataEntriesProp.isExpanded, Contents.metadata);

            if (m_MetadataEntriesProp.isExpanded)
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
                            actorName = Contents.unknown;
                        }
                    }

                    var overrideStr = string.Empty;

                    if (isOverride)
                    {
                        overrideStr = $" ({Contents.overrideStr})";
                    }

                    var label = new GUIContent($"{track.name}{overrideStr} - {actorName}");

                    EditorGUILayout.PropertyField(metadataProp, label, true);
                }
            }
        }
    }
}
