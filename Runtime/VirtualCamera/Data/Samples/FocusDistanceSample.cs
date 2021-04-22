using System;
using System.Runtime.InteropServices;
using Unity.LiveCapture.CompanionApp;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The payload sent over the network to transport a focus distance samples.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FocusDistanceSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The focus distance in meters.
        /// </summary>
        public float focusDistance;

        /// <inheritdoc/>
        public float timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
