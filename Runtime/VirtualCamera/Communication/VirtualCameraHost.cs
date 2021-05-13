using System;
using System.IO;
using Unity.LiveCapture.CompanionApp;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A class used to communicate with a virtual camera device in the Unity editor from the companion app.
    /// </summary>
    class VirtualCameraHost : CompanionAppHost
    {
        readonly DataSender<VirtualCameraChannelFlags> m_ChannelFlagsSender;
        readonly DataSender<PoseSample> m_PoseSender;
        readonly DataSender<JoysticksSample> m_JoysticksSender;
        readonly DataSender<FocalLengthSample> m_FocalLengthSender;
        readonly DataSender<FocusDistanceSample> m_FocusDistanceSender;
        readonly DataSender<ApertureSample> m_ApertureSender;
        readonly DataSender<Settings> m_SettingsSender;
        readonly DataSender<Vector2> m_ReticlePositionSender;
        readonly EventSender m_PoseToOriginSender;
        readonly BinarySender<SerializableGuid> m_SetLensAssetSender;
        readonly EventSender m_TakeSnapshotSender;
        readonly BinarySender<int> m_GoToSnapshotSender;
        readonly BinarySender<int> m_LoadSnapshotSender;
        readonly BinarySender<int> m_DeleteSnapshotSender;

        /// <summary>
        /// An event invoked when this client has been assigned to a virtual camera.
        /// </summary>
        public event Action Initializing;

        /// <summary>
        /// An event invoked when updated camera channel flags are received.
        /// </summary>
        public event Action<VirtualCameraChannelFlags> ChannelFlagsReceived;

        /// <summary>
        /// An event invoked when updated camera lens parameters are received.
        /// </summary>
        public event Action<Lens> CameraLensReceived;

        /// <summary>
        /// An event invoked when updated camera body parameters are received.
        /// </summary>
        public event Action<CameraBody> CameraBodyReceived;

        /// <summary>
        /// An event invoked when the camera state has been modified.
        /// </summary>
        public event Action<Settings> SettingsReceived;

        /// <summary>
        /// An event invoked when the video stream has been modified.
        /// </summary>
        public event Action<VideoStreamState> VideoStreamStateReceived;

        /// <summary>
        /// An event invoked when the lens kit descriptor has been modified.
        /// </summary>
        public event Action<LensKitDescriptor> LensKitDescriptorReceived;

        /// <summary>
        /// An event invoked when the snapshot list descriptor has been modified.
        /// </summary>
        public event Action<SnapshotListDescriptor> SnapshotListDescriptorReceived;

        /// <inheritdoc />
        public VirtualCameraHost(NetworkBase network, Remote remote, Stream stream)
            : base(network, remote, stream)
        {
            m_ChannelFlagsSender = BinarySender<VirtualCameraChannelFlags>.Get(m_Protocol, VirtualCameraMessages.ToServer.ChannelFlags);
            m_PoseSender = BinarySender<PoseSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.PoseSample);
            m_JoysticksSender = BinarySender<JoysticksSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.JoysticksSample);
            m_FocalLengthSender = BinarySender<FocalLengthSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.FocalLengthSample);
            m_FocusDistanceSender = BinarySender<FocusDistanceSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.FocusDistanceSample);
            m_ApertureSender = BinarySender<ApertureSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.ApertureSample);
            m_SettingsSender = BinarySender<Settings>.Get(m_Protocol, VirtualCameraMessages.ToServer.SetSettings);
            m_ReticlePositionSender = BinarySender<Vector2>.Get(m_Protocol, VirtualCameraMessages.ToServer.SetReticlePosition);
            m_PoseToOriginSender = EventSender.Get(m_Protocol, VirtualCameraMessages.ToServer.SetPoseToOrigin);
            m_SetLensAssetSender = BinarySender<SerializableGuid>.Get(m_Protocol, VirtualCameraMessages.ToServer.SetLensAsset);
            m_TakeSnapshotSender = EventSender.Get(m_Protocol, VirtualCameraMessages.ToServer.TakeSnapshot);
            m_GoToSnapshotSender = BinarySender<int>.Get(m_Protocol, VirtualCameraMessages.ToServer.GoToSnapshot);
            m_LoadSnapshotSender = BinarySender<int>.Get(m_Protocol, VirtualCameraMessages.ToServer.LoadSnapshot);
            m_DeleteSnapshotSender = BinarySender<int>.Get(m_Protocol, VirtualCameraMessages.ToServer.DeleteSnapshot);

            BinaryReceiver<VirtualCameraChannelFlags>.Get(m_Protocol, VirtualCameraMessages.ToClient.ChannelFlags).AddHandler((flags) =>
            {
                ChannelFlagsReceived?.Invoke(flags);
            });
            BinaryReceiver<Lens>.Get(m_Protocol, VirtualCameraMessages.ToClient.CameraLens).AddHandler((lens) =>
            {
                CameraLensReceived?.Invoke(lens);
            });
            BinaryReceiver<CameraBody>.Get(m_Protocol, VirtualCameraMessages.ToClient.CameraBody).AddHandler((body) =>
            {
                CameraBodyReceived?.Invoke(body);
            });
            BinaryReceiver<Settings>.Get(m_Protocol, VirtualCameraMessages.ToClient.Settings).AddHandler((state) =>
            {
                SettingsReceived?.Invoke(state);
            });
            BinaryReceiver<VideoStreamState>.Get(m_Protocol, VirtualCameraMessages.ToClient.VideoStreamState).AddHandler((state) =>
            {
                VideoStreamStateReceived?.Invoke(state);
            });
            JsonReceiver<LensKitDescriptor>.Get(m_Protocol, VirtualCameraMessages.ToClient.LensKitDescriptor).AddHandler((descriptor) =>
            {
                LensKitDescriptorReceived?.Invoke(descriptor);
            });
            JsonReceiver<SnapshotListDescriptor>.Get(m_Protocol, VirtualCameraMessages.ToClient.SnapshotListDescriptor).AddHandler((descriptor) =>
            {
                SnapshotListDescriptorReceived?.Invoke(descriptor);
            });
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            Initializing?.Invoke();
        }

        /// <summary>
        /// Sends channel flags to the server.
        /// </summary>
        /// <param name="channelFlags">The channel flags.</param>
        public void SendChannelFlags(VirtualCameraChannelFlags channelFlags)
        {
            m_ChannelFlagsSender.Send(channelFlags);
        }

        /// <summary>
        /// Sends a joystick sample to the server.
        /// </summary>
        /// <param name="sample">The joystick sample.</param>
        public void SendJoysticks(JoysticksSample sample)
        {
            m_JoysticksSender.Send(sample);
        }

        /// <summary>
        /// Sends a pose sample to the server.
        /// </summary>
        /// <param name="sample">The pose sample.</param>
        public void SendPose(PoseSample sample)
        {
            m_PoseSender.Send(sample);
        }

        /// <summary>
        /// Sends a focal length sample to the server.
        /// </summary>
        /// <param name="sample">The focal length sample.</param>
        public void SendFocalLength(FocalLengthSample sample)
        {
            m_FocalLengthSender.Send(sample);
        }

        /// <summary>
        /// Sends a focus distance sample to the server.
        /// </summary>
        /// <param name="sample">The aperture sample.</param>
        public void SendFocusDistance(FocusDistanceSample sample)
        {
            m_FocusDistanceSender.Send(sample);
        }

        /// <summary>
        /// Sends an aperture sample to the server.
        /// </summary>
        /// <param name="sample">The aperture sample.</param>
        public void SendAperture(ApertureSample sample)
        {
            m_ApertureSender.Send(sample);
        }

        /// <summary>
        /// Sets the virtual camera's settings.
        /// </summary>
        /// <param name="settings">The state to set.</param>
        public void SetSettings(Settings settings)
        {
            m_SettingsSender.Send(settings);
        }

        /// <summary>
        /// Sets the virtual camera's auto-focus reticle position.
        /// </summary>
        /// <param name="position">The screen space position of the reticle on the client device's screen.</param>
        public void SetReticlePosition(Vector2 position)
        {
            m_ReticlePositionSender.Send(position);
        }

        /// <summary>
        /// Moves the virtual camera back to the origin.
        /// </summary>
        public void SetPoseToOrigin()
        {
            m_PoseToOriginSender.Send();
        }

        /// <summary>
        /// Requests to set a lens asset with Guid.
        /// </summary>
        /// <param name="guid">The Guid of the lens asset to set.</param>
        public void SetLensAsset(SerializableGuid guid)
        {
            m_SetLensAssetSender.Send(guid);
        }

        /// <summary>
        /// Requests to take a snapshot.
        /// </summary>
        public void TakeSnapshot()
        {
            m_TakeSnapshotSender.Send();
        }

        /// <summary>
        /// Requests to go to a snapshot with index.
        /// </summary>
        /// <param name="index">The index of the snapshot in the list to set.</param>
        public void GoToSnapshot(int index)
        {
            m_GoToSnapshotSender.Send(index);
        }

        /// <summary>
        /// Requests to load a snapshot with index.
        /// </summary>
        /// <param name="index">The index of the snapshot in the list to set.</param>
        public void LoadSnapshot(int index)
        {
            m_LoadSnapshotSender.Send(index);
        }

        /// <summary>
        /// Requests to delete a snapshot with index.
        /// </summary>
        /// <param name="index">The index of the snapshot in the list to delete.</param>
        public void DeleteSnapshot(int index)
        {
            m_DeleteSnapshotSender.Send(index);
        }
    }
}
