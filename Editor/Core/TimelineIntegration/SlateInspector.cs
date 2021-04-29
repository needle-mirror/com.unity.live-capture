using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;

namespace Unity.LiveCapture.Editor
{
    using Editor = UnityEditor.Editor;

    class SlateInspector : IDisposable
    {
        static class Contents
        {
            public static readonly string FieldUndo = "Inspector";
            public static readonly string SetBaseUndo = "Set Iteration Base";
            public static readonly string ClearBaseUndo = "Clear Iteration Base";
            public static readonly GUIContent FolderIcon = EditorGUIUtility.TrIconContent("Project", "Select a directory.");
            public static readonly GUILayoutOption ButtonSmall = GUILayout.Width(30f);
            public static readonly GUILayoutOption ButtonMid = GUILayout.Width(55f);
            public static readonly GUIContent CurrentBaseIcon = EditorGUIUtility.TrIconContent("Animation.Record", "Set as current Take iteration base.");
            public static readonly GUIContent SetBaseIcon = EditorGUIUtility.TrIconContent("Animation.Record", "Set the Take as iteration base.");
            public static readonly GUIContent IterationBaseLabel = EditorGUIUtility.TrTextContent("Iteration Base", "The Take used as iteration base.");
            public static readonly GUIContent ClearBaseLabel = EditorGUIUtility.TrTextContent("Clear", "Remove the Take used as iteration base.");
            public static readonly GUIContent SceneNumberLabel = EditorGUIUtility.TrTextContent("Scene Number", "The number associated with the scene to record.");
            public static readonly GUIContent ShotNameLabel = EditorGUIUtility.TrTextContent("Shot Name", "The name of the shot.");
            public static readonly GUIContent TakeNumberLabel = EditorGUIUtility.TrTextContent("Take Number", "The number associated with the take to record.");
            public static readonly GUIContent DescriptionLabel = EditorGUIUtility.TrTextContent("Description", "The description of the shot.");
            public static readonly GUIContent DirectoryLabel = EditorGUIUtility.TrTextContent("Directory", "The directory where the Takes are stored.");
            public static readonly GUIContent TakesLabel = EditorGUIUtility.TrTextContent("Takes", "The available Takes in the selected directory.");
            public static readonly GUIContent NoTakesLabel = EditorGUIUtility.TrTextContent("No takes available in directory", "No Takes available in the selected directory.");
            public static readonly GUIContent Take = EditorGUIUtility.TrTextContent("Take", "The Take to play.");
            public static GUIStyle TextAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        }

        PlayableDirector m_Director;
        List<Take> m_Takes;
        CompactList m_TakeList;
        ISlate m_Slate;
        int m_SceneNumber;
        string m_ShotName;
        int m_TakeNumber;
        string m_Description;
        string m_Directory;
        Take m_Take;
        Editor m_Editor;
        Type m_EditorType = typeof(TakeBindingsEditor);

        public void Dispose()
        {
            if (m_Editor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Editor);
            }
        }

        public void Refresh()
        {
            RefreshCache();
        }

