using System;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// An enum defining the modes a <see cref="CompanionAppDevice{TClient}"/> operates in.
    /// </summary>
    public enum DeviceMode
    {
        /// <summary>
        /// The device is configured to play recorded Takes.
        /// </summary>
        Playback,
        /// <summary>
        /// The device is configured to receive live data.
        /// </summary>
        LiveStream,
    }

    /// <summary>
    /// The interface used to communicate with the companion app.
    /// </summary>
    public interface ICompanionAppClient
    {
        /// <summary>
        /// The name of the client device.
        /// </summary>
        string name { get; }

        /// <summary>
        /// The unique identifier for this client instance.
        /// </summary>
        Guid id { get; }

        /// <summary>
        /// The resolution of the device screen in pixels, or <see cref="Vector2Int.zero"/> is no screen is available.
        /// </summary>
        Vector2Int screenResolution { get; }

        /// <summary>
        /// An event invoked when the client wants to set the device mode.
        /// </summary>
        /// <remarks>
        /// The event provides the desired mode.
        /// </remarks>
        event Action<DeviceMode> setDeviceMode;

        /// <summary>
        /// An event invoked when the client wants to initiate recording.
        /// </summary>
        event Action startRecording;

        /// <summary>
        /// An event invoked when the client wants to end recording.
        /// </summary>
        event Action stopRecording;

        /// <summary>
        /// An event invoked when the client wants to start playback on the current <see cref="ISlate"/>.
        /// </summary>
        event Action startPlayer;

        /// <summary>
        /// An event invoked when the client wants to stop playback on the current <see cref="ISlate"/>.
        /// </summary>
        event Action stopPlayer;

        /// <summary>
        /// An event invoked when the client wants to pause playback on the current <see cref="ISlate"/>.
        /// </summary>
        event Action pausePlayer;

        /// <summary>
        /// An event invoked when the client wants to set the playback time in the current <see cref="ISlate"/>.
        /// </summary>
        /// <remarks>
        /// The event provides the time in seconds from the start of the Shot.
        /// </remarks>
        event Action<double> setPlayerTime;

        /// <summary>
        /// An event invoked when the client requests the take to select.
        /// </summary>
        event Action<SerializableGuid> setSelectedTake;

        /// <summary>
        /// Resets the client communication state.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Sends the device mode to the client.
        /// </summary>
        /// <param name="deviceMode">The state to send.</param>
        void SendDeviceMode(DeviceMode deviceMode);

        /// <summary>
        /// Sends the recording state to the client.
        /// </summary>
        /// <param name="isRecording">The state to send.</param>
        void SendRecordingState(bool isRecording);

        /// <summary>
        /// Sends the player state to the client.
        /// </summary>
        /// <param name="state">The state to send.</param>
        void SendPlayerState(PlayerState state);

        /// <summary>
        /// Sends the available take list to the client.
        /// </summary>
        /// <param name="descriptor">The slate information to send.</param>
        void SendSlateDescriptor(SlateDescriptor descriptor);
    }

    /// <inheritdoc cref="ICompanionAppClient"/>
    public abstract class CompanionAppClient : ICompanionAppClient
    {
        readonly NetworkBase m_Network;
        readonly Remote m_Remote;

        /// <summary>
        /// The protocol used when communicating with the client.
        /// </summary>
        protected readonly Protocol m_Protocol;

        readonly EventSender m_InitializeSender;
        readonly DataSender<ServerState> m_ServerStateSender;
        readonly DataSender<PlayerState> m_PlayerStateSender;
        readonly JsonSender<SlateDescriptor> m_SlateDescriptorSender;

        bool m_IsRecording;
        DeviceMode m_DeviceMode;

        /// <inheritdoc/>
        public string name { get; }

        /// <inheritdoc/>
        public Guid id { get; }

        /// <inheritdoc />
        public Vector2Int screenResolution { get; }

        /// <inheritdoc />
        public event Action<DeviceMode> setDeviceMode;

        /// <inheritdoc />
        public event Action startRecording;

        /// <inheritdoc />
        public event Action stopRecording;

        /// <inheritdoc />
        public event Action startPlayer;

        /// <inheritdoc />
        public event Action stopPlayer;

        /// <inheritdoc />
        public event Action pausePlayer;

        /// <inheritdoc />
        public event Action<double> setPlayerTime;

        /// <inheritdoc />
        public event Action<SerializableGuid> setSelectedTake;

        /// <summary>
        /// Creates a new <see cref="CompanionAppClient"/> instance.
        /// </summary>
        /// <param name="network">The <see cref="NetworkBase"/> used to send and receive messages.</param>
        /// <param name="remote">The handle of the client on the network.</param>
        /// <param name="data">The initialization data received from the client.</param>
        protected CompanionAppClient(NetworkBase network, Remote remote, ClientInitialization data)
        {
            m_Network = network;
            m_Remote = remote;

            name = data.name;
            id = Guid.Parse(data.id);
            screenResolution = data.screenResolution;

            m_Protocol = new Protocol($"{GetType().Name}Protocol");
            m_Protocol.SetNetwork(network, remote);

            m_InitializeSender = m_Protocol.Add(new EventSender(CompanionAppMessages.ToClient.k_Initialize));
            m_ServerStateSender = m_Protocol.Add(new BinarySender<ServerState>(CompanionAppMessages.ToClient.k_ServerState));
            m_PlayerStateSender = m_Protocol.Add(new BinarySender<PlayerState>(CompanionAppMessages.ToClient.k_PlayerState));
            m_SlateDescriptorSender = m_Protocol.Add(new JsonSender<SlateDescriptor>(CompanionAppMessages.ToClient.k_SlateDescriptor));

            m_Protocol.Add(new BinaryReceiver<ServerMode>(CompanionAppMessages.ToServer.k_SetMode)).AddHandler((serverMode) =>
            {
                var mode = serverMode == ServerMode.LiveStream ? DeviceMode.LiveStream : DeviceMode.Playback;
                setDeviceMode?.Invoke(mode);
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.k_StartRecording)).AddHandler(() =>
            {
                startRecording?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.k_StopRecording)).AddHandler(() =>
            {
                stopRecording?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.k_PlayerStart)).AddHandler(() =>
            {
                startPlayer?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.k_PlayerStop)).AddHandler(() =>
            {
                stopPlayer?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.k_PlayerPause)).AddHandler(() =>
            {
                pausePlayer?.Invoke();
            });
            m_Protocol.Add(new BinaryReceiver<double>(CompanionAppMessages.ToServer.k_PlayerSetTime)).AddHandler((time) =>
            {
                setPlayerTime?.Invoke(time);
            });
            m_Protocol.Add(new JsonReceiver<SerializableGuid>(CompanionAppMessages.ToServer.k_SetSelectedTake)).AddHandler((index) =>
            {
                setSelectedTake?.Invoke(index);
            });
        }

        /// <summary>
        /// Transmits the communication protocol to the client.
        /// </summary>
        internal void SendProtocol()
        {
            var message = Message.Get(m_Remote, ChannelType.ReliableOrdered, 8192);

            m_Protocol.CreateInverse().Serialize(message.data);

            m_Network.SendMessage(message);
        }

        /// <inheritdoc />
        public virtual void Initialize()
        {
            m_Protocol.Reset();

            m_InitializeSender.Send();
        }

        /// <inheritdoc />
        public void SendDeviceMode(DeviceMode deviceMode)
        {
            m_DeviceMode = deviceMode;
            SendServerState();
        }

        /// <inheritdoc />
        public void SendRecordingState(bool isRecording)
        {
            m_IsRecording = isRecording;
            SendServerState();
        }

        void SendServerState()
        {
            m_ServerStateSender.Send(new ServerState
            {
                recording = m_IsRecording,
                mode = m_DeviceMode == DeviceMode.LiveStream ? ServerMode.LiveStream : ServerMode.Playback,
            });
        }

        /// <inheritdoc />
        public void SendPlayerState(PlayerState state)
        {
            m_PlayerStateSender.Send(state);
        }

        /// <inheritdoc />
        public void SendSlateDescriptor(SlateDescriptor descriptor)
        {
            m_SlateDescriptorSender.Send(descriptor);
        }
    }
}
