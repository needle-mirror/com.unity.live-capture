using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;

namespace Unity.LiveCapture
{
    [CustomEditor(typeof(SlateDatabase))]
    class SlateDatabaseEditor : Editor
    {
        class Contents
        {
            public static readonly GUIContent slateLabel = new GUIContent("Slate", "The current Slate to play.");
        }

        SlateDatabase m_SlateDatabase;
        SlateInspector m_SlateInspector;

        void OnEnable()
        {
            m_SlateInspector = new SlateInspector();
            m_SlateDatabase = target as SlateDatabase;

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            m_SlateInspector.Refresh();
        }

        public override void OnInspectorGUI()
        {
            if (m_SlateDatabase.slateCount > 0)
            {
                DoSlatePopup();
            }

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SlateInspector.OnGUI(m_SlateDatabase.slate);

                if (change.changed)
                {
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
            }
        }

        void DoSlatePopup()
        {
            var formatter = new UniqueNameFormatter();
            var slates = m_SlateDatabase.GetSlates().ToArray();
            var slateNames = slates.Select(s => formatter.Format(s.shotName)).ToArray();
            var selectedSlate = m_SlateDatabase.slate;
            var index = Array.IndexOf(slates, selectedSlate);

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var newIndex = EditorGUILayout.Popup(Contents.slateLabel, index, slateNames);

                if (change.changed)
                {
                    var slate = default(ISlate);

                    if (newIndex > -1)
                    {
                        slate = slates[newIndex];
                    }

                    if (slate != selectedSlate)
                    {
                        m_SlateDatabase.slate = slate;
                    }

                    if (slate != null)
                    {
                        slate.time = 0d;
                        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                    }
                }
            }
        }
    }
}
