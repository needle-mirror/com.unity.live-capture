using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Unity.LiveCapture.Editor
{
    [CustomEditor(typeof(SynchronizerComponent))]
    class SynchronizerEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly string FieldUndo = "Inspector";
            public static readonly string AddDataSource = "Add data source to synchronizer";
            public static readonly string RemoveDataSource = "Remove data source from synchronizer";
            public static readonly GUIContent DisplayTimecodeLabel = EditorGUIUtility.TrTextContent(
                "Display Timecode",
                "Display the current timecode in the Game View (as a burn-in).");
            public static readonly GUIContent TimecodeSourceLabel = EditorGUIUtility.TrTextContent(
                "Timecode Source",
                "The timecode source that corresponds to the primary clock, giving the global time.");
            public static readonly GUIContent GlobalTimeOffsetLabel = EditorGUIUtility.TrTextContent(
                "Global Time Offset",
                "The offset (in frames) applied to the global time used for synchronization updates.\n\n" +
                "Use a negative value to add a delay and compensate for high-latency sources.");
            public static readonly GUIContent DataSourcesLabel = EditorGUIUtility.TrTextContent("Timed data sources");
            public static readonly GUIContent StatusLabel = EditorGUIUtility.TrTextContent("Status");
            public static readonly GUIContent BufferSizeLabel = EditorGUIUtility.TrTextContent("Buffer", "Buffer size in frames");
            public static readonly GUIContent LocalOffsetLabel = EditorGUIUtility.TrTextContent("Offset", "Local time offset in frames");
            public static readonly GUIContent CalibrateLabel = EditorGUIUtility.TrTextContent("Calibrate");
            public static readonly GUIContent StopCalibrateLabel = EditorGUIUtility.TrTextContent("Stop Calibration");
            public static readonly Vector2 OptionDropdownSize = new Vector2(300f, 250f);
        }

        SynchronizerComponent m_Synchronizer;

        SerializedProperty m_DataSourcesProperty;
        ReorderableList m_DataSourceList;

        SerializedProperty m_DisplayTimecodeProperty;
        SerializedProperty m_GlobalTimeOffsetProperty;
        SerializedProperty m_TimecodeSourceProperty;

        void OnEnable()
        {
            m_Synchronizer = target as SynchronizerComponent;
            m_DisplayTimecodeProperty = serializedObject.FindProperty("m_DisplayTimecode");
            m_TimecodeSourceProperty = serializedObject.FindProperty("m_Impl.m_TimecodeSourceRef");
            m_GlobalTimeOffsetProperty = serializedObject.FindProperty("m_Impl.m_GlobalTimeOffset");
            m_DataSourcesProperty = serializedObject.FindProperty("m_Impl.m_SourcesAndStatuses");
            CreateDataSourcesList();
        }

        void CreateDataSourcesList()
        {
            if (m_DataSourceList != null) return;

            m_DataSourceList = new ReorderableList(
                serializedObject,
                m_DataSourcesProperty,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true)
            {
                drawHeaderCallback = rect =>
                {
                    var left = rect;

                    rect.width -= SourceAndStatusBundlePropertyDrawer.Contents.HandleSize;
                    rect.x += SourceAndStatusBundlePropertyDrawer.Contents.HandleSize;

                    var right = rect;
                    right.width = SourceAndStatusBundlePropertyDrawer.Contents.RightSectionWidth;
                    right.x = rect.width - right.width + rect.x;
                    var status = right;
                    status.width = SourceAndStatusBundlePropertyDrawer.Contents.StatusColumnWidth;

                    left.width -= right.width;
                    EditorGUI.LabelField(left, Contents.DataSourcesLabel);                    EditorGUI.LabelField(status, Contents.StatusLabel);

                    right.x += status.width;
                    var intColumn = right;
                    intColumn.width = SourceAndStatusBundlePropertyDrawer.Contents.IntColumnWidth;
                    EditorGUI.LabelField(intColumn, Contents.BufferSizeLabel);

                    intColumn.x += SourceAndStatusBundlePropertyDrawer.Contents.IntColumnWidth;
                    EditorGUI.LabelField(intColumn, Contents.LocalOffsetLabel);
                },
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = m_DataSourcesProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                },
                onCanAddCallback = list => GetUnusedSources().Any(),
                onAddDropdownCallback = (rect, list) =>
                {
                    ShowAddDataSourceMenu();
                },
                onRemoveCallback = list =>
                {
                    Undo.RegisterCompleteObjectUndo(m_Synchronizer, Contents.RemoveDataSource);
                    m_Synchronizer.Impl.RemoveDataSource(m_Synchronizer.Impl.GetDataSource(list.index));
                    EditorUtility.SetDirty(m_Synchronizer);
                }
            };
        }

        void ShowAddDataSourceMenu()
        {
            var sources = GetUnusedSources()
                .Where(s => s != null)
                .OrderBy(s => s.FriendlyName)
                .ToArray();

            var names = sources
                .Select(s => new GUIContent(s.FriendlyName))
                .ToArray();

            var pos = new Rect(Event.current.mousePosition, Vector2.zero);

            OptionSelectWindow.SelectOption(pos, Contents.OptionDropdownSize, names, (index, value) =>
            {
                var source = sources[index];

                if (source is Object sourceObject)
                {
                    Undo.RecordObject(sourceObject, Contents.FieldUndo);
                }

                // Find out if the data source is already part of another synchronization group
                if (source.Synchronizer is {} oldSynchronizer)
                {
                    // Track down the SynchronizerComponent that owns the Synchronizer instance.
                    // Undo tracking requires a UnityEngine.Object target.
                    foreach (var oldSyncObject in Resources.FindObjectsOfTypeAll<SynchronizerComponent>())
                    {
                        if (EditorUtility.IsPersistent(oldSyncObject) || oldSyncObject.hideFlags == HideFlags.NotEditable || oldSyncObject.hideFlags == HideFlags.HideAndDontSave)
                            continue;

                        if (oldSyncObject.Impl == oldSynchronizer)
                        {
                            Undo.RecordObject(oldSyncObject, Contents.FieldUndo);
                            break;
                        }
                    }

                    // Remove source from the old synchronizer
                    oldSynchronizer.RemoveDataSource(source);
                }

                // Add the source to this synchronizer
                Undo.RecordObject(m_Synchronizer, Contents.FieldUndo);
                m_Synchronizer.Impl.AddDataSource(source);

                // FIXME: Can't get this name to show up for Redo.
                Undo.SetCurrentGroupName(Contents.AddDataSource);
            });
        }

        IEnumerable<ITimedDataSource> GetUnusedSources()
        {
            return TimedDataSourceManager.Instance.Where(i => !m_Synchronizer.Impl.ContainsDataSource(i));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DisplayTimecodeProperty, Contents.DisplayTimecodeLabel);
            EditorGUILayout.PropertyField(m_TimecodeSourceProperty, Contents.TimecodeSourceLabel, true);
            EditorGUILayout.PropertyField(m_GlobalTimeOffsetProperty, Contents.GlobalTimeOffsetLabel);

            EditorGUILayout.Space();

            m_DataSourceList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();

            var calibrationStatus = m_Synchronizer.Impl.CalibrationStatus;
            if (calibrationStatus != CalibrationStatus.InProgress)
            {
                if (GUILayout.Button(Contents.CalibrateLabel))
                {
                    m_Synchronizer.StartCalibration();
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(calibrationStatus.ToString());
                    if (GUILayout.Button(Contents.StopCalibrateLabel))
                    {
                        m_Synchronizer.StopCalibration();
                    }
                }
            }
        }
    }
}
