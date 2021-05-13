using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;

using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture.Editor
{
    [CustomEditor(typeof(SlatePlayableAsset))]
    class SlatePlayableAssetEditor : UnityEditor.Editor
    {
        SerializedProperty m_DirectoryProp;
        SerializedProperty m_NameProp;
        SlatePlayableAsset m_SlateAsset;
        SlateInspector m_SlateInspector;

        void OnEnable()
        {
            m_SlateInspector = new SlateInspector();
            m_SlateAsset = target as SlatePlayableAsset;

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
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SlateInspector.OnGUI(m_SlateAsset);

                if (change.changed)
                {
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
            }
        }
    }
}
