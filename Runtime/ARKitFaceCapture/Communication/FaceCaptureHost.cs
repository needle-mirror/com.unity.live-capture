using System.IO;
using Unity.LiveCapture.ARKitFaceCapture.Networking;
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
        readonly BinarySender<FaceSampleV0> m_FacePoseV0Sender;

        /// <inheritdoc />
        public FaceCaptureHost(NetworkBase network, Remote remote, Stream stream)
            : base(network, remote, stream)
        {
            BinarySender<FaceSampleV0>.TryGet(m_Protocol, FaceMessages.ToServer.FacePoseSample_V0, out m_FacePoseV0Sender);
        }

        /// <summary>
        /// Sends a face pose sample to the server.
        /// </summary>
        /// <param name="sample">The pose sample.</param>
        public void SendPose(FaceSample sample)
        {
            if (m_FacePoseV0Sender != null)
            {
                m_FacePoseV0Sender.Send((FaceSampleV0)sample);
            }
        }
    }
}
