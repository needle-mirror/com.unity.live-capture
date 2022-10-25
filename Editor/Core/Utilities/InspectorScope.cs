using System;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    class InspectorScope : IDisposable
    {
        bool m_Disposed;
        bool m_HierarchyMode;
        float m_LabelWidth;

        public InspectorScope(float width, bool fullWidth = false)
        {
            var style = EditorStyles.inspectorDefaultMargins;

            if (fullWidth)
            {
                style = EditorStyles.inspectorFullWidthMargins;
            }

            m_HierarchyMode = EditorGUIUtility.hierarchyMode;
            m_LabelWidth = EditorGUIUtility.labelWidth;

            // EditorGUIUtility.labelWidth uses this formula then using hierarchyMode,
            // only that IMGUIContainer fails to setup a correct contextWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(width * 0.45f - 40, 120f);
            EditorGUIUtility.hierarchyMode = true;
            EditorGUILayout.BeginVertical(style);
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                EditorGUILayout.EndVertical();
                EditorGUIUtility.labelWidth = m_LabelWidth;
                EditorGUIUtility.hierarchyMode = m_HierarchyMode;

                m_Disposed = true;
            }
        }
    }
}