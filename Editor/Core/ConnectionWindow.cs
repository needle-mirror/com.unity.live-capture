using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// A window used to manage connections to external devices.
    /// </summary>
    public class ConnectionWindow : EditorWindow
    {
        static readonly Vector2 k_WindowSize = new Vector2(300f, 100f);

        static class Contents
        {
            static readonly string k_IconPath = $"Packages/{LiveCaptureInfo.Name}/Editor/Core/Icons";

            public const string WindowName = "Connections";
            public const string WindowPath = "Window/Live Capture/" + WindowName;
            public static readonly GUIContent WindowTitle = EditorGUIUtility.TrTextContentWithIcon(WindowName, $"{k_IconPath}/LiveCaptureConnectionWindow.png");
            public static readonly GUIContent FirewallConfigureLabel = EditorGUIUtility.TrTextContent("Configure Firewall", "Add rules to the firewall that enable Unity to receive connections on private or work networks.");
            public static readonly GUIContent CreateConnectionLabel = EditorGUIUtility.TrTextContent("Add Connection", "Create a Connection used to communicate with external devices.");
            public static readonly string FirewallNotConfiguredMessage = "The firewall is not configured optimally for Live Capture. You may experience difficulty connecting devices to Unity.";

            public static readonly GUILayoutOption[] LargeButtonOptions =
            {
                GUILayout.Width(230f),
                GUILayout.Height(24f)
            };
        }

        static IEnumerable<(Type, CreateConnectionMenuItemAttribute[])> s_CreateConnectionMenuItems;
        static bool s_FirewallConfigured;

        [SerializeField]
        Vector2 m_Scroll;

        readonly Dictionary<Connection, ConnectionEditor> m_EditorCache = new Dictionary<Connection, ConnectionEditor>();

        /// <summary>
        /// Opens an instance of the connections window.
        /// </summary>
        [MenuItem(Contents.WindowPath)]
        public static void ShowWindow()
        {
            GetWindow<ConnectionWindow>();
        }

        void OnEnable()
        {
            titleContent = Contents.WindowTitle;
            minSize = k_WindowSize;

            Undo.undoRedoPerformed += Repaint;
            ConnectionManager.ConnectionChanged += Repaint;

            if (FirewallUtility.IsSupported)
            {
                s_FirewallConfigured = FirewallUtility.IsConfigured();
                FirewallUtility.FirewallConfigured += OnFirewallConfigured;
            }
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            ConnectionManager.ConnectionChanged -= Repaint;

            if (FirewallUtility.IsSupported)
            {
                FirewallUtility.FirewallConfigured -= OnFirewallConfigured;
            }

            foreach (var editor in m_EditorCache.Values)
            {
                DestroyImmediate(editor);
            }

            m_EditorCache.Clear();
        }

        static void OnFirewallConfigured(bool successful)
        {
            s_FirewallConfigured = successful;
        }

        void OnGUI()
        {
            using var scrollView = new EditorGUILayout.ScrollViewScope(m_Scroll);
            m_Scroll = scrollView.scrollPosition;

            EditorGUIUtility.wideMode = true;

            DoFirewallGUI();
            DoConnectionsGUI();
        }

        void DoFirewallGUI()
        {
            if (FirewallUtility.IsSupported && !s_FirewallConfigured)
            {
                EditorGUILayout.HelpBox(Contents.FirewallNotConfiguredMessage, MessageType.Warning);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(Contents.FirewallConfigureLabel, Contents.LargeButtonOptions))
                    {
                        FirewallUtility.ConfigureFirewall();
                    }

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(10f);
            }
        }

        void DoConnectionsGUI()
        {
            foreach (var connection in ConnectionManager.Instance.Connections)
            {
                DoConnectionGUI(connection);
            }

            EditorGUILayout.Space();

            DoCreateConnectionGUI();
        }

        void DoConnectionGUI(Connection connection)
        {
            if (!m_EditorCache.TryGetValue(connection, out var editor))
            {
                editor = Editor.CreateEditor(connection) as ConnectionEditor;
                m_EditorCache.Add(connection, editor);
            }
            else if (!ReferenceEquals(connection, editor.target))
            {
                DestroyImmediate(editor);
                editor = Editor.CreateEditor(connection) as ConnectionEditor;
                m_EditorCache[connection] = editor;
            }

            var serializedObject = editor.serializedObject;
            var expandedProp = serializedObject.FindProperty("m_Expanded");

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                serializedObject.Update();

                expandedProp.boolValue = EditorGUILayout.Foldout(expandedProp.boolValue, connection.GetName(), true);

                serializedObject.ApplyModifiedProperties();
                editor.OnToolbarGUI();
            }

            if (expandedProp.boolValue)
            {
                EditorGUIUtility.hierarchyMode = true;

                using (new EditorGUI.IndentLevelScope())
                {
                    editor.OnInspectorGUI();
                }

                EditorGUIUtility.hierarchyMode = false;
            }
        }

        static void DoCreateConnectionGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Contents.CreateConnectionLabel, Contents.LargeButtonOptions))
                {
                    if (s_CreateConnectionMenuItems == null)
                    {
                        s_CreateConnectionMenuItems = AttributeUtility.GetAllTypes<CreateConnectionMenuItemAttribute>();
                    }

                    var menu = MenuUtility.CreateMenu(s_CreateConnectionMenuItems, (t) => !ConnectionManager.Instance.HasConnection(t), (type, attribute) =>
                    {
                        ConnectionManager.Instance.CreateConnection(type);
                    });

                    menu.ShowAsContext();
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
