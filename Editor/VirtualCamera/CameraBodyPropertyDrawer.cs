using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CustomPropertyDrawer(typeof(CameraBody))]
    class CameraBodyPropertyDrawer : PropertyDrawer
    {
        internal static class Contents
        {
            public static GUIContent bodyLabel = EditorGUIUtility.TrTextContent("Camera Body");
            public static GUIContent iso = EditorGUIUtility.TrTextContent("ISO", "Set the sensibility of the real-world " +
                "camera sensor. Higher values increase the Camera's sensitivity to light and result in faster exposure times.");
            public static GUIContent shutterSpeed = EditorGUIUtility.TrTextContent("Shutter Speed", "Sets the exposure time " +
                "in seconds for the camera. Lower values result in less exposed pictures.");
        }

        SerializedProperty m_SensorSizeProp;
        SerializedProperty m_IsoProp;
        SerializedProperty m_ShutterSpeedProp;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var lineHeight = GetLineHeight() + 2f;
            var rowCount = 5;

            return lineHeight * rowCount - 2f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_SensorSizeProp = property.FindPropertyRelative("sensorSize");
            m_IsoProp = property.FindPropertyRelative("iso");
            m_ShutterSpeedProp = property.FindPropertyRelative("shutterSpeed");

            position = BeginLine(position);

            EditorGUI.LabelField(position, Contents.bodyLabel, EditorStyles.boldLabel);

            using (new EditorGUI.IndentLevelScope())
            {
                position = NextLine(position);

                DoBodyGUI(ref position, property);
            }
        }

        void DoBodyGUI(ref Rect position, SerializedProperty property)
        {
            EditorGUI.PropertyField(position, m_SensorSizeProp);

            position = NextLine(position);
            position = NextLine(position);

            EditorGUI.PropertyField(position, m_IsoProp, Contents.iso);

            position = NextLine(position);

            EditorGUI.PropertyField(position, m_ShutterSpeedProp, Contents.shutterSpeed);
        }

        float GetLineHeight()
        {
            return EditorGUIUtility.singleLineHeight;
        }

        Rect BeginLine(Rect position)
        {
            return new Rect(
                position.x,
                position.y,
                position.width,
                GetLineHeight());
        }

        Rect NextLine(Rect position)
        {
            return new Rect(
                position.x,
                position.y + position.height + 2f,
                position.width,
                GetLineHeight());
        }
    }
}
