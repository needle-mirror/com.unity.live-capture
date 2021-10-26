using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    [CustomPropertyDrawer(typeof(FrameNumberAttribute))]
    class FrameNumberDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var frameProp = property.FindPropertyRelative("m_FrameNumber");
            EditorGUI.PropertyField(position, frameProp, label);
        }
    }
}
