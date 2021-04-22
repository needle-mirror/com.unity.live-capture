using UnityEngine;
using UnityEditor;

namespace Unity.LiveCapture
{
    class TakeBindingsEditor : Editor
    {
        static class Contents
        {
            public const string undoSetBinding = "Inspector";
            public static GUIContent bindingsLabel = new GUIContent("Bindings", "The list of scene objects referenced by the Take.");
        }

        SerializedProperty m_EntriesProp;
        Take m_Take;

        void OnEnable()
        {
            m_EntriesProp = serializedObject.FindProperty("m_Entries");
            m_Take = target as Take;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var resolver = GetResolver();

            if (resolver != null)
            {
                m_EntriesProp.isExpanded = EditorGUILayout.Foldout(m_EntriesProp.isExpanded, Contents.bindingsLabel);

                if (m_EntriesProp.isExpanded)
                {
                    DoBindingsGUI(resolver);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DoBindingsGUI(IExposedPropertyTable resolver)
        {
            Debug.Assert(resolver != null);

            var entries = m_Take.bindingEntries;
            var index = 0;

            foreach (var entry in entries)
            {
                var binding = entry.binding;
                var exposedPropertyNameProp = m_EntriesProp.GetArrayElementAtIndex(index++)
                    .FindPropertyRelative("m_Binding.m_ExposedReference.exposedName");
                var exposedNameStr = exposedPropertyNameProp.stringValue;
                var value = binding.GetValue(resolver);
                var position = EditorGUILayout.GetControlRect(false);
                var labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - 2.5f, position.height);
                var valuePosition = new Rect(labelPosition.xMax + 2.5f, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    exposedNameStr = EditorGUI.TextField(labelPosition, GUIContent.none, exposedNameStr);

                    if (change.changed)
                    {
                        exposedPropertyNameProp.stringValue = exposedNameStr;
                    }
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    var newValue = EditorGUI.ObjectField(valuePosition, GUIContent.none, value, binding.type, true);

                    if (change.changed)
                    {
                        var undoObject = resolver as UnityEngine.Object;

                        Undo.RecordObject(undoObject, Contents.undoSetBinding);
                        binding.SetValue(newValue, resolver);

                        EditorUtility.SetDirty(undoObject);
                    }
                }
            }
        }

        IExposedPropertyTable GetResolver()
        {
            return serializedObject.context as IExposedPropertyTable;
        }
    }
}
