using System;
using System.Runtime.InteropServices;
using Unity.LiveCapture.CompanionApp;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The payload sent over the network to transport a aperture samples.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ApertureSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The aperture in millimeters.
        /// </summary>
        public float aperture;

        /// <inheritdoc/>
        public float timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
