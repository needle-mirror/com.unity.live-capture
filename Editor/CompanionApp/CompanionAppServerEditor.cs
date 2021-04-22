using System;
using System.Linq;
using System.Net;
using Unity.LiveCapture.Networking;
using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    [CustomEditor(typeof(CompanionAppServer), true)]
    class CompanionAppServerEditor : ServerEditor
    {
        const double k_PortRefreshPeriod = 2.0;
        const double k_InterfaceRefreshPeriod = 5.0;

        static class Contents
        {
            public static readonly GUILayoutOption[] startButtonOptions =
            {
                GUILayout.Width(60f),
            };
            public static readonly GUIContent startLabel = new GUIContent("Start", "Start the server.");
            public static readonly GUIContent stopLabel = new GUIContent("Stop", "Stop the server.");

            public static readonly GUIContent interfacesLabel = new GUIContent("Available Interfaces", "Available IP addresses on this machine.");
            public static readonly GUIContent clientDevicesLabel = new GUIContent("Connected Clients", "The client devices currently connected to the server.");
            public static readonly GUIContent clientNameTitle = new GUIContent("Name", "The name of the client device.");
            public static readonly GUIContent clientTypeTitle = new GUIContent("Type", "The type of the client device.");
            public static readonly GUIContent noClientsLabel = new GUIContent("No clients connected");
        }

        SerializedProperty m_AutoStartOnPlay;
        SerializedProperty m_Port;
        SerializedProperty m_InterfacesExpanded;
        SerializedProperty m_ClientsExpanded;

        CompanionAppServer m_Server;
        bool m_PortValid;
        string m_PortMessage;
        bool m_PortAvailable;
        double m_LastPortRefreshTime;
        IPAddress[] m_Addresses;
        double m_LastInterfaceRefreshTime;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_AutoStartOnPlay = serializedObject.FindProperty("m_AutoStartOnPlay");
            m_Port = serializedObject.FindProperty("m_Port");
            m_InterfacesExpanded = serializedObject.FindProperty("m_InterfacesExpanded");
            m_ClientsExpanded = serializedObject.FindProperty("m_ClientsExpanded");

            m_Server = target as CompanionAppServer;
            m_LastPortRefreshTime = double.MinValue;
            m_LastInterfaceRefreshTime = double.MinValue;
        }

        /// <inheritdoc />
        public override void OnToolbarGUI()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var label = m_Server.isRunning ? Contents.stopLabel : Contents.startLabel;
                var run = GUILayout.Toggle(m_Server.isRunning, label, EditorStyles.toolbarButton, Contents.startButtonOptions);

                if (change.changed)
                {
                    if (run)
                    {
                        m_Server.StartServer();
                    }
                    else
                    {
                        m_Server.StopServer();
                    }
                }
            }

            base.OnToolbarGUI();
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_AutoStartOnPlay);
            DrawPort();

            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            DrawInterfaces();
            DrawClients();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPort()
        {
            var refreshPort = EditorApplication.timeSinceStartup - m_LastPortRefreshTime > k_PortRefreshPeriod;

            using (new EditorGUI.DisabledGroupScope(m_Server.isRunning))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_Port);
                refreshPort |= change.changed;
            }

            // check if the selected port is valid
            if (refreshPort && !m_Server.isRunning)
            {
                var port = m_Port.intValue;

                m_PortValid = NetworkUtilities.IsPortValid(port, out m_PortMessage);

                if (m_PortValid)
                {
                    m_PortAvailable = NetworkUtilities.IsPortAvailable(port);
                }

                m_LastPortRefreshTime = EditorApplication.timeSinceStartup;
            }

            // display messages explaining why the port is not valid
            if (!m_Server.isRunning)
            {
                if (!m_PortValid)
                {
                    EditorGUILayout.HelpBox(m_PortMessage, MessageType.Error);
                }
                else
                {
                    if (!string.IsNullOrEmpty(m_PortMessage))
                    {
                        EditorGUILayout.HelpBox(m_PortMessage, MessageType.Warning);
                    }
                    if (!m_PortAvailable)
                    {
                        EditorGUILayout.HelpBox($"Port {m_Port.intValue} is in use by another program or Unity instance! Close the other program or assign a free port.", MessageType.Warning);
                    }
                }
            }
        }

        void DrawInterfaces()
        {
            if (!DoFoldout(m_InterfacesExpanded, Contents.interfacesLabel))
                return;

            if (EditorApplication.timeSinceStartup - m_LastInterfaceRefreshTime > k_InterfaceRefreshPeriod)
            {
                m_Addresses = NetworkUtilities.GetIPAddresses(false);
                m_LastInterfaceRefreshTime = EditorApplication.timeSinceStartup;
            }

            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var address in m_Addresses)
                {
                    EditorGUILayout.LabelField(address.ToString());
                }
            }
        }

        void DrawClients()
        {
            if (!DoFoldout(m_ClientsExpanded, Contents.clientDevicesLabel))
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (m_Server.clientCount > 0)
                {
                    var clients = m_Server.GetClients().ToArray();

                    for (var i = 0; i < m_Server.clientCount + 1; i++)
                    {
                        var rect = EditorGUILayout.GetControlRect();
                        rect = EditorGUI.IndentedRect(rect);

                        var leftRect = new Rect(rect)
                        {
                            xMax = (rect.xMin + rect.xMax) * 0.5f,
                        };
                        var rightRect = new Rect(rect)
                        {
                            xMin = leftRect.xMax,
                        };

                        var indent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;

                        if (i == 0)
                        {
                            EditorGUI.LabelField(leftRect, Contents.clientNameTitle, EditorStyles.boldLabel);
                            EditorGUI.LabelField(rightRect, Contents.clientTypeTitle, EditorStyles.boldLabel);
                        }
                        else
                        {
                            var client = clients[i - 1];
                            EditorGUI.LabelField(leftRect, client.name);
                            EditorGUI.LabelField(rightRect, client.ToString());
                        }

                        EditorGUI.indentLevel = indent;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(Contents.noClientsLabel);
                }
            }
        }

        bool DoFoldout(SerializedProperty boolProp, GUIContent label)
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var isExpanded = EditorGUILayout.Foldout(boolProp.boolValue, label, true);

                if (change.changed)
                    boolProp.boolValue = isExpanded;
            }

            return boolProp.boolValue;
        }
    }
}
