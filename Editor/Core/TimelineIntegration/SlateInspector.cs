using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Unity.LiveCapture
{
    class SlateInspector
    {
        class Contents
        {
            public static readonly string fieldUndo = "Inspector";
            public static readonly string setBaseUndo = "Set Iteration Base";
            public static readonly string clearBaseUndo = "Clear Iteration Base";
            public static readonly GUIContent folderIcon = EditorGUIUtility.TrIconContent("Project", "Select a directory.");
            public static readonly GUILayoutOption buttonSmall = GUILayout.Width(30f);
            public static readonly GUILayoutOption buttonMid = GUILayout.Width(55f);
            public static readonly GUIContent currentBaseIcon = EditorGUIUtility.TrIconContent("Animation.Record", "Set as current Take iteration base.");
            public static readonly GUIContent setBaseIcon = EditorGUIUtility.TrIconContent("Animation.Record", "Set the Take as iteration base.");
            public static readonly GUIContent iterationBaseLabel = EditorGUIUtility.TrTextContent("Iteration Base", "The Take used as iteration base.");
            public static readonly GUIContent clearBaseLabel = EditorGUIUtility.TrTextContent("Clear", "Remove the Take used as iteration base.");
            public static readonly GUIContent sceneNumberLabel = EditorGUIUtility.TrTextContent("Scene Number", "The number associated with the scene to record.");
            public static readonly GUIContent shotNameLabel = EditorGUIUtility.TrTextContent("Shot Name", "The name of the shot.");
            public static readonly GUIContent takeNumberLabel = EditorGUIUtility.TrTextContent("Take Number", "The number associated with the take to record.");
            public static readonly GUIContent descriptionLabel = EditorGUIUtility.TrTextContent("Description", "The description of the shot.");
            public static readonly GUIContent directoryLabel = EditorGUIUtility.TrTextContent("Directory", "The directory where the Takes are stored.");
            public static readonly GUIContent takesLabel = EditorGUIUtility.TrTextContent("Takes", "The available Takes in the selected directory.");
            public static GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
        }

        List<Take> m_Takes;
        CompactList m_TakeList;
        ISlate m_Slate;
        int m_SceneNumber;
        string m_ShotName;
        int m_TakeNumber;
        string m_Description;
        string m_Directory;
        Take m_Take;

        public void Refresh()
        {
            RefreshCache();
        }

        public void OnGUI(ISlate slate)
        {
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
                    var sceneNumber = EditorGUILayout.IntField(Contents.sceneNumberLabel, m_SceneNumber);

                    if (change.changed)
                    {
                        m_SceneNumber = Mathf.Max(1, sceneNumber);

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.fieldUndo);

                        m_Slate.sceneNumber = m_SceneNumber;

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var name = EditorGUILayout.DelayedTextField(Contents.shotNameLabel, m_ShotName);

                    if (change.changed && !string.IsNullOrEmpty(name))
                    {
                        m_ShotName = FileNameFormatter.instance.Format(name);

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.fieldUndo);

                        m_Slate.shotName = m_ShotName;

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var takeNumber = EditorGUILayout.IntField(Contents.takeNumberLabel, m_TakeNumber);

                    if (change.changed)
                    {
                        m_TakeNumber = Mathf.Max(1, takeNumber);

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.fieldUndo);

                        m_Slate.takeNumber = m_TakeNumber;

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.LabelField(Contents.descriptionLabel, GUIContent.none);

                    var description = EditorGUILayout.TextArea(m_Description,
                        Contents.textAreaStyle,
                        GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f));

                    if (change.changed)
                    {
                        m_Description = description;

                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.fieldUndo);

                        m_Slate.description = m_Description;

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var path = DoDirectoryField(m_Directory);

                    if (change.changed)
                    {
                        Debug.Assert(m_Slate != null);

                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.fieldUndo);

                        m_Slate.directory = path;

                        PrepareTakes(m_Slate);

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }

                if (m_TakeList != null)
                {
                    EditorGUILayout.LabelField(Contents.takesLabel);
                    m_TakeList.DoGUILayout();
                }

                if (m_Slate != null)
                {
                    DoIterationBaseGUI();
                }
            }
        }

        void DoIterationBaseGUI()
        {
            Debug.Assert(m_Slate != null);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var iterationBase = EditorGUILayout.ObjectField(Contents.iterationBaseLabel,
                        m_Slate.iterationBase, typeof(Take), false) as Take;

                    if (change.changed)
                    {
                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.setBaseUndo);

                        m_Slate.iterationBase = iterationBase;

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }

                using (new EditorGUI.DisabledScope(m_Slate.iterationBase == null))
                {
                    if (GUILayout.Button(Contents.clearBaseLabel, Contents.buttonMid))
                    {
                        Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.clearBaseUndo);

                        m_Slate.iterationBase = null;

                        EditorUtility.SetDirty(m_Slate.unityObject);
                    }
                }
            }
        }

        string DoDirectoryField(string path)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                path = EditorGUILayout.DelayedTextField(Contents.directoryLabel, path);

                if (GUILayout.Button(Contents.folderIcon, EditorStyles.miniButton, Contents.buttonSmall))
                {
                    var newPath = EditorUtility.OpenFolderPanel(Contents.directoryLabel.text, path, string.Empty);
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
                m_SceneNumber = m_Slate.sceneNumber;
                m_ShotName = m_Slate.shotName;
                m_TakeNumber = m_Slate.takeNumber;
                m_Description = m_Slate.description;
                m_Directory = m_Slate.directory;
                m_Take = m_Slate.take;
                m_Takes = AssetDatabaseUtility.GetAssetsAtPath<Take>(m_Directory);
            }

            CreateTakeList();
        }

        void CreateTakeList()
        {
            m_TakeList = new CompactList(m_Takes, $"{m_Slate.unityObject.GetInstanceID()}/takes");
            m_TakeList.onCanAddCallback = () => false;
            m_TakeList.onCanRemoveCallback = () => false;
            m_TakeList.reorderable = false;
            m_TakeList.index = m_Takes.IndexOf(m_Take);
            m_TakeList.drawListItemCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_Takes[index] as Take;

                rect.height = EditorGUIUtility.singleLineHeight;

                var selected = m_TakeList.index == index;
                var isIterationBase = element == m_Slate.iterationBase;
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

            m_TakeList.onSelectCallback += OnSelectCallback;
        }

        void DoSetBaseButtonGUI(Rect rect, Take selectedTake)
        {
            if (GUI.Button(rect, Contents.setBaseIcon))
            {
                Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Contents.setBaseUndo);

                m_Slate.iterationBase = selectedTake;

                EditorUtility.SetDirty(m_Slate.unityObject);
            }
        }

        void DoSlectedBaseButtonGUI(Rect rect)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                GUI.Button(rect, Contents.currentBaseIcon);
            }
        }

        void OnSelectCallback()
        {
            var take = m_Takes[m_TakeList.index];

            if (m_Slate.take != take)
            {
                Undo.RegisterCompleteObjectUndo(m_Slate.unityObject, Undo.GetCurrentGroupName());

                m_Take = take;
                m_Slate.take = take;

                GUI.changed = true;

                EditorUtility.SetDirty(m_Slate.unityObject);
            }
        }

        void HandleSlateChangedExternally()
        {
            if (m_Slate == null)
            {
                return;
            }

            if (m_Take != m_Slate.take ||
                m_SceneNumber != m_Slate.sceneNumber ||
                m_ShotName != m_Slate.shotName ||
                m_Description != m_Slate.description ||
                m_Directory != m_Slate.directory)
            {
                RefreshCache();
            }
        }
    }
}
