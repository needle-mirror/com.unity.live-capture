using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>
    /// The main window used to interact with live capture.
    /// </summary>
    class ServerWindow : EditorWindow
    {
        static readonly Vector2 k_WindowSize = new Vector2(300f, 100f);

        static class Contents
        {
            public static readonly GUILayoutOption[] LargeButtonOptions =
            {
                GUILayout.Width(230f),
                GUILayout.Height(24f)
            };
            static readonly string k_IconPath = "Packages/com.unity.live-capture/Editor/Core/Icons";
            public static readonly GUIContent WindowTitle = EditorGUIUtility.TrTextContentWithIcon("Connections", $"{k_IconPath}/LiveCaptureConnectionWindow.png");
            public static readonly GUIContent FirewallConfigureLabel = EditorGUIUtility.TrTextContent("Configure Firewall", "Add rules to the firewall that enable Unity to receive connections on private or work networks.");
            public static readonly GUIContent CreateServerLabel = EditorGUIUtility.TrTextContent("Create Server", "Create a Server of the selected type.");
        }

        static IEnumerable<(Type, CreateServerMenuItemAttribute[])> s_CreateServerMenuItems;
        static bool s_FirewallConfigured;

        [SerializeField]
        Vector2 m_Scroll;
        Editor m_Editor;

        [MenuItem("Window/Live Capture/Connections")]
        public static void ShowWindow()
        {
            var window = GetWindow<ServerWindow>();

            window.minSize = k_WindowSize;
        }

        void OnEnable()
        {
            titleContent = Contents.WindowTitle;

            Undo.undoRedoPerformed += Repaint;
            ServerManager.ServerChanged += Repaint;

            if (FirewallUtility.IsSupported)
            {
                s_FirewallConfigured = FirewallUtility.IsConfigured();
                FirewallUtility.FirewallConfigured += OnFirewallConfigured;
            }
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            ServerManager.ServerChanged -= Repaint;

            if (FirewallUtility.IsSupported)
            {
                FirewallUtility.FirewallConfigured -= OnFirewallConfigured;
            }

            if (m_Editor != null)
                DestroyImmediate(m_Editor);
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
            DoServersGUI();
        }

        void DoFirewallGUI()
        {
            if (FirewallUtility.IsSupported && !s_FirewallConfigured)
            {
                EditorGUILayout.HelpBox("The firewall is not configured optimally for Live Capture. You may experience difficulty connecting devices to Unity.", MessageType.Warning);

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

        void DoServersGUI()
        {
            foreach (var server in ServerManager.Instance.Servers)
            {
                DoServerGUI(server);
            }

            EditorGUILayout.Space();

            DoCreateServerGUI();
        }

        void DoServerGUI(Server server)
        {
            Editor.CreateCachedEditor(server, null, ref m_Editor);
            var editor = m_Editor as ServerEditor;

            var serializedObject = m_Editor.serializedObject;
            var expandedProp = serializedObject.FindProperty("m_Expanded");

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                serializedObject.Update();

                expandedProp.boolValue = EditorGUILayout.Foldout(expandedProp.boolValue, server.GetName(), true);

                serializedObject.ApplyModifiedProperties();

                if (editor != null)
                {
                    editor.OnToolbarGUI();
                }
            }

            if (expandedProp.boolValue)
            {
                EditorGUIUtility.hierarchyMode = true;

                using (new EditorGUI.IndentLevelScope())
                {
                    m_Editor.OnInspectorGUI();
                }

                EditorGUIUtility.hierarchyMode = false;
            }
        }

        static void DoCreateServerGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Contents.CreateServerLabel, Contents.LargeButtonOptions))
                {
                    if (s_CreateServerMenuItems == null)
                    {
                        s_CreateServerMenuItems = AttributeUtility.GetAllTypes<CreateServerMenuItemAttribute>();
                    }

                    var menu = MenuUtility.CreateMenu(s_CreateServerMenuItems, (t) => !ServerManager.Instance.HasServer(t), (type, attribute) =>
                    {
                        ServerManager.Instance.CreateServer(type);
                    });

                    menu.ShowAsContext();
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
