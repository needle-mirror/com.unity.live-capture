using System;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera.Editor
{
    class ShowMixedValuesScope : IDisposable
    {
        bool m_ShowMixedValues;
        bool m_Disposed;

        public ShowMixedValuesScope(SerializedProperty property)
        {
            m_ShowMixedValues = EditorGUI.showMixedValue;

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                EditorGUI.showMixedValue = m_ShowMixedValues;
            }

            m_Disposed = true;
        }
    }
}