        public void OnGUI(ISlate slate, PlayableDirector director)
        {
            m_Director = director;

            if (m_Slate != slate)
            {
                m_Slate = slate;

                PrepareTakes(m_Slate);
            }
            else
            {
                HandleSlateChangedExternally();
            }

            using (new EditorGUI.DisabledScope(m_Slate == null))
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var sceneNumber = EditorGUILayout.IntField(Contents.SceneNumberLabel, m_SceneNumber);

                    if (change.changed)
                    {
                        m_SceneNumber = Mathf.Max(1, sceneNumber);

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Contents.FieldUndo);

                        m_Slate.SceneNumber = m_SceneNumber;

                        EditorUtility.SetDirty(m_Slate.UnityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var name = EditorGUILayout.DelayedTextField(Contents.ShotNameLabel, m_ShotName);

                    if (change.changed && !string.IsNullOrEmpty(name))
                    {
                        m_ShotName = FileNameFormatter.Instance.Format(name);

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Contents.FieldUndo);

                        m_Slate.ShotName = m_ShotName;

                        EditorUtility.SetDirty(m_Slate.UnityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var takeNumber = EditorGUILayout.IntField(Contents.TakeNumberLabel, m_TakeNumber);

                    if (change.changed)
                    {
                        m_TakeNumber = Mathf.Max(1, takeNumber);

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Contents.FieldUndo);

                        m_Slate.TakeNumber = m_TakeNumber;

                        EditorUtility.SetDirty(m_Slate.UnityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.LabelField(Contents.DescriptionLabel, GUIContent.none);

                    var description = EditorGUILayout.TextArea(m_Description,
                        Contents.TextAreaStyle,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f));

                    if (change.changed)
                    {
                        m_Description = description;

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Contents.FieldUndo);

                        m_Slate.Description = m_Description;

                        EditorUtility.SetDirty(m_Slate.UnityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var path = DoDirectoryField(m_Directory);

                    if (change.changed)
                    {
                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Contents.FieldUndo);

                        m_Slate.Directory = path;

                        PrepareTakes(m_Slate);

                        EditorUtility.SetDirty(m_Slate.UnityObject);
                    }
                }

                if (m_TakeList != null)
                {
                    EditorGUILayout.LabelField(Contents.TakesLabel);
                    m_TakeList.DoGUILayout();
                }

                DoIterationBaseGUI();
                DoTakeEditor(director);
            }
        }

