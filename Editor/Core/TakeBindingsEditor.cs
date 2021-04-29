using UnityEngine;
using UnityEngine.Playables;
using UnityEditor;

namespace Unity.LiveCapture.Editor
{
    class TakeBindingsEditor : UnityEditor.Editor
    {
        internal static class Contents
        {
            public const string UndoSetBinding = "Inspector";
            public static readonly GUIContent BindingsLabel = EditorGUIUtility.TrTextContent("Bindings", "The list of scene objects referenced by the Take.");
            public static readonly string NullBindingsMsg = EditorGUIUtility.TrTextContent("Missing scene bindings. Set all the required object references in the Bindings list to play this take.").text;
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

            var resolver = serializedObject.context as IExposedPropertyTable;

            if (resolver != null)
            {
                DoBindingWarning(resolver);

                m_EntriesProp.isExpanded = EditorGUILayout.Foldout(m_EntriesProp.isExpanded, Contents.BindingsLabel, true);

                if (m_EntriesProp.isExpanded)
                {
                    DoBindingsGUI(resolver);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DoBindingWarning(IExposedPropertyTable resolver)
        {
            if (ContainsNullBindings(m_Take, resolver))
            {
                EditorGUILayout.HelpBox(Contents.NullBindingsMsg, MessageType.Warning, true);
            }
        }

        internal static bool ContainsNullBindings(Take take, IExposedPropertyTable resolver)
        {
            Debug.Assert(resolver != null);

            var containsNull = false;
            var entries = take.BindingEntries;

            foreach (var entry in entries)
            {
                var binding = entry.Binding;
                var value = binding.GetValue(resolver);

                if (value == null)
                {
                    containsNull = true;
                    break;
                }
            }

            return containsNull;
        }

        void DoBindingsGUI(IExposedPropertyTable resolver)
        {
            Debug.Assert(resolver != null);

            var entries = m_Take.BindingEntries;
            var index = 0;

            foreach (var entry in entries)
            {
                var binding = entry.Binding;
                var exposedPropertyNameProp = m_EntriesProp.GetArrayElementAtIndex(index++)
                    .FindPropertyRelative("m_Binding.m_ExposedReference.exposedName");
                var exposedNameStr = exposedPropertyNameProp.stringValue;
                var value = binding.GetValue(resolver);
                var position = EditorGUILayout.GetControlRect(false);
                var labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - 2.5f, position.height);
                var valuePosition = new Rect(labelPosition.xMax + 2.5f, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

                using (new EditorGUI.DisabledScope(true))
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
                    var newValue = EditorGUI.ObjectField(valuePosition, GUIContent.none, value, binding.Type, true);

                    if (change.changed)
                    {
                        Undo.RecordObject(serializedObject.context, Contents.UndoSetBinding);
                        
                        binding.SetValue(newValue, resolver);

                        EditorUtility.SetDirty(serializedObject.context);
                    }
                }
            }
        }
    }
}
