using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// The base editor for all <see cref="Server"/> instances.
    /// </summary>
    [CustomEditor(typeof(Server), true)]
    public class ServerEditor : Editor
    {
        static class Contents
        {
            public static readonly GUIStyle menuButtonStyle = new GUIStyle("toolbarbuttonRight")
            {
                padding = new RectOffset(),
                contentOffset = new Vector2(1, 0),
            };
            public static readonly GUILayoutOption[] menuButtonOptions =
            {
                GUILayout.Width(16f),
            };
            public static readonly GUIContent menuIcon = EditorGUIUtility.TrIconContent("_Menu");
            public static readonly GUIContent removeOption = new GUIContent("Remove Server", "Removes this server.");
        }

        Server m_Server;

        /// <summary>
        /// Initializes the server inspector.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_Server = target as Server;
        }

        /// <summary>
        /// Draws additional controls to the server's header toolbar.
        /// </summary>
        public virtual void OnToolbarGUI()
        {
            DoToolbarMenu();
        }

        /// <summary>
        /// Draws the inspector for this server.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the server menu in the toolbar.
        /// </summary>
        protected void DoToolbarMenu()
        {
            if (GUILayout.Button(Contents.menuIcon, Contents.menuButtonStyle, Contents.menuButtonOptions))
            {
                var menu = new GenericMenu();

                menu.AddItem(Contents.removeOption, false, () =>
                {
                    ServerManager.instance.DestroyServer(m_Server);
                });

                menu.ShowAsContext();
            }
        }
    }
}