        void DoIterationBaseGUI()
        {
            var iterationBase = default(Take);

            if (m_Slate != null)
            {
                iterationBase = m_Slate.IterationBase;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(true))
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    iterationBase = EditorGUILayout.ObjectField(Contents.IterationBaseLabel,
                        iterationBase, typeof(Take), false) as Take;

                    if (change.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Contents.SetBaseUndo);

                        m_Slate.IterationBase = iterationBase;

                        EditorUtility.SetDirty(m_Slate.UnityObject);
                    }
                }

                using (new EditorGUI.DisabledScope(iterationBase == null))
                {
                    if (GUILayout.Button(Contents.ClearBaseLabel, Contents.ButtonMid))
                    {
                        SetIterationBase(null, Contents.ClearBaseUndo);
                    }
                }
            }
        }

        string DoDirectoryField(string path)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                path = EditorGUILayout.DelayedTextField(Contents.DirectoryLabel, path);

                if (GUILayout.Button(Contents.FolderIcon, EditorStyles.miniButton, Contents.ButtonSmall))
                {
                    var newPath = EditorUtility.OpenFolderPanel(Contents.DirectoryLabel.text, path, string.Empty);
                    var assetsPath = Application.dataPath;

                    if (string.Compare(newPath, path) != 0 &&
                        newPath.StartsWith(assetsPath))
                    {
                        var projectPath = assetsPath.Substring(0, assetsPath.Length - "Assets/".Length + 1);

                        path = newPath.Substring(projectPath.Length);
                    }
                }
            }

            return path;
        }

        void PrepareTakes(ISlate slate)
        {
            m_Slate = slate;

            RefreshCache();
        }

        void RefreshCache()
        {
            if (m_Slate == null)
            {
                m_SceneNumber = 1;
                m_ShotName = string.Empty;
                m_TakeNumber = 1;
                m_Description = string.Empty;
                m_Directory = string.Empty;
                m_Take = null;
                m_Takes = new List<Take>();
            }
            else
            {
                m_SceneNumber = m_Slate.SceneNumber;
                m_ShotName = m_Slate.ShotName;
                m_TakeNumber = m_Slate.TakeNumber;
                m_Description = m_Slate.Description;
                m_Directory = m_Slate.Directory;
                m_Take = m_Slate.Take;
                m_Takes = AssetDatabaseUtility.GetAssetsAtPath<Take>(m_Directory);
            }

            CreateTakeList();
        }

        void CreateTakeList()
        {
            var instanceID = 0;

            if (m_Slate != null && m_Slate.UnityObject != null)
            {
                instanceID = m_Slate.UnityObject.GetInstanceID();
            }

            m_TakeList = new CompactList(m_Takes, $"{instanceID}/takes");
            m_TakeList.OnCanAddCallback = () => false;
            m_TakeList.OnCanRemoveCallback = () => false;
            m_TakeList.Reorderable = false;
            m_TakeList.Searchable = false;
            m_TakeList.Index = m_Takes.IndexOf(m_Take);
            m_TakeList.DrawListItemCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_Takes[index] as Take;

                if (element == null)
                {
                    return;
                }

                rect.height = EditorGUIUtility.singleLineHeight;

                var selected = m_TakeList.Index == index;
                var isIterationBase = element == m_Slate.IterationBase;
                var buttonWidth = 30f;
                var rect1 = rect;
                var rect2 = rect;

                rect1.width -= buttonWidth - 5f;
                rect2.x = rect2.xMax - buttonWidth;
                rect2.width = buttonWidth;

                EditorGUI.LabelField(rect1, element.name);

                if (isIterationBase)
                {
                    DoSlectedBaseButtonGUI(rect2);
                }
                else if (selected)
                {
                    DoSetBaseButtonGUI(rect2, element);
                }
            };
            m_TakeList.DrawNoneListItemCallback = rect => EditorGUI.LabelField(rect, Contents.NoTakesLabel);

            m_TakeList.OnSelectCallback += OnSelectCallback;
        }

        void DoSetBaseButtonGUI(Rect rect, Take take)
        {
            if (GUI.Button(rect, Contents.SetBaseIcon))
            {
                SetIterationBase(take, Contents.SetBaseUndo);
            }
        }

        void DoSlectedBaseButtonGUI(Rect rect)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                GUI.Button(rect, Contents.CurrentBaseIcon);
            }
        }

        void OnSelectCallback()
        {
            Debug.Assert(m_Slate != null);

            var currentTake = m_Slate.Take;
            var newTake = m_Takes[m_TakeList.Index];

            RegisterDirectorUndo();
            ClearSceneBindings();

            if (currentTake != newTake)
            {
                Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, Undo.GetCurrentGroupName());

                m_Take = newTake;
                m_Slate.Take = newTake;

                GUI.changed = true;

                EditorUtility.SetDirty(m_Slate.UnityObject);
            }

            SetSceneBindings();
        }

        void SetIterationBase(Take iterationBase, string undoMessage)
        {
            RegisterDirectorUndo();
            ClearSceneBindings();

            Undo.RegisterCompleteObjectUndo(m_Slate.UnityObject, undoMessage);

            m_Slate.IterationBase = iterationBase;

            EditorUtility.SetDirty(m_Slate.UnityObject);

            SetSceneBindings();
        }

        void HandleSlateChangedExternally()
        {
            if (m_Slate == null)
            {
                return;
            }

            if (m_Take != m_Slate.Take ||
                m_SceneNumber != m_Slate.SceneNumber ||
                m_ShotName != m_Slate.ShotName ||
                m_Description != m_Slate.Description ||
                m_Directory != m_Slate.Directory)
            {
                RefreshCache();
            }
        }

        void DoTakeEditor(PlayableDirector director)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(Contents.Take, m_Take, typeof(Take), false);
            }

            if (director == null || m_Take == null)
            {
                return;
            }

            Editor.CreateCachedEditorWithContext(m_Take, director, m_EditorType, ref m_Editor);
            
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_Editor.OnInspectorGUI();

                if (change.changed)
                {
                    m_Slate.SetSceneBindings(director);
                }
            }
        }

        void RegisterDirectorUndo()
        {
            if (m_Director != null)
            {
                Undo.RegisterCompleteObjectUndo(m_Director, Undo.GetCurrentGroupName());
            }
        }

        void ClearSceneBindings()
        {
            if (m_Director != null && m_Slate != null)
            {
                m_Slate.ClearSceneBindings(m_Director);

                EditorUtility.SetDirty(m_Director);
            }
        }

        void SetSceneBindings()
        {
            if (m_Director != null && m_Slate != null)
            {
                m_Slate.SetSceneBindings(m_Director);

                EditorUtility.SetDirty(m_Director);
            }
        }
    }
}
