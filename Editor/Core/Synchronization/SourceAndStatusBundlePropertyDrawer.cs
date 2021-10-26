using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    [CustomPropertyDrawer(typeof(Synchronizer.SourceAndStatusBundle))]
    class SourceAndStatusBundlePropertyDrawer : PropertyDrawer
    {
        internal static class Contents
        {
            public static readonly string FieldUndo = "Inspector";
            public static readonly GUIContent EnableSyncToggleTooltip = EditorGUIUtility.TrTextContent("", "Turn on to enable device synchronization");
            public static readonly GUIContent BufferSizeTooltip = EditorGUIUtility.TrTextContent("", "Buffer size in frames");
            public static readonly GUIContent OffsetFramesTooltip = EditorGUIUtility.TrTextContent("", "Local time offset in frames");

            public const float HandleSize = 18f;
            public const float ToggleSize = 16f;
            public const float ToggleColumnWidth = ToggleSize + 3f;
            public const float SyncMonitorSize = 8f;
            public const float StatusTextWidth = 56f;
            public const float FieldPadding = 2f;
            public const float IntFieldWidth = 36f;
            public const float IntColumnWidth = IntFieldWidth + 4;
            public const float MonitorColumnWidth = SyncMonitorSize + 4;
            public const float StatusColumnWidth = MonitorColumnWidth + StatusTextWidth + 12;
            public const float RightSectionWidth = StatusColumnWidth + IntColumnWidth * 2;

            public static readonly Dictionary<TimedSampleStatus, GUIContent> StatusNames = new Dictionary<TimedSampleStatus, GUIContent>
            {
                { TimedSampleStatus.Ok, EditorGUIUtility.TrTextContent("synced", "Source data is synchronized") },
                { TimedSampleStatus.DataMissing, EditorGUIUtility.TrTextContent("no data", "Source data is missing") },
                { TimedSampleStatus.Ahead, EditorGUIUtility.TrTextContent("ahead", "Source data is ahead of global time") },
                { TimedSampleStatus.Behind, EditorGUIUtility.TrTextContent("behind", "Source data is behind global time") }
            };
            public static readonly Dictionary<TimedSampleStatus, Color32> StatusColors = new Dictionary<TimedSampleStatus, Color32>
            {
                { TimedSampleStatus.Ok, new Color32(0x53, 0xc2, 0x2b, 0xff) },
                { TimedSampleStatus.DataMissing,  new Color32(0xb2, 0xb2, 0xb2, 0xff) },
                { TimedSampleStatus.Ahead,  new Color32(0xff, 0xc1, 0x07, 0xff) },
                { TimedSampleStatus.Behind,  new Color32(0xff, 0xc1, 0x07, 0xff) }
            };
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var isSyncedProp = property.FindPropertyRelative("m_SynchronizationRequested");
            var sourceProp = property.FindPropertyRelative("m_Source");
            var statusProp = property.FindPropertyRelative("m_Status");

            var sourceRef = sourceProp.GetValue<TimedDataSourceRef>();
            var source = sourceRef.Resolve();

            if (source == null)
            {
                EditorGUI.LabelField(position, "Source disabled");
                return;
            }

            // Compute the layout
            var right = position;
            right.width = Contents.RightSectionWidth;
            right.x = position.width - right.width + position.x;

            var left = position;
            left.width -= right.width;

            var toggleRect = left;
            toggleRect.width = Contents.ToggleSize;

            var sourceNameRect = left;
            sourceNameRect.x += Contents.ToggleColumnWidth;
            sourceNameRect.width = left.width - Contents.ToggleColumnWidth;

            var monitorRect = right;
            monitorRect.width = Contents.SyncMonitorSize;
            monitorRect.height = Contents.SyncMonitorSize;
            monitorRect.y += (position.height - monitorRect.height) / 2f;

            var statusRect = right;
            statusRect.width = Contents.StatusTextWidth;
            statusRect.x += Contents.MonitorColumnWidth;

            var bufferFieldRect = statusRect;
            bufferFieldRect.x = right.x + Contents.StatusColumnWidth;
            bufferFieldRect.width = Contents.IntFieldWidth;
            bufferFieldRect.y += Contents.FieldPadding;
            bufferFieldRect.height -= Contents.FieldPadding * 2;

            var offsetFieldRect = bufferFieldRect;
            offsetFieldRect.x += Contents.IntColumnWidth;

            // Draw property fields
            EditorGUI.PropertyField(toggleRect, isSyncedProp, GUIContent.none);
            EditorGUI.LabelField(sourceNameRect, source.FriendlyName);
            EditorGUI.DrawRect(monitorRect, Contents.StatusColors[(TimedSampleStatus)statusProp.intValue]);
            EditorGUI.LabelField(statusRect, Contents.StatusNames[(TimedSampleStatus)statusProp.intValue]);

            // Handle numerical fields
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var sourceObject = source as Object;

                var bufferSize = source.BufferSize;
                var sourceOffset = source.PresentationOffset.FrameNumber;
                var isSynced = source.IsSynchronized;

                bufferSize = EditorGUI.IntField(bufferFieldRect, bufferSize);
                sourceOffset = EditorGUI.IntField(offsetFieldRect, sourceOffset);

                if (change.changed)
                {
                    if (sourceObject != null)
                    {
                        Undo.RegisterCompleteObjectUndo(sourceObject, Contents.FieldUndo);
                    }

                    source.IsSynchronized = isSynced;
                    source.BufferSize = bufferSize;
                    source.PresentationOffset = new FrameTime(sourceOffset, source.PresentationOffset.Subframe);

                    if (sourceObject != null)
                    {
                        EditorUtility.SetDirty(sourceObject);
                    }
                }
            }

            // Dummy widgets to show tooltips for fields without labels
            EditorGUI.LabelField(toggleRect, Contents.EnableSyncToggleTooltip);
            EditorGUI.LabelField(bufferFieldRect, Contents.BufferSizeTooltip);
            EditorGUI.LabelField(offsetFieldRect, Contents.OffsetFramesTooltip);
        }
    }
}
