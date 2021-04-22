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
        /// <summary>
        /// An event invoked when a transform sample is received.
        /// </summary>
        event Action<PoseSample> poseSampleReceived;

        /// <summary>
        /// An event invoked when a focal length sample is received.
        /// </summary>
        event Action<FocalLengthSample> focalLengthSampleReceived;

        /// <summary>
        /// An event invoked when a focus distance sample is received.
        /// </summary>
        event Action<FocusDistanceSample> focusDistanceSampleReceived;

        /// <summary>
        /// An event invoked when a aperture sample is received.
        /// </summary>
        event Action<ApertureSample> apertureSampleReceived;

        /// <summary>
        /// An event invoked when the client changes the virtual camera's settings.
        /// </summary>
        event Action<CameraState> cameraStateReceived;

        /// <summary>
        /// An event invoked when the client changes the virtual camera's auto-focus reticle position.
        /// </summary>
        /// <remarks>
        /// The value is the screen space position of the reticle on the client device's screen.
        /// Use <see cref="ICompanionAppClient.screenResolution"/> to normalize the value if needed.
        /// </remarks>
        event Action<Vector2> reticlePositionReceived;

        /// <summary>
        /// An event invoked when the client wants to move the virtual camera back to the origin.
        /// </summary>
        event Action setPoseToOrigin;

        /// <summary>
        /// Sends the camera lens parameters to the client.
        /// </summary>
        /// <param name="lens">The lens parameters to send.</param>
        void SendCameraLens(Lens lens);

        /// <summary>
        /// Sends the camera body parameters to the client.
        /// </summary>
        /// <param name="body">The body parameters to send.</param>
        void SendCameraBody(CameraBody body);

        /// <summary>
        /// Sends the camera state to the client.
        /// </summary>
        /// <param name="state">The state to send.</param>
        void SendCameraState(CameraState state);

        /// <summary>
        /// Sends the video stream state to the client.
        /// </summary>
        /// <param name="state">The state to send.</param>
        void SendVideoStreamState(VideoStreamState state);
    }

    /// <summary>
    /// A class used to communicate with the virtual camera companion app.
    /// </summary>
    [Preserve]
    [Client(k_ClientType)]
    public class VirtualCameraClient : CompanionAppClient, IVirtualCameraClient
    {
        /// <summary>
        /// The type of client this device supports.
        /// </summary>
        const string k_ClientType = "Virtual Camera";

        readonly DataSender<Lens> m_CameraLensSender;
        readonly DataSender<CameraBody> m_CameraBodySender;
        readonly DataSender<CameraState> m_CameraStateSender;
        readonly DataSender<VideoStreamState> m_VideoStreamStateSender;

        /// <inheritdoc />
        public event Action<PoseSample> poseSampleReceived;
        /// <inheritdoc />
        public event Action<FocalLengthSample> focalLengthSampleReceived;
        /// <inheritdoc />
        public event Action<FocusDistanceSample> focusDistanceSampleReceived;
        /// <inheritdoc />
        public event Action<ApertureSample> apertureSampleReceived;
        /// <inheritdoc />
        public event Action<CameraState> cameraStateReceived;
        /// <inheritdoc />
        public event Action<Vector2> reticlePositionReceived;
        /// <inheritdoc />
        public event Action setPoseToOrigin;

        /// <inheritdoc />
        public VirtualCameraClient(NetworkBase network, Remote remote, ClientInitialization data)
            : base(network, remote, data)
        {
            m_CameraLensSender = m_Protocol.Add(new BinarySender<Lens>(VirtualCameraMessages.ToClient.k_CameraLens));
            m_CameraBodySender = m_Protocol.Add(new BinarySender<CameraBody>(VirtualCameraMessages.ToClient.k_CameraBody));
            m_CameraStateSender = m_Protocol.Add(new BinarySender<CameraState>(VirtualCameraMessages.ToClient.k_CameraState));
            m_VideoStreamStateSender = m_Protocol.Add(new BinarySender<VideoStreamState>(VirtualCameraMessages.ToClient.k_VideoStreamState));

            m_Protocol.Add(new BinaryReceiver<PoseSample>(VirtualCameraMessages.ToServer.k_PoseSample, ChannelType.UnreliableUnordered)).AddHandler((pose) =>
            {
                poseSampleReceived?.Invoke(pose);
            });
            m_Protocol.Add(new BinaryReceiver<FocalLengthSample>(VirtualCameraMessages.ToServer.k_FocalLengthSample, ChannelType.UnreliableUnordered)).AddHandler((focalLength) =>
            {
                focalLengthSampleReceived?.Invoke(focalLength);
            });
            m_Protocol.Add(new BinaryReceiver<FocusDistanceSample>(VirtualCameraMessages.ToServer.k_FocusDistanceSample, ChannelType.UnreliableUnordered)).AddHandler((focusDistance) =>
            {
                focusDistanceSampleReceived?.Invoke(focusDistance);
            });
            m_Protocol.Add(new BinaryReceiver<ApertureSample>(VirtualCameraMessages.ToServer.k_ApertureSample, ChannelType.UnreliableUnordered)).AddHandler((aperture) =>
            {
                apertureSampleReceived?.Invoke(aperture);
            });
            m_Protocol.Add(new BinaryReceiver<CameraState>(VirtualCameraMessages.ToServer.k_SetCameraState)).AddHandler((state) =>
            {
                cameraStateReceived?.Invoke(state);
            });
            m_Protocol.Add(new BinaryReceiver<Vector2>(VirtualCameraMessages.ToServer.k_SetReticlePosition)).AddHandler((position) =>
            {
                reticlePositionReceived?.Invoke(position);
            });
            m_Protocol.Add(new EventReceiver(VirtualCameraMessages.ToServer.k_SetPoseToOrigin)).AddHandler(() =>
            {
                setPoseToOrigin?.Invoke();
            });
        }

        /// <inheritdoc />
        public void SendCameraLens(Lens lens)
        {
            m_CameraLensSender.Send(lens);
        }

        /// <inheritdoc />
        public void SendCameraBody(CameraBody body)
        {
            m_CameraBodySender.Send(body);
        }

        /// <inheritdoc />
        public void SendCameraState(CameraState state)
        {
            m_CameraStateSender.Send(state);
        }

        /// <inheritdoc />
        public void SendVideoStreamState(VideoStreamState state)
        {
            m_VideoStreamStateSender.Send(state);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => k_ClientType;
    }
}
