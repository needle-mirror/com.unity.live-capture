using System;
using System.IO;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// A class used to communicate with with the Unity editor from the companion apps.
    /// </summary>
    abstract class CompanionAppHost
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
        readonly BinarySender<SerializableGuid> m_SetSelectedTakeSender;
        readonly JsonSender<TakeDescriptor> m_SetTakeDataSender;
        readonly BinarySender<SerializableGuid> m_DeleteTakeSender;
        readonly BinarySender<SerializableGuid> m_SetIterationBaseSender;
        readonly EventSender m_ClearIterationBase;

        /// <summary>
        /// An event invoked when the server state has been modified.
        /// </summary>
        public event Action<ServerState> ServerStateReceived;

        /// <summary>
        /// An event invoked when the player state has been modified.
        /// </summary>
        public event Action<PlayerState> PlayerStateReceived;

        /// <summary>
        /// An event invoked when the slate descriptor has been modified.
        /// </summary>
        public event Action<SlateDescriptor> SlateDescriptorReceived;

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

            m_ServerModeSender = BinarySender<ServerMode>.Get(m_Protocol, CompanionAppMessages.ToServer.SetMode);
            m_StartRecordingSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.StartRecording);
            m_StopRecordingSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.StopRecording);
            m_StartPlayerSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.PlayerStart);
            m_StopPlayerSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.PlayerStop);
            m_PausePlayerSender = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.PlayerPause);
            m_PlayerTimeSender = BinarySender<double>.Get(m_Protocol, CompanionAppMessages.ToServer.PlayerSetTime);
            m_SetSelectedTakeSender = BinarySender<SerializableGuid>.Get(m_Protocol, CompanionAppMessages.ToServer.SetSelectedTake);
            m_SetTakeDataSender = JsonSender<TakeDescriptor>.Get(m_Protocol, CompanionAppMessages.ToServer.SetTakeData);
            m_DeleteTakeSender = BinarySender<SerializableGuid>.Get(m_Protocol, CompanionAppMessages.ToServer.DeleteTake);
            m_SetIterationBaseSender = BinarySender<SerializableGuid>.Get(m_Protocol, CompanionAppMessages.ToServer.SetIterationBase);
            m_ClearIterationBase = EventSender.Get(m_Protocol, CompanionAppMessages.ToServer.ClearIterationBase);

            EventReceiver.Get(m_Protocol, CompanionAppMessages.ToClient.Initialize).AddHandler(() =>
            {
                Initialize();
            });
            BinaryReceiver<ServerState>.Get(m_Protocol, CompanionAppMessages.ToClient.ServerState).AddHandler((state) =>
            {
                ServerStateReceived?.Invoke(state);
            });
            BinaryReceiver<PlayerState>.Get(m_Protocol, CompanionAppMessages.ToClient.PlayerState).AddHandler((state) =>
            {
                PlayerStateReceived?.Invoke(state);
            });
            JsonReceiver<SlateDescriptor>.Get(m_Protocol, CompanionAppMessages.ToClient.SlateDescriptor).AddHandler((descriptor) =>
            {
                SlateDescriptorReceived?.Invoke(descriptor);
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

        /// <summary>
        /// Requests to change take metadata.
        /// </summary>
        /// <param name="descriptor">The take descriptor containing the new metadata to set.</param>
        public void SetTakeData(TakeDescriptor descriptor)
        {
            m_SetTakeDataSender.Send(descriptor);
        }

        /// <summary>
        /// Requests to delete a take using its guid.
        /// </summary>
        /// <param name="guid">The guid of the take to delete.</param>
        public void DeleteTake(SerializableGuid guid)
        {
            m_DeleteTakeSender.Send(guid);
        }

        /// <summary>
        /// Requests to set a take as an iteration base using its guid.
        /// </summary>
        /// <param name="guid">The guid of the take to set as iteration base.</param>
        public void SetIterationBase(SerializableGuid guid)
        {
            m_SetIterationBaseSender.Send(guid);
        }

        /// <summary>
        /// Requests to clear the current iteration base.
        /// </summary>
        public void ClearIterationBase()
        {
            m_ClearIterationBase.Send();
        }
    }
}
