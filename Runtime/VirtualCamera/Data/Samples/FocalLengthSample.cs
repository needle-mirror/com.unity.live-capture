using System;
using System.Runtime.InteropServices;
using Unity.LiveCapture.CompanionApp;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The payload sent over the network to transport a focal length samples.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FocalLengthSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The focal length in millimeters.
        /// </summary>
        public float focalLength;

        /// <inheritdoc/>
        public float timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
