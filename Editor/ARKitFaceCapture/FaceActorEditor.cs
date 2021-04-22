using UnityEditor;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    [CustomEditor(typeof(FaceActor))]
    class FaceActorEditor : Editor
    {
        SerializedProperty m_Mapper;
        SerializedProperty m_EnabledChannels;
        SerializedProperty m_Pose;

        void OnEnable()
        {
            m_Mapper = serializedObject.FindProperty("m_Mapper");
            m_EnabledChannels = serializedObject.FindProperty("m_EnabledChannels");
            m_Pose = serializedObject.FindProperty("m_Pose");
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_Mapper);

                if (change.changed)
                {
                    foreach (var target in targets)
                    {
                        if (target is FaceActor actor)
                        {
                            actor.ClearCache();
                        }
                    }
                }
            }

            EditorGUILayout.PropertyField(m_EnabledChannels);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_Pose);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
