using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Timeline;

using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture.Editor
{
    [CustomEditor(typeof(SlatePlayableAsset))]
    class SlatePlayableAssetEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly GUIContent AutoClipName = EditorGUIUtility.TrTextContent("Auto Clip Name", "Enable this option to let Unity manage the clip name, or disable it to manually name the clip.");
            public static readonly GUIContent Lock = EditorGUIUtility.TrTextContent("Lock In Take Recorder", "Lock this Clip to keep it active in the Take Recorder regardless of the Timeline playhead position.");
            public static readonly GUIStyle ButtonToggleStyle = "Button";

            public static readonly GUILayoutOption[] LargeButtonOptions =
            {
                GUILayout.Height(30f)
            };
        }

        SerializedProperty m_AutoClipName;
        SlatePlayableAsset m_SlateAsset;
        SlateInspector m_SlateInspector;
        PlayableDirector m_Director;
        PlayableAssetContext m_Context;
        TakeRecorder m_TakeRecorder;

        void OnEnable()
        {
            m_AutoClipName = serializedObject.FindProperty("m_AutoClipName");

            m_SlateInspector = new SlateInspector();
            m_SlateAsset = target as SlatePlayableAsset;
            m_Director = TimelineEditor.inspectedDirector;
            m_TakeRecorder = TakeRecorder.Main;

            var timeline = Timeline.InspectedAsset;

            if (timeline != null && timeline.FindClip(m_SlateAsset, out var clip))
            {
                var hierarchyContext = TimelineHierarchyContextUtility.FromTimelineNavigation();

                m_Context = new PlayableAssetContext(clip, hierarchyContext);
            }

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;

            m_SlateInspector.Dispose();
        }

        void UndoRedoPerformed()
        {
            m_SlateInspector.Refresh();
        }

        public override void OnInspectorGUI()
        {
            DoLockButton();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_AutoClipName, Contents.AutoClipName);

            serializedObject.ApplyModifiedProperties();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SlateInspector.OnGUI(m_SlateAsset, m_Director);

                if (change.changed)
                {
                    TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
                }
            }
        }

        void DoLockButton()
        {
            if (m_TakeRecorder == null)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(m_Context == null))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var value = GUILayout.Toggle(IsLocked(), Contents.Lock,
                    Contents.ButtonToggleStyle, Contents.LargeButtonOptions);

                if (change.changed)
                {
                    if (value)
                    {
                        m_TakeRecorder.LockContext(m_Context);
                    }
                    else
                    {
                        m_TakeRecorder.UnlockContext();
                    }

                    TimelineEditor.Refresh(RefreshReason.WindowNeedsRedraw);
                }
            }
        }

        bool IsLocked()
        {
            Debug.Assert(m_TakeRecorder != null);

            var context = m_TakeRecorder.GetContext() as PlayableAssetContext;

            return m_TakeRecorder.IsLocked()
                && context != null
                && m_SlateAsset == context.GetClip().asset
                && context.GetHierarchyContext().MatchesTimelineNavigation();
        }
    }
}
