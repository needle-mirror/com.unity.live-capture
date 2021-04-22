using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VirtualCameraActor))]
    class VirtualCameraActorEditor : Editor
    {
        static readonly string[] s_ExcludeProperties = { "m_Script" };

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);
            }
        }
    }
}
