using System.IO;
using Unity.LiveCapture.CompanionApp;
using Unity.LiveCapture.Networking;
using Unity.LiveCapture.Networking.Protocols;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// A class used to communicate with a face capture device in the Unity editor from the companion app.
    /// </summary>
    class FaceCaptureHost : CompanionAppHost
    {
        readonly DataSender<FaceSample> m_FacePoseSender;

        /// <inheritdoc />
        public FaceCaptureHost(NetworkBase network, Remote remote, Stream stream)
            : base(network, remote, stream)
        {
            m_FacePoseSender = BinarySender<FaceSample>.Get(m_Protocol, FaceMessages.ToServer.FacePoseSample);
        }

        /// <summary>
        /// Sends a face pose sample to the server.
        /// </summary>
        /// <param name="sample">The pose sample.</param>
        public void SendPose(FaceSample sample)
        {
            m_FacePoseSender.Send(sample);
        }
    }
}
