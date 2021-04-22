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
    public class VirtualCameraHost : CompanionAppHost
    {
        readonly DataSender<PoseSample> m_PoseSender;
        readonly DataSender<FocalLengthSample> m_FocalLengthSender;
        readonly DataSender<FocusDistanceSample> m_FocusDistanceSender;
        readonly DataSender<ApertureSample> m_ApertureSender;
        readonly DataSender<CameraState> m_CameraStateSender;
        readonly DataSender<Vector2> m_ReticlePositionSender;
        readonly EventSender m_PoseToOriginSender;

        /// <summary>
        /// An event invoked when this client has been assigned to a virtual camera.
        /// </summary>
        public event Action initialize;

        /// <summary>
        /// An event invoked when updated camera lens parameters are received.
        /// </summary>
        public event Action<Lens> cameraLensReceived;

        /// <summary>
        /// An event invoked when updated camera body parameters are received.
        /// </summary>
        public event Action<CameraBody> cameraBodyReceived;

        /// <summary>
        /// An event invoked when the camera state has been modified.
        /// </summary>
        public event Action<CameraState> cameraStateReceived;

        /// <summary>
        /// An event invoked when the video stream has been modified.
        /// </summary>
        public event Action<VideoStreamState> videoStreamStateReceived;

        /// <inheritdoc />
        public VirtualCameraHost(NetworkBase network, Remote remote, Stream stream)
            : base(network, remote, stream)
        {
            m_PoseSender = BinarySender<PoseSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.k_PoseSample);
            m_FocalLengthSender = BinarySender<FocalLengthSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.k_FocalLengthSample);
            m_FocusDistanceSender = BinarySender<FocusDistanceSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.k_FocusDistanceSample);
            m_ApertureSender = BinarySender<ApertureSample>.Get(m_Protocol, VirtualCameraMessages.ToServer.k_ApertureSample);
            m_CameraStateSender = BinarySender<CameraState>.Get(m_Protocol, VirtualCameraMessages.ToServer.k_SetCameraState);
            m_ReticlePositionSender = BinarySender<Vector2>.Get(m_Protocol, VirtualCameraMessages.ToServer.k_SetReticlePosition);
            m_PoseToOriginSender = EventSender.Get(m_Protocol, VirtualCameraMessages.ToServer.k_SetPoseToOrigin);

            BinaryReceiver<Lens>.Get(m_Protocol, VirtualCameraMessages.ToClient.k_CameraLens).AddHandler((lens) =>
            {
                cameraLensReceived?.Invoke(lens);
            });
            BinaryReceiver<CameraBody>.Get(m_Protocol, VirtualCameraMessages.ToClient.k_CameraBody).AddHandler((body) =>
            {
                cameraBodyReceived?.Invoke(body);
            });
            BinaryReceiver<CameraState>.Get(m_Protocol, VirtualCameraMessages.ToClient.k_CameraState).AddHandler((state) =>
            {
                cameraStateReceived?.Invoke(state);
            });
            BinaryReceiver<VideoStreamState>.Get(m_Protocol, VirtualCameraMessages.ToClient.k_VideoStreamState).AddHandler((state) =>
            {
                videoStreamStateReceived?.Invoke(state);
            });
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            initialize?.Invoke();
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
        /// Sets the virtual camera's state.
        /// </summary>
        /// <param name="state">The state to set.</param>
        public void SetCameraState(CameraState state)
        {
            m_CameraStateSender.Send(state);
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
    }
}
