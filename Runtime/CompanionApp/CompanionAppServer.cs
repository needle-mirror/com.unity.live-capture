using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Discovery;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// The server used to communicate with the companion apps.
    /// </summary>
    [CreateServerMenuItem("Companion App Server")]
    public class CompanionAppServer : Server
    {
        const int k_DefaultPort = 9000;

        /// <summary>
        /// The server executes this event when a client has connected.
        /// </summary>
        public static event Action<ICompanionAppClient> clientConnected = delegate {};

        /// <summary>
        /// The server executes this event when a client has disconnected.
        /// </summary>
        public static event Action<ICompanionAppClient> clientDisconnected = delegate {};

        struct ConnectHandler
        {
            public string name;
            public DateTime time;
            public Func<ICompanionAppClient, bool> handler;
        }

        static readonly Dictionary<string, Type> s_TypeToClientType = new Dictionary<string, Type>();
        static readonly List<ConnectHandler> s_ClientConnectHandlers = new List<ConnectHandler>();

        /// <summary>
        /// Adds a callback used to take ownership of a client that has connected.
        /// </summary>
        /// <param name="handler">The callback function. It must return true if it takes ownership of a client.</param>
        /// <param name="name">The name of the client to prefer. If set, this handler has priority over clients that have the given name.</param>
        /// <param name="time">The time used to determine the priority of handlers when many are listening for the same
        /// client <paramref name="name"/>. More recent values have higher priority.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is null.</exception>
        public static void RegisterClientConnectHandler(Func<ICompanionAppClient, bool> handler, string name, DateTime time)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            DeregisterClientConnectHandler(handler);

            s_ClientConnectHandlers.Add(new ConnectHandler
            {
                name = name,
                time = time,
                handler = handler,
            });
        }

        /// <summary>
        /// Removes a client connection callback.
        /// </summary>
        /// <param name="handler">The callback to remove.</param>>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is null.</exception>
        public static void DeregisterClientConnectHandler(Func<ICompanionAppClient, bool> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            for (var i = 0; i < s_ClientConnectHandlers.Count; i++)
            {
                if (s_ClientConnectHandlers[i].handler == handler)
                {
                    s_ClientConnectHandlers.RemoveAt(i);
                }
            }
        }

        static CompanionAppServer()
        {
            foreach (var(type, attributes) in AttributeUtility.GetAllTypes<ClientAttribute>())
            {
                if (!typeof(CompanionAppClient).IsAssignableFrom(type))
                {
                    Debug.LogError($"{type.FullName} must be assignable from {nameof(CompanionAppClient)} to use the {nameof(ClientAttribute)} attribute.");
                    continue;
                }

                foreach (var attribute in attributes)
                {
                    s_TypeToClientType[attribute.type] = type;
                }
            }
        }

#if UNITY_EDITOR
#pragma warning disable 414
        [SerializeField, HideInInspector]
        bool m_InterfacesExpanded = false;
        [SerializeField, HideInInspector]
        bool m_ClientsExpanded = true;
#pragma warning restore 414
#endif

        [SerializeField, Tooltip("The TCP port on which the server will listen for incoming connections. Changes to the port only take effect after restarting the server.")]
        int m_Port = k_DefaultPort;
        [SerializeField, Tooltip("Start the server automatically after entering play mode.")]
        bool m_AutoStartOnPlay = true;

        readonly DiscoveryServer m_Discovery = new DiscoveryServer();
        readonly NetworkServer m_Server = new NetworkServer();
        readonly Dictionary<Remote, ICompanionAppClient> m_RemoteToClient = new Dictionary<Remote, ICompanionAppClient>();

        /// <summary>
        /// The TCP port on which the server will listen for incoming connections.
        /// </summary>
        /// <remarks>
        /// Changes to the port only take effect after restarting the server.
        /// </remarks>
        public int port
        {
            get => m_Port;
            set
            {
                if (m_Port != value)
                {
                    m_Port = value;
                    OnServerChanged(true);
                }
            }
        }

        /// <summary>
        /// Start the server automatically after entering play mode.
        /// </summary>
        public bool autoStartOnPlay
        {
            get => m_AutoStartOnPlay;
            set
            {
                if (m_AutoStartOnPlay != value)
                {
                    m_AutoStartOnPlay = value;
                    OnServerChanged(true);
                }
            }
        }

        /// <summary>
        /// Are clients able to connect to the server.
        /// </summary>
        public bool isRunning => m_Server.isRunning;

        /// <summary>
        /// The number of clients currently connected to the server.
        /// </summary>
        public int clientCount => m_RemoteToClient.Count;

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            m_Server.remoteConnected += OnClientConnected;
            m_Server.remoteDisconnected += OnClientDisconnected;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
