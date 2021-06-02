using System;
using Unity.LiveCapture.CompanionApp;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A struct containing an aperture sample.
    /// </summary>
    struct ApertureSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The aperture in millimeters.
        /// </summary>
        public float Aperture;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
