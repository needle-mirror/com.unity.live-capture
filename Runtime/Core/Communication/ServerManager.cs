using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

namespace Unity.LiveCapture
{
    /// <summary>
    /// The class that manages <see cref="Server"/> instances.
    /// </summary>
    public class ServerManager : ScriptableObject
    {
        const string k_AssetPath = "UserSettings/LiveCapture/ServerManager.asset";

        /// <summary>
        /// An event invoked when a Server is added to the manager.
        /// </summary>
        public static event Action<Server> ServerCreated;

        /// <summary>
        /// An event invoked when a Server is removed from the manager.
        /// </summary>
        public static event Action<Server> ServerDestroyed;

        /// <summary>
        /// An event invoked when any Server has been modified.
        /// </summary>
        internal static event Action ServerChanged;

        static ServerManager s_Instance;
        static bool s_Loading;

        /// <summary>
        /// The ServerManager instance.
        /// </summary>
        public static ServerManager Instance
        {
            get
            {
                if (s_Instance == null)
                    CreateAndLoad();
                return s_Instance;
            }
        }

        [SerializeField, HideInInspector]
        List<Server> m_Servers = new List<Server>();

        /// <summary>
        /// The servers that are currently active.
        /// </summary>
        public IReadOnlyList<Server> Servers => m_Servers;

        ServerManager()
        {
            if (s_Instance != null)
            {
                Debug.LogError($"{nameof(ServerManager)} already exists. Did you query the singleton in a constructor?");
            }
            else
            {
                s_Instance = this;
            }
        }

        /// <summary>
        /// Gets the server of a specified type, if it exists.
        /// </summary>
        /// <param name="server">The returned server, or default if there is no matching server instance.</param>
        /// <typeparam name="TServer">The type of the server to get.</typeparam>
        /// <returns>True if a matching server instance was found; false otherwise.</returns>
        public bool TryGetServer<TServer>(out TServer server) where TServer : Server
        {
            foreach (var s in m_Servers)
            {
                if (s is TServer serv)
                {
                    server = serv;
                    return true;
                }
            }

            server = default;
            return false;
        }

        /// <summary>
        /// Checks if a server of the given type exists.
        /// </summary>
        /// <param name="type">The type of server to check.</param>
        /// <returns>True if a server of the type exists; false otherwise.</returns>
        public bool HasServer(Type type)
        {
            foreach (var s in m_Servers)
            {
                if (type.IsInstanceOfType(s))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a server of a specified type.
        /// </summary>
        /// <remarks>
        /// Only one instance of each server may be created at a time.
        /// If a server of type <paramref name="type"/> already exists, it
        /// is returned instead of a new instance.
        /// </remarks>
        /// <param name="type">The type of server to create.</param>
        /// <returns>The server instance.</returns>
        public Server CreateServer(Type type)
        {
            var server = m_Servers.FirstOrDefault(s => type.IsInstanceOfType(s));

            if (server != null)
                return server;

            server = CreateInstance(type) as Server;
            server.hideFlags = HideFlags.DontSave;
            m_Servers.Add(server);

            Save();

            ServerCreated?.Invoke(server);
            OnServerChanged();

            return server;
        }

        /// <summary>
        /// Destroys a server.
        /// </summary>
        /// <param name="server">The server to destroy.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="server"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="server"/> is not owned
        /// by this instance.</exception>
        public void DestroyServer(Server server)
        {
            if (server == null)
                throw new ArgumentNullException(nameof(server));
            if (!m_Servers.Remove(server))
                throw new ArgumentException(nameof(server));

            Save();

            ServerDestroyed?.Invoke(server);
            OnServerChanged();

            if (Application.isPlaying)
            {
                Destroy(server);
            }
            else
            {
                DestroyImmediate(server);
            }
        }

        static void CreateAndLoad()
        {
            if (s_Loading)
                return;

            s_Loading = true;

#if UNITY_EDITOR
            InternalEditorUtility.LoadSerializedFileAndForget(k_AssetPath);
#endif

            if (s_Instance != null)
            {
                // remove any servers that are not valid when loaded
                s_Instance.m_Servers.RemoveAll(s => s == null);
            }
            else
            {
                CreateInstance<ServerManager>().hideFlags = HideFlags.HideAndDontSave;
            }

            s_Loading = false;
        }

        void OnEnable()
        {
            foreach (var server in s_Instance.m_Servers)
            {
                ServerCreated?.Invoke(server);
            }

            OnServerChanged();
        }

        /// <summary>
        /// Serializes the servers to disk.
        /// </summary>
        /// <remarks>
        /// The server data file is stored in the UserSettings folder of the project.
        /// </remarks>
        internal void Save()
        {
            if (s_Loading)
                return;

#if UNITY_EDITOR
            var directoryName = Path.GetDirectoryName(k_AssetPath);

            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            // we serialize the manager and all the servers to a single file
            var objectsToSerialize = new ScriptableObject[] { s_Instance }
                .Union(m_Servers)
                .ToArray();

            InternalEditorUtility.SaveToSerializedFileAndForget(objectsToSerialize, k_AssetPath, true);
#endif
        }

        /// <summary>
        /// Called to invoke the <see cref="ServerChanged"/> event.
        /// </summary>
        internal void OnServerChanged()
        {
            ServerChanged?.Invoke();
        }
    }
}