#endif
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();

            m_Server.remoteConnected -= OnClientConnected;
            m_Server.remoteDisconnected -= OnClientDisconnected;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        void PlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    if (m_AutoStartOnPlay)
                    {
                        StartServer();
                    }
                    break;
                }
            }
        }

#endif

        /// <summary>
        /// Gets the currently connected clients.
        /// </summary>
        /// <returns>A new collection containing the client handles.</returns>
        public IEnumerable<ICompanionAppClient> GetClients()
        {
            return m_RemoteToClient.Values;
        }

        /// <inheritdoc />
        public override string GetName() => "Companion App Server";

        /// <summary>
        /// Start listening for clients connections.
        /// </summary>
        public void StartServer()
        {
            if (!NetworkUtilities.IsPortAvailable(m_Port))
            {
                Debug.LogError($"Unable to start server: Port {m_Port} is in use by another program! Close the other program, or assign a free port using the Live Capture Window.");
                return;
            }

            if (m_Server.StartServer(m_Port))
            {
                // start server discovery
                var config = new ServerData(
                    "Live Capture",
                    Environment.MachineName,
                    m_Server.id,
                    m_Server.protocolVersion
                );
                var endPoints = m_Server.endPoints.ToArray();

                m_Discovery.Start(config, endPoints);
            }

            OnServerChanged(false);
        }

        /// <summary>
        /// Disconnects all clients and stop listening for new connections.
        /// </summary>
        public void StopServer()
        {
            m_Server.Stop();
            m_Discovery.Stop();

            OnServerChanged(false);
        }

        /// <inheritdoc/>
        public override void OnUpdate()
        {
            m_Server.Update();
            m_Discovery.Update();
        }

        void OnClientConnected(Remote remote)
        {
            m_Server.RegisterMessageHandler(remote, InitializeClient, false);
        }

        void OnClientDisconnected(Remote remote, DisconnectStatus status)
        {
            if (m_RemoteToClient.TryGetValue(remote, out var client))
            {
                try
                {
                    clientDisconnected.Invoke(client);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                m_RemoteToClient.Remove(remote);
                OnServerChanged(false);
            }
        }

        void InitializeClient(Message message)
        {
            var remote = message.remote;

            m_Server.DeregisterMessageHandler(remote);

            try
            {
                var streamReader = new StreamReader(message.data, Encoding.UTF8);
                var data = JsonUtility.FromJson<ClientInitialization>(streamReader.ReadToEnd());

                if (!s_TypeToClientType.TryGetValue(data.type, out var clientType))
                {
                    Debug.LogError($"Unknown client type \"{data.type}\" connected to {nameof(CompanionAppServer)}!");
                    return;
                }

                var client = Activator.CreateInstance(clientType, m_Server, remote, data) as CompanionAppClient;
                client.SendProtocol();

                m_RemoteToClient.Add(remote, client);

                AssignOwner(client);

                clientConnected.Invoke(client);
                OnServerChanged(false);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                message.Dispose();
            }
        }

        static void AssignOwner(ICompanionAppClient client)
        {
            // connect to the registered handler that was most recently used with this client if possible
            foreach (var handler in s_ClientConnectHandlers.OrderByDescending(h => h.time.Ticks))
            {
                try
                {
                    if (handler.name == client.name)
                    {
                        if (handler.handler(client))
                            return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            // fall back to the first free device that is compatible with the client
            foreach (var handler in s_ClientConnectHandlers)
            {
                try
                {
                    if (handler.handler(client))
                        return;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
