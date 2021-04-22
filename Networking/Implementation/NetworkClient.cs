using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.LiveCapture.Networking
{
    /// <summary>
    /// A client that can connect to a <see cref="NetworkServer"/> instance.
    /// </summary>
    public class NetworkClient : NetworkBase
    {
        /// <summary>
        /// Used to keep track of the client state for connection attempts. This is used
        /// as opposed to having these fields in <see cref="NetworkClient"/> so that the
        /// old connection tasks that are being cancelled cannot modify the current state.
        /// </summary>
        class ConnectState
        {
            public IPEndPoint remoteEndPoint;
            public IPEndPoint localEndPoint;
            public NetworkSocket udp;
            public NetworkSocket tcp;
            public CancellationTokenSource cancellationToken;
        }

        ConnectState m_ConnectState;

        /// <summary>
        /// The IP and port the client is using to communicate, or null if the client is not running.
        /// </summary>
        public IPEndPoint localEndPoint => m_IsRunning ? m_ConnectState.localEndPoint : null;

        /// <summary>
        /// The IP and port of the server the client is connecting to, or null if the client is not running.
        /// </summary>
        public IPEndPoint serverEndPoint => m_IsRunning ? m_ConnectState.remoteEndPoint : null;

        /// <summary>
        /// Is this client currently trying to connect to a server.
        /// </summary>
        public bool isConnecting => m_IsRunning && !m_ConnectState.cancellationToken.IsCancellationRequested;

        /// <summary>
        /// How long in milliseconds to wait between connection attempts.
        /// </summary>
        public int connectAttemptTimeout { get; set; } = 2000;


        /// <summary>
        /// Creates a new <see cref="NetworkClient"/> instance.
        /// </summary>
        public NetworkClient()
        {
            remoteDisconnected += OnDisconnect;
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        /// <remarks>
        /// If the client is already running but a new server address is provided, the client will
        /// be restarted and connect to the new end point. The client will try to connect endlessly
        /// until connected or <see cref="Stop"/> is called, waiting <see cref="connectAttemptTimeout"/>
        /// ms between connection attempts.
        /// </remarks>
        /// <param name="serverIP">The remote address of the server.</param>
        /// <param name="serverPort">The TCP/UDP port on the server to connect to.</param>
        /// <param name="localPort">The local TCP/UDP port to connect using. Use 0 to let the
        /// system automatically assign free ports.</param>
        /// <returns>True if the server started successfully.</returns>
        public bool ConnectToServer(string serverIP, int serverPort, int localPort = 0)
        {
            if (!IPAddress.TryParse(serverIP, out var ip))
            {
                Debug.LogError($"Unable to start client: IPv4 address \"{serverIP}\" is not valid.");
                return false;
            }

            if (!NetworkUtilities.IsPortValid(serverPort, out var serverPortMessage))
            {
                Debug.LogError($"Unable to start client: Server port {serverPort} is not valid. {serverPortMessage}");
                return false;
            }

            if (localPort != 0)
            {
                if (!NetworkUtilities.IsPortValid(localPort, out var localPortMessage))
                {
                    Debug.LogError($"Unable to start client: Local port {localPort} is not valid. {localPortMessage}");
                    return false;
                }
                if (!string.IsNullOrEmpty(localPortMessage))
                {
                    Debug.Log($"Client: {localPortMessage}");
                }
            }

            var remoteEndPoint = new IPEndPoint(ip, serverPort);

            if (isRunning)
            {
                if (remoteEndPoint.Equals(serverEndPoint))
                    return true;

                Stop();
            }

            if (!StartInternal(remoteEndPoint, localPort))
                return false;

            m_IsRunning = true;
            RaiseStartedEvent();
            return true;
        }

        /// <inheritdoc/>
        public override void Stop(bool graceful = true)
        {
            m_ConnectState?.cancellationToken?.Cancel();

            base.Stop(graceful);

            if (m_ConnectState != null)
            {
                m_ConnectState.udp?.Dispose();
                m_ConnectState.tcp?.Dispose();
                m_ConnectState = null;
            }
        }

        bool StartInternal(IPEndPoint remoteEndPoint, int localPort)
        {
            // Assign an IP/port so the server can know where to send messages. We should pick the local
            // address that is most similar to the server address, since they should share the subnet
            // portion of the address if they are on the same network. For the port we can let the system
            // decide on a port if the user does not specify one.
            var localEndPoint = new IPEndPoint(NetworkUtilities.FindClosestAddresses(remoteEndPoint).localAddress, localPort);

            // We create a new instance so that if the client is stopped we can safely modify the state
            // without worrying about it being modified by the EndConnect callback.
            m_ConnectState = new ConnectState
            {
                remoteEndPoint = remoteEndPoint,
                localEndPoint = localEndPoint,
            };

            if (!CreateUdpSocket(m_ConnectState))
            {
                Stop();
                return false;
            }

            ConnectAsync(m_ConnectState);

            return true;
        }

        bool CreateUdpSocket(ConnectState state)
        {
            try
            {
                var socket = NetworkUtilities.CreateSocket(ProtocolType.Udp);
                state.udp = new NetworkSocket(this, socket, false);

                socket.Bind(state.localEndPoint);

                // UDP is connectionless, but by calling connect we configure the socket to reject
                // all packets received by the socket not from the server.
                socket.Connect(state.remoteEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"Client: Failed to create UDP socket on {state.localEndPoint}. {e}");
                return false;
            }

            return true;
        }

        bool CreateTcpSocket(ConnectState state)
        {
            try
            {
                var socket = NetworkUtilities.CreateSocket(ProtocolType.Tcp);
                state.tcp = new NetworkSocket(this, socket, false, (remote) =>
                {
                    state.cancellationToken.Cancel();

                    DoHandshake(state.tcp, state.udp);

                    new Connection(this, state.tcp, state.udp, remote);
                });

                socket.Bind(state.localEndPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"Client: Failed to create TCP socket on {state.localEndPoint}. {e}");
                return false;
            }

            return true;
        }

        async void ConnectAsync(ConnectState state)
        {
            state.cancellationToken = new CancellationTokenSource();

            while (!state.cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // sockets that fail to connect need to be disposed and recreated, otherwise subsequent
                    // connection calls to connect will fail
                    state.tcp?.Dispose();

                    if (CreateTcpSocket(state))
                        state.tcp.socket.BeginConnect(state.remoteEndPoint, EndConnect, state);

                    await Task.Delay(connectAttemptTimeout, state.cancellationToken.Token);
                }
                catch (OperationCanceledException)
                {
                    // this is expected when the connection attempts are stopped
                }
                catch (Exception e)
                {
                    Debug.LogError($"Client: Failed to begin connection attempt. {e}");
                }
            }
        }

        void EndConnect(IAsyncResult result)
        {
            var state = result.AsyncState as ConnectState;

            try
            {
                state.tcp.socket.EndConnect(result);
            }
            catch (ObjectDisposedException)
            {
                // This callback is invoked while the connecting socket is closing.
                // The call to EndConnect will throw this exception when that occurs.
            }
            catch (SocketException e)
            {
                switch (e.SocketErrorCode)
                {
                    case SocketError.ConnectionRefused:
                        Debug.LogWarning($"Client: {state.remoteEndPoint} refused the connection.");
                        break;
                    default:
                        Debug.LogError($"Client: Failed to connect. {e}");
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Client: Failed to connect. {e}");
            }
        }

        void OnDisconnect(Remote remote, DisconnectStatus status)
        {
            switch (status)
            {
                case DisconnectStatus.Graceful:
                    break;
                default:
                    // when there is a non-graceful disconnect we should try to reconnect to the
                    // server to avoid needing to stop and start the client.
                    StartInternal(m_ConnectState.remoteEndPoint, m_ConnectState.localEndPoint.Port);
                    break;
            }
        }
    }
}
