using System;
using Unity.LiveCapture.CompanionApp.Networking;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
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
        /// The event provides the time in seconds from the start of the Slate.
        /// </remarks>
        event Action<double> SetPlayerTime;

        /// <summary>
        /// An event invoked when the client requests the take to select.
        /// </summary>
        event Action<Guid> SetSelectedTake;

        /// <summary>
        /// An event invoked when the client requests to rate a take.
        /// </summary>
        event Action<TakeDescriptor> SetTakeData;

        /// <summary>
        /// An event invoked when the client requests to delete a take.
        /// </summary>
        event Action<Guid> DeleteTake;

        /// <summary>
        /// An event invoked when the client requests to set a take as the iteration base.
        /// </summary>
        event Action<Guid> SetIterationBase;

        /// <summary>
        /// An event invoked when the client requests to operate without an iteration base.
        /// </summary>
        event Action ClearIterationBase;

        /// <summary>
        /// An event invoked when the client requests the preview texture of an asset.
        /// </summary>
        event Action<Guid> TexturePreviewRequested;

        /// <summary>
        /// Resets the communication state and notifies the client that a new session starts by sending
        /// the Initialize event.
        /// </summary>
        /// <remarks>
        /// The Initialize event is only sent once before any other event.
        /// </remarks>
        void SendInitialize();

        /// <summary>
        /// Notifies the client that the session has ended.
        /// </summary>
        /// <remarks>
        /// After the session has ended, the client can safely assume that the communicaiton is over.
        /// The client can wait for the Initialize event again to start a new session.
        /// </remarks>
        void SendEndSession();

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
        /// Sends the take recorder frame rate to the client.
        /// </summary>
        /// <param name="frameRate">The frame rate to send.</param>
        void SendFrameRate(FrameRate frameRate);

        /// <summary>
        /// Sends if there a slate assigned to the device to the client.
        /// </summary>
        /// <param name="hasSlate">Is there a slate assigned to the device.</param>
        void SendHasSlate(bool hasSlate);

        /// <summary>
        /// Sends the duration of the current slate to the client.
        /// </summary>
        /// <param name="duration">The duration of the current slate in seconds.</param>
        void SendSlateDuration(double duration);

        /// <summary>
        /// Sends if a slate is currently being previewed to the client.
        /// </summary>
        /// <param name="isPreviewing">Is a slate currently being previewed.</param>
        void SendSlateIsPreviewing(bool isPreviewing);

        /// <summary>
        /// Sends the slate preview time to the client.
        /// </summary>
        /// <param name="previewTime">The preview time in seconds.</param>
        void SendSlatePreviewTime(double previewTime);

        /// <summary>
        /// Sends the available take list to the client.
        /// </summary>
        /// <param name="descriptor">The slate information to send.</param>
        void SendSlateDescriptor(SlateDescriptor descriptor);

        /// <summary>
        /// Sends the name the take recorder will use for the next recording.
        /// </summary>
        /// <param name="name">The formatted take name.</param>
        void SendNextTakeName(string name);

        /// <summary>
        /// Sends the name this device will use for its next recording.
        /// </summary>
        /// <param name="name">The formatted asset name.</param>
        void SendNextAssetName(string name);

        /// <summary>
        /// Sends the texture preview of an asset.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        /// <param name="texture">The texture containing the preview of the asset.</param>
        void SendTexturePreview(Guid guid, Texture2D texture);
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
        readonly EventSender m_EndSessionSender;
        readonly BoolSender m_IsRecordingSender;
        readonly DataSender<DeviceMode> m_DeviceModeSender;
        readonly DataSender<FrameRate> m_FrameRateSender;
        readonly BoolSender m_HasSlateSender;
        readonly DataSender<double> m_SlateDurationSender;
        readonly DataSender<double> m_SlatePreviewTimeSender;
        readonly BoolSender m_SlateIsPreviewingSender;
        readonly BinarySender<int> m_SlateSelectedTakeSender;
        readonly BinarySender<int> m_SlateIterationBaseSender;
        readonly BinarySender<int> m_SlateTakeNumberSender;
        readonly StringSender m_SlateShotNameSender;
        readonly JsonSender<TakeDescriptorArrayV0> m_SlateTakesSender;
        readonly StringSender m_NextTakeNameSender;
        readonly StringSender m_NextAssetNameSender;
        readonly TextureSender m_TexturePreviewSender;

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
        public event Action<Guid> SetSelectedTake;

        /// <inheritdoc />
        public event Action<TakeDescriptor> SetTakeData;

        /// <inheritdoc />
        public event Action<Guid> DeleteTake;

        /// <inheritdoc />
        public event Action<Guid> SetIterationBase;

        /// <inheritdoc />
        public event Action ClearIterationBase;

        /// <inheritdoc />
        public event Action<Guid> TexturePreviewRequested;

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

            m_Protocol = new Protocol($"{GetType().Name}Protocol", PackageUtility.GetVersion(LiveCaptureInfo.Version));
            m_Protocol.SetNetwork(network, remote);

            m_InitializeSender = m_Protocol.Add(new EventSender(CompanionAppMessages.ToClient.Initialize));
            m_EndSessionSender = m_Protocol.Add(new EventSender(CompanionAppMessages.ToClient.EndSession));
            m_IsRecordingSender = m_Protocol.Add(new BoolSender(CompanionAppMessages.ToClient.IsRecordingChanged));
            m_DeviceModeSender = m_Protocol.Add(new BinarySender<DeviceMode>(CompanionAppMessages.ToClient.DeviceModeChanged));
            m_FrameRateSender = m_Protocol.Add(new BinarySender<FrameRate>(CompanionAppMessages.ToClient.FrameRate));
            m_HasSlateSender = m_Protocol.Add(new BoolSender(CompanionAppMessages.ToClient.HasSlateChanged));
            m_SlateDurationSender = m_Protocol.Add(new BinarySender<double>(CompanionAppMessages.ToClient.SlateDurationChanged));
            m_SlateIsPreviewingSender = m_Protocol.Add(new BoolSender(CompanionAppMessages.ToClient.SlateIsPreviewingChanged));
            m_SlatePreviewTimeSender = m_Protocol.Add(new BinarySender<double>(CompanionAppMessages.ToClient.SlatePreviewTimeChanged));
            m_SlateSelectedTakeSender = m_Protocol.Add(new BinarySender<int>(CompanionAppMessages.ToClient.SlateSelectedTake, options: DataOptions.None));
            m_SlateIterationBaseSender = m_Protocol.Add(new BinarySender<int>(CompanionAppMessages.ToClient.SlateIterationBase, options: DataOptions.None));
            m_SlateTakeNumberSender = m_Protocol.Add(new BinarySender<int>(CompanionAppMessages.ToClient.SlateTakeNumber, options: DataOptions.None));
            m_SlateShotNameSender = m_Protocol.Add(new StringSender(CompanionAppMessages.ToClient.SlateShotName, options: DataOptions.None));
            m_SlateTakesSender = m_Protocol.Add(new JsonSender<TakeDescriptorArrayV0>(CompanionAppMessages.ToClient.SlateTakes_V0, options: DataOptions.None));
            m_NextTakeNameSender = m_Protocol.Add(new StringSender(CompanionAppMessages.ToClient.NextTakeName));
            m_NextAssetNameSender = m_Protocol.Add(new StringSender(CompanionAppMessages.ToClient.NextAssetName));
            m_TexturePreviewSender = m_Protocol.Add(new TextureSender(CompanionAppMessages.ToClient.TexturePreview));

            m_Protocol.Add(new BinaryReceiver<DeviceMode>(CompanionAppMessages.ToServer.SetDeviceMode,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler(deviceMode =>
                {
                    SetDeviceMode?.Invoke(deviceMode);
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
            m_Protocol.Add(new BinaryReceiver<double>(CompanionAppMessages.ToServer.PlayerSetTime)).AddHandler(time =>
            {
                SetPlayerTime?.Invoke(time);
            });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.SetSelectedTake,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler(guid =>
                {
                    SetSelectedTake?.Invoke(guid);
                });
            m_Protocol.Add(new JsonReceiver<TakeDescriptorV0>(CompanionAppMessages.ToServer.SetTakeData_V0)).AddHandler(take =>
            {
                SetTakeData?.Invoke((TakeDescriptor)take);
            });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.DeleteTake,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler(guid =>
                {
                    DeleteTake?.Invoke(guid);
                });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.SetIterationBase,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler(guid =>
                {
                    SetIterationBase?.Invoke(guid);
                });
            m_Protocol.Add(new EventReceiver(CompanionAppMessages.ToServer.ClearIterationBase)).AddHandler(() =>
            {
                ClearIterationBase?.Invoke();
            });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(CompanionAppMessages.ToServer.RequestTexturePreview,
                ChannelType.UnreliableUnordered, DataOptions.None)).AddHandler((guid) =>
            {
                TexturePreviewRequested?.Invoke(guid);
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
        public void SendInitialize()
        {
            m_Protocol.Reset();

            m_InitializeSender.Send();
        }

        /// <inheritdoc />
        public void SendEndSession()
        {
            m_EndSessionSender.Send();
        }

        /// <inheritdoc />
        public void SendDeviceMode(DeviceMode deviceMode)
        {
            m_DeviceModeSender.Send(deviceMode);
        }

        /// <inheritdoc />
        public void SendRecordingState(bool isRecording)
        {
            m_IsRecordingSender.Send(isRecording);
        }

        /// <inheritdoc />
        public void SendFrameRate(FrameRate frameRate)
        {
            m_FrameRateSender.Send(frameRate);
        }

        /// <inheritdoc />
        public void SendHasSlate(bool hasSlate)
        {
            m_HasSlateSender.Send(hasSlate);
        }

        /// <inheritdoc />
        public void SendSlateDuration(double duration)
        {
            m_SlateDurationSender.Send(duration);
        }

        /// <inheritdoc />
        public void SendSlateIsPreviewing(bool isPreviewing)
        {
            m_SlateIsPreviewingSender.Send(isPreviewing);
        }

        /// <inheritdoc />
        public void SendSlatePreviewTime(double previewTime)
        {
            m_SlatePreviewTimeSender.Send(previewTime);
        }

        /// <inheritdoc />
        public void SendSlateDescriptor(SlateDescriptor descriptor)
        {
            m_SlateTakesSender.Send((TakeDescriptorArrayV0)descriptor.Takes);
            m_SlateSelectedTakeSender.Send(descriptor.SelectedTake);
            m_SlateIterationBaseSender.Send(descriptor.IterationBase);
            m_SlateTakeNumberSender.Send(descriptor.TakeNumber);
            m_SlateShotNameSender.Send(descriptor.ShotName);
        }

        /// <inheritdoc />
        public void SendNextTakeName(string name)
        {
            m_NextTakeNameSender.Send(name);
        }

        /// <inheritdoc />
        public void SendNextAssetName(string name)
        {
            m_NextAssetNameSender.Send(name);
        }

        /// <inheritdoc />
        public void SendTexturePreview(Guid guid, Texture2D texture)
        {
            m_TexturePreviewSender.Send(new TextureData(texture, guid.ToString("N")));
        }
    }
}
