using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    /// <summary>
    /// The base editor for <see cref="Server"/> instances.
    /// </summary>
    /// <remarks>
    /// Inherit from this class when implementing the editor for a custom server.
    /// </remarks>
    [CustomEditor(typeof(Server), true)]
    public class ServerEditor : UnityEditor.Editor
    {
        static class Contents
        {
            public static readonly GUIStyle MenuButtonStyle = new GUIStyle("toolbarbuttonRight")
            {
                padding = new RectOffset(),
                contentOffset = new Vector2(1, 0),
            };
            public static readonly GUILayoutOption[] MenuButtonOptions =
            {
                GUILayout.Width(16f),
            };
            public static readonly GUIContent MenuIcon = EditorGUIUtility.TrIconContent("_Menu");
            public static readonly GUIContent RemoveOption = new GUIContent("Remove Server", "Removes this server.");
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
            if (GUILayout.Button(Contents.MenuIcon, Contents.MenuButtonStyle, Contents.MenuButtonOptions))
            {
                var menu = new GenericMenu();

                menu.AddItem(Contents.RemoveOption, false, () =>
                {
                    ServerManager.Instance.DestroyServer(m_Server);
                });

                menu.ShowAsContext();
            }
        }
    }
}
