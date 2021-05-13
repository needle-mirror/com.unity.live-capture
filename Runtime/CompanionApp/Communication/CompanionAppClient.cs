using System;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// An enum defining the modes a <see cref="CompanionAppDevice{TClient}"/> operates in.
    /// </summary>
    enum DeviceMode
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
        string Name { get; }
    }

    /// <summary>
    /// The interface used to communicate with the companion app.
    /// </summary>
    interface ICompanionAppClientInternal : ICompanionAppClient
    {
        /// <summary>
        /// The unique identifier for this client instance.
        /// </summary>
        Guid ID { get; }

        /// <summary>
        /// The resolution of the device screen in pixels, or <see cref="Vector2Int.zero"/> is no screen is available.
        /// </summary>
        Vector2Int ScreenResolution { get; }

        /// <summary>
        /// An event invoked when the client wants to set the device mode.
        /// </summary>
        /// <remarks>
        /// The event provides the desired mode.
        /// </remarks>
        event Action<DeviceMode> SetDeviceMode;

        /// <summary>
        /// An event invoked when the client wants to initiate recording.
        /// </summary>
        event Action StartRecording;

        /// <summary>
        /// An event invoked when the client wants to end recording.
        /// </summary>
        event Action StopRecording;

        /// <summary>
        /// An event invoked when the client wants to start playback on the current <see cref="ISlate"/>.
        /// </summary>
        event Action StartPlayer;

        /// <summary>
        /// An event invoked when the client wants to stop playback on the current <see cref="ISlate"/>.
        /// </summary>
        event Action StopPlayer;

        /// <summary>
        /// An event invoked when the client wants to pause playback on the current <see cref="ISlate"/>.
        /// </summary>
        event Action PausePlayer;

        /// <summary>
        /// An event invoked when the client wants to set the playback time in the current <see cref="ISlate"/>.
        /// </summary>
        /// <remarks>
        /// The event provides the time in seconds from the start of the Shot.
        /// </remarks>
        event Action<double> SetPlayerTime;

        /// <summary>
        /// An event invoked when the client requests the take to select.
        /// </summary>
        event Action<SerializableGuid> SetSelectedTake;

        /// <summary>
        /// An event invoked when the client requests to rate a take.
        /// </summary>
        event Action<TakeDescriptor> SetTakeData;

        /// <summary>
        /// An event invoked when the client requests to delete a take.
        /// </summary>
        event Action<SerializableGuid> DeleteTake;

        /// <summary>
        /// An event invoked when the client requests to set a take as the iteration base.
        /// </summary>
        event Action<SerializableGuid> SetIterationBase;

        /// <summary>
        /// An event invoked when the client requests to operate without an iteration base.
        /// </summary>
        event Action ClearIterationBase;

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
    abstract class CompanionAppClient : ICompanionAppClientInternal
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
        public string Name { get; }

        /// <inheritdoc/>
        public Guid ID { get; }

        /// <inheritdoc />
        public Vector2Int ScreenResolution { get; }

        /// <inheritdoc />
        public event Action<DeviceMode> SetDeviceMode;

        /// <inheritdoc />
        public event Action StartRecording;

        /// <inheritdoc />
        public event Action StopRecording;

        /// <inheritdoc />
        public event Action StartPlayer;

        /// <inheritdoc />
        public event Action StopPlayer;

        /// <inheritdoc />
        public event Action PausePlayer;

        /// <inheritdoc />
        public event Action<double> SetPlayerTime;

        /// <inheritdoc />
        public event Action<SerializableGuid> SetSelectedTake;

        /// <inheritdoc />
        public event Action<TakeDescriptor> SetTakeData;

        /// <inheritdoc />
        public event Action<SerializableGuid> DeleteTake;

        /// <inheritdoc />
        public event Action<SerializableGuid> SetIterationBase;

        /// <inheritdoc />
        public event Action ClearIterationBase;

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

            Name = data.Name;
            ID = Guid.Parse(data.ID);
            ScreenResolution = data.ScreenResolution;

            m_Protocol = new Protocol($"{GetType().Name}Protocol");
            m_Protocol.SetNetwork(network, remote);

            m_InitializeSender = m_Protocol.Add(new EventSender(CompanionAppMessages.ToClient.Initialize));
            m_ServerStateSender = m_Protocol.Add(new BinarySender<ServerState>(CompanionAppMessages.ToClient.ServerState));
            m_PlayerStateSender = m_Protocol.Add(new BinarySender<PlayerState>(CompanionAppMessages.ToClient.PlayerState));
            m_SlateDescriptorSender = m_Protocol.Add(new JsonSender<SlateDescriptor>(CompanionAppMessages.ToClient.SlateDescriptor));

            m_Protocol.Add(new BinaryReceiver<ServerMode>(CompanionAppMessages.ToServer.SetMode)).AddHandler((serverMode) =>
            {
                var mode = serverMode == ServerMode.LiveStream ? DeviceMode.LiveStream : DeviceMode.Playback;
                SetDeviceMode?.Invoke(mode);
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.StartRecording)).AddHandler(() =>
            {
                StartRecording?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.StopRecording)).AddHandler(() =>
            {
                StopRecording?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.PlayerStart)).AddHandler(() =>
            {
                StartPlayer?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.PlayerStop)).AddHandler(() =>
            {
                StopPlayer?.Invoke();
            });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.PlayerPause)).AddHandler(() =>
            {
                PausePlayer?.Invoke();
            });
            m_Protocol.Add(new BinaryReceiver<double>(CompanionAppMessages.ToServer.PlayerSetTime)).AddHandler((time) =>
            {
                SetPlayerTime?.Invoke(time);
            });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.SetSelectedTake,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((guid) =>
                {
                    SetSelectedTake?.Invoke(guid);
                });
            m_Protocol.Add(new JsonReceiver<TakeDescriptor>(CompanionAppMessages.ToServer.SetTakeData)).AddHandler((descriptor) =>
            {
                SetTakeData?.Invoke(descriptor);
            });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.DeleteTake,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((guid) =>
                {
                    DeleteTake?.Invoke(guid);
                });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.SetIterationBase,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((guid) =>
                {
                    SetIterationBase?.Invoke(guid);
                });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.ClearIterationBase)).AddHandler(() =>
            {
                ClearIterationBase?.Invoke();
            });
        }

        /// <summary>
        /// Transmits the communication protocol to the client.
        /// </summary>
        internal void SendProtocol()
        {
            var message = Message.Get(m_Remote, ChannelType.ReliableOrdered, 8192);

            m_Protocol.CreateInverse().Serialize(message.Data);

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
                Recording = m_IsRecording,
                Mode = m_DeviceMode == DeviceMode.LiveStream ? ServerMode.LiveStream : ServerMode.Playback,
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
