using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    /// <summary>
    /// The base editor for <see cref="Connection"/> instances.
    /// </summary>
    /// <remarks>
    /// Inherit from this class when implementing the editor for a custom <see cref="Connection"/>.
    /// </remarks>
    [CustomEditor(typeof(Connection), true)]
    public class ConnectionEditor : UnityEditor.Editor
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
            public static readonly GUIContent RemoveOption = new GUIContent("Remove Connection", "Removes this connection.");
        }

        /// <summary>
        /// Initializes the connection inspector.
        /// </summary>
        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Draws additional controls to the connection's header toolbar.
        /// </summary>
        public virtual void OnToolbarGUI()
        {
            DoToolbarMenu();
        }

        /// <summary>
        /// Draws the inspector for this connection.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the connection menu in the toolbar.
        /// </summary>
        protected void DoToolbarMenu()
        {
            if (GUILayout.Button(Contents.MenuIcon, Contents.MenuButtonStyle, Contents.MenuButtonOptions))
            {
                var menu = new GenericMenu();

                menu.AddItem(Contents.RemoveOption, false, () =>
                {
                    ConnectionManager.Instance.DestroyConnection(target as Connection);
                });

                menu.ShowAsContext();
            }
        }
    }
}
