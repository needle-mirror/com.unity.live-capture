using System;
using System.IO;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// A class used to communicate with with the Unity editor from the companion apps.
    /// </summary>
    public abstract class CompanionAppHost
    {
        /// <summary>
        /// The protocol used when communicating with the host.
        /// </summary>
        protected readonly Protocol m_Protocol;

        readonly DataSender<ServerMode> m_ServerModeSender;
        readonly EventSender m_StartRecordingSender;
        readonly EventSender m_StopRecordingSender;
        readonly EventSender m_StartPlayerSender;
        readonly EventSender m_StopPlayerSender;
        readonly EventSender m_PausePlayerSender;
        readonly DataSender<double> m_PlayerTimeSender;
        readonly JsonSender<SerializableGuid> m_SetSelectedTakeSender;

        /// <summary>
        /// An event invoked when the server state has been modified.
        /// </summary>
        public event Action<ServerState> serverStateReceived;

        /// <summary>
        /// An event invoked when the player state has been modified.
        /// </summary>
        public event Action<PlayerState> playerStateReceived;

        /// <summary>
        /// An event invoked when the slate descriptor has been modified.
        /// </summary>
        public event Action<SlateDescriptor> slateDescriptorReceived;

        /// <summary>
        /// Creates a new <see cref="CompanionAppHost"/> instance.
        /// </summary>
        /// <param name="network">The <see cref="NetworkBase"/> used to send and receive messages.</param>
        /// <param name="remote">The handle of the server on the network.</param>
        /// <param name="stream">A stream containing the protocol received from the server.</param>
        protected CompanionAppHost(NetworkBase network, Remote remote, Stream stream)
        {
            m_Protocol = new Protocol(stream);
            m_Protocol.SetNetwork(network, remote);

            m_ServerModeSender = BinarySender<ServerMode>.Get(m_Protocol, CompanionAppMessages.ToServer.k_SetMode);
            m_StartRecordingSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.k_StartRecording);
            m_StopRecordingSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.k_StopRecording);
            m_StartPlayerSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.k_PlayerStart);
            m_StopPlayerSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.k_PlayerStop);
            m_PausePlayerSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.k_PlayerPause);
            m_PlayerTimeSender = BinarySender<double>.Get(m_Protocol, CompanionAppMessages.ToServer.k_PlayerSetTime);
            m_SetSelectedTakeSender = JsonSender<SerializableGuid>.Get(m_Protocol, CompanionAppMessages.ToServer.k_SetSelectedTake);

            EventReceiver.Get(m_Protocol, CompanionAppMessages.ToClient.k_Initialize).AddHandler(() =>
            {
                Initialize();
            });
            BinaryReceiver<ServerState>.Get(m_Protocol, CompanionAppMessages.ToClient.k_ServerState).AddHandler((state) =>
            {
                serverStateReceived?.Invoke(state);
            });
            BinaryReceiver<PlayerState>.Get(m_Protocol, CompanionAppMessages.ToClient.k_PlayerState).AddHandler((state) =>
            {
                playerStateReceived?.Invoke(state);
            });
            JsonReceiver<SlateDescriptor>.Get(m_Protocol, CompanionAppMessages.ToClient.k_SlateDescriptor).AddHandler((descriptor) =>
            {
                slateDescriptorReceived?.Invoke(descriptor);
            });
        }

        /// <summary>
        /// Resets the protocol state.
        /// </summary>
        protected virtual void Initialize()
        {
            m_Protocol.Reset();
        }

        /// <summary>
        /// Sets the server mode.
        /// </summary>
        /// <param name="mode">The server mode to set.</param>
        public void SetServerMode(ServerMode mode)
        {
            m_ServerModeSender.Send(mode);
        }

        /// <summary>
        /// Starts recording a take.
        /// </summary>
        public void StartRecording()
        {
            m_StartRecordingSender.Send();
        }

        /// <summary>
        /// Stops recording the current take.
        /// </summary>
        public void StopRecording()
        {
            m_StopRecordingSender.Send();
        }

        /// <summary>
        /// Starts playback of the current take.
        /// </summary>
        public void StartPlayer()
        {
            m_StartPlayerSender.Send();
        }

        /// <summary>
        /// Stops playback of the current take.
        /// </summary>
        public void StopPlayer()
        {
            m_StopPlayerSender.Send();
        }

        /// <summary>
        /// Pauses playback of the current take.
        /// </summary>
        public void PausePlayer()
        {
            m_PausePlayerSender.Send();
        }

        /// <summary>
        /// Sets the playback time in the current take.
        /// </summary>
        /// <param name="time">The time to set in seconds since the start of the take.</param>
        public void SetPlayerTime(double time)
        {
            m_PlayerTimeSender.Send(time);
        }

        /// <summary>
        /// Requests to select a take using its guid.
        /// </summary>
        /// <param name="guid">The guid of the take to select.</param>
        public void SetSelectedTake(SerializableGuid guid)
        {
            m_SetSelectedTakeSender.Send(guid);
        }
    }
}
