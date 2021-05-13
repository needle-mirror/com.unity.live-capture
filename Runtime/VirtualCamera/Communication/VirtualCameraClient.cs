using System;
using Unity.LiveCapture.CompanionApp;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// An interface used to communicate with the virtual camera companion app.
    /// </summary>
    public interface IVirtualCameraClient : ICompanionAppClient
    {
    }

    interface IVirtualCameraClientInternal : IVirtualCameraClient, ICompanionAppClientInternal
    {
        /// <summary>
        /// An event invoked when channel flags are received.
        /// </summary>
        event Action<VirtualCameraChannelFlags> ChannelFlagsReceived;

        /// <summary>
        /// An event invoked when a transform sample is received.
        /// </summary>
        event Action<PoseSample> PoseSampleReceived;

        /// <summary>
        /// An event invoked when a focal length sample is received.
        /// </summary>
        event Action<FocalLengthSample> FocalLengthSampleReceived;

        /// <summary>
        /// An event invoked when a focus distance sample is received.
        /// </summary>
        event Action<FocusDistanceSample> FocusDistanceSampleReceived;

        /// <summary>
        /// An event invoked when a aperture sample is received.
        /// </summary>
        event Action<ApertureSample> ApertureSampleReceived;

        /// <summary>
        /// An event invoked when the client changes the virtual camera's settings.
        /// </summary>
        event Action<Settings> SettingsReceived;

        /// <summary>
        /// An event invoked when the client requests the lens asset to select.
        /// </summary>
        event Action<SerializableGuid> SetLensAsset;

        /// <summary>
        /// An event invoked when the client requests to take a snapshot.
        /// </summary>
        event Action TakeSnapshot;

        /// <summary>
        /// An event invoked when the client requests to go to a snapshot.
        /// </summary>
        event Action<int> GoToSnapshot;

        /// <summary>
        /// An event invoked when the client requests to load a shapshot.
        /// </summary>
        event Action<int> LoadSnapshot;

        /// <summary>
        /// An event invoked when the client requests to delete a shapshot.
        /// </summary>
        event Action<int> DeleteSnapshot;

        /// <summary>
        /// An event invoked when the client changes the virtual camera's auto-focus reticle position.
        /// </summary>
        /// <remarks>
        /// The value is the screen space position of the reticle on the client device's screen.
        /// Use <see cref="ICompanionAppClientInternal.ScreenResolution"/> to normalize the value if needed.
        /// </remarks>
        event Action<Vector2> ReticlePositionReceived;

        /// <summary>
        /// An event invoked when a joystick sample is received.
        /// </summary>
        event Action<JoysticksSample> JoysticksSampleReceived;

        /// <summary>
        /// An event invoked when the client wants to move the virtual camera back to the origin.
        /// </summary>
        event Action SetPoseToOrigin;

        /// <summary>
        /// Sends the <see cref="VirtualCameraChannelFlags"/> parameters to the client.
        /// </summary>
        /// <param name="channelFlags">The channel flags to send.</param>
        void SendChannelFlags(VirtualCameraChannelFlags channelFlags);

        /// <summary>
        /// Sends the <see cref="Lens"/> parameters to the client.
        /// </summary>
        /// <param name="lens">The lens parameters to send.</param>
        void SendLens(Lens lens);

        /// <summary>
        /// Sends the camera body parameters to the client.
        /// </summary>
        /// <param name="body">The body parameters to send.</param>
        void SendCameraBody(CameraBody body);

        /// <summary>
        /// Sends the camera settings to the client.
        /// </summary>
        /// <param name="settings">The state to send.</param>
        void SendSettings(Settings settings);

        /// <summary>
        /// Sends the video stream state to the client.
        /// </summary>
        /// <param name="state">The state to send.</param>
        void SendVideoStreamState(VideoStreamState state);

        /// <summary>
        /// Sends the lens kit descriptor to the client.
        /// </summary>
        /// <param name="descriptor">The lens kit information to send.</param>
        void SendLensKitDescriptor(LensKitDescriptor descriptor);

        /// <summary>
        /// Sends the snapshot list descriptor to the client.
        /// </summary>
        /// <param name="descriptor">The snapshot list information to send.</param>
        void SendSnapshotListDescriptor(SnapshotListDescriptor descriptor);
    }

    /// <summary>
    /// A class used to communicate with the virtual camera companion app.
    /// </summary>
    [Preserve]
    [Client(k_ClientType)]
    class VirtualCameraClient : CompanionAppClient, IVirtualCameraClientInternal
    {
        /// <summary>
        /// The type of client this device supports.
        /// </summary>
        const string k_ClientType = "Virtual Camera";

        readonly DataSender<VirtualCameraChannelFlags> m_ChannelFlagsSender;
        readonly DataSender<Lens> m_LensSender;
        readonly DataSender<CameraBody> m_CameraBodySender;
        readonly DataSender<Settings> m_SettingsSender;
        readonly DataSender<VideoStreamState> m_VideoStreamStateSender;
        readonly JsonSender<LensKitDescriptor> m_LensKitDescriptorSender;
        readonly JsonSender<SnapshotListDescriptor> m_SnapshotListDescriptorSender;

        /// <inheritdoc />
        public event Action<VirtualCameraChannelFlags> ChannelFlagsReceived;
        /// <inheritdoc />
        public event Action<PoseSample> PoseSampleReceived;
        /// <inheritdoc />
        public event Action<FocalLengthSample> FocalLengthSampleReceived;
        /// <inheritdoc />
        public event Action<FocusDistanceSample> FocusDistanceSampleReceived;
        /// <inheritdoc />
        public event Action<ApertureSample> ApertureSampleReceived;
        /// <inheritdoc />
        public event Action<Settings> SettingsReceived;
        /// <inheritdoc />
        public event Action<Vector2> ReticlePositionReceived;
        /// <inheritdoc />
        public event Action<JoysticksSample> JoysticksSampleReceived;
        /// <inheritdoc />
        public event Action SetPoseToOrigin;
        /// <inheritdoc />
        public event Action<SerializableGuid> SetLensAsset;
        /// <inheritdoc />
        public event Action TakeSnapshot;
        /// <inheritdoc />
        public event Action<int> GoToSnapshot;
        /// <inheritdoc />
        public event Action<int> LoadSnapshot;
        /// <inheritdoc />
        public event Action<int> DeleteSnapshot;

        /// <inheritdoc />
        public VirtualCameraClient(NetworkBase network, Remote remote, ClientInitialization data)
            : base(network, remote, data)
        {
            m_ChannelFlagsSender = m_Protocol.Add(new BinarySender<VirtualCameraChannelFlags>(VirtualCameraMessages.ToClient.ChannelFlags));
            m_LensSender = m_Protocol.Add(new BinarySender<Lens>(VirtualCameraMessages.ToClient.CameraLens));
            m_CameraBodySender = m_Protocol.Add(new BinarySender<CameraBody>(VirtualCameraMessages.ToClient.CameraBody));
            m_SettingsSender = m_Protocol.Add(new BinarySender<Settings>(VirtualCameraMessages.ToClient.Settings));
            m_VideoStreamStateSender = m_Protocol.Add(new BinarySender<VideoStreamState>(VirtualCameraMessages.ToClient.VideoStreamState));
            m_LensKitDescriptorSender = m_Protocol.Add(new JsonSender<LensKitDescriptor>(VirtualCameraMessages.ToClient.LensKitDescriptor));
            m_SnapshotListDescriptorSender = m_Protocol.Add(new JsonSender<SnapshotListDescriptor>(VirtualCameraMessages.ToClient.SnapshotListDescriptor));

            m_Protocol.Add(new BinaryReceiver<VirtualCameraChannelFlags>(VirtualCameraMessages.ToServer.ChannelFlags, ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((flags) =>
            {
                ChannelFlagsReceived?.Invoke(flags);
            });
            m_Protocol.Add(new BinaryReceiver<PoseSample>(VirtualCameraMessages.ToServer.PoseSample, ChannelType.UnreliableUnordered)).AddHandler((pose) =>
            {
                PoseSampleReceived?.Invoke(pose);
            });
            m_Protocol.Add(new BinaryReceiver<JoysticksSample>(VirtualCameraMessages.ToServer.JoysticksSample, ChannelType.UnreliableUnordered, DataOptions.None)).AddHandler((joysticks) =>
            {
                JoysticksSampleReceived?.Invoke(joysticks);
            });
            m_Protocol.Add(new BinaryReceiver<FocalLengthSample>(VirtualCameraMessages.ToServer.FocalLengthSample, ChannelType.UnreliableUnordered)).AddHandler((focalLength) =>
            {
                FocalLengthSampleReceived?.Invoke(focalLength);
            });
            m_Protocol.Add(new BinaryReceiver<FocusDistanceSample>(VirtualCameraMessages.ToServer.FocusDistanceSample, ChannelType.UnreliableUnordered)).AddHandler((focusDistance) =>
            {
                FocusDistanceSampleReceived?.Invoke(focusDistance);
            });
            m_Protocol.Add(new BinaryReceiver<ApertureSample>(VirtualCameraMessages.ToServer.ApertureSample, ChannelType.UnreliableUnordered)).AddHandler((aperture) =>
            {
                ApertureSampleReceived?.Invoke(aperture);
            });
            m_Protocol.Add(new BinaryReceiver<Settings>(VirtualCameraMessages.ToServer.SetSettings)).AddHandler((state) =>
            {
                SettingsReceived?.Invoke(state);
            });
            m_Protocol.Add(new BinaryReceiver<Vector2>(VirtualCameraMessages.ToServer.SetReticlePosition)).AddHandler((position) =>
            {
                ReticlePositionReceived?.Invoke(position);
            });
            m_Protocol.Add(new EventReceiver(VirtualCameraMessages.ToServer.SetPoseToOrigin)).AddHandler(() =>
            {
                SetPoseToOrigin?.Invoke();
            });
            m_Protocol.Add(new BinaryReceiver<SerializableGuid>(VirtualCameraMessages.ToServer.SetLensAsset,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((guid) =>
                {
                    SetLensAsset?.Invoke(guid);
                });
            m_Protocol.Add(new EventReceiver(VirtualCameraMessages.ToServer.TakeSnapshot)).AddHandler(() =>
            {
                TakeSnapshot?.Invoke();
            });
            m_Protocol.Add(new BinaryReceiver<int>(VirtualCameraMessages.ToServer.GoToSnapshot,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((index) =>
                {
                    GoToSnapshot?.Invoke(index);
                });
            m_Protocol.Add(new BinaryReceiver<int>(VirtualCameraMessages.ToServer.LoadSnapshot,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((index) =>
                {
                    LoadSnapshot?.Invoke(index);
                });
            m_Protocol.Add(new BinaryReceiver<int>(VirtualCameraMessages.ToServer.DeleteSnapshot,
                ChannelType.ReliableOrdered, DataOptions.None)).AddHandler((index) =>
                {
                    DeleteSnapshot?.Invoke(index);
                });
        }

        /// <inheritdoc />
        public void SendChannelFlags(VirtualCameraChannelFlags channelFlags)
        {
            m_ChannelFlagsSender.Send(channelFlags);
        }

        /// <inheritdoc />
        public void SendLens(Lens lens)
        {
            m_LensSender.Send(lens);
        }

        /// <inheritdoc />
        public void SendCameraBody(CameraBody body)
        {
            m_CameraBodySender.Send(body);
        }

        /// <inheritdoc />
        public void SendSettings(Settings state)
        {
            m_SettingsSender.Send(state);
        }

        /// <inheritdoc />
        public void SendVideoStreamState(VideoStreamState state)
        {
            m_VideoStreamStateSender.Send(state);
        }

        /// <inheritdoc />
        public void SendLensKitDescriptor(LensKitDescriptor descriptor)
        {
            m_LensKitDescriptorSender.Send(descriptor);
        }

        /// <inheritdoc />
        public void SendSnapshotListDescriptor(SnapshotListDescriptor descriptor)
        {
            m_SnapshotListDescriptorSender.Send(descriptor);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => k_ClientType;
    }
}
