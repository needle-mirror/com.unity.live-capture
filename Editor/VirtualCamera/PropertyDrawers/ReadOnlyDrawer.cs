using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Draw a field to be read-only on the unity inspector.
    /// </summary>
    /// <remarks>
    /// Field will also be tinted to tell the user it's read only
    /// </remarks>
    [CustomPropertyDrawer(typeof(ReadOnly))]
    class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}
