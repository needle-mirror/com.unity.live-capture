using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Timeline;

namespace Unity.LiveCapture
{
    [CustomEditor(typeof(TakePlayer))]
    class TakePlayerEditor : Editor
    {
        class Contents
        {
            public static readonly GUIContent takeLabel = EditorGUIUtility.TrTextContent("Take", "The Take to play.");
        }

        SerializedProperty m_TakeProp;
        TakePlayer m_TakePlayer;
        PlayableDirector m_Director;
        Editor m_Editor;
        Type m_EditorType = typeof(TakeBindingsEditor);

        void OnEnable()
        {
            m_TakeProp = serializedObject.FindProperty("m_Take");
            m_TakePlayer = target as TakePlayer;
            m_Director = m_TakePlayer.GetComponent<PlayableDirector>();
        }

        void OnDisable()
        {
            if (m_Editor != null)
            {
                DestroyImmediate(m_Editor);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_TakeProp, Contents.takeLabel);

            serializedObject.ApplyModifiedProperties();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                DoTakeEditor();

                if (change.changed)
                {
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
            }
        }

        void DoTakeEditor()
        {
            var take = m_TakePlayer.take;

            if (take != null)
            {
                CreateCachedEditorWithContext(take, m_Director, m_EditorType, ref m_Editor);

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    m_Editor.OnInspectorGUI();

                    if (change.changed)
                    {
                        m_TakePlayer.Validate();
                    }
                }
            }
        }
    }
}
