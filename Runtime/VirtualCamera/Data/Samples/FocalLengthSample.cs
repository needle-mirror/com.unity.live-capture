using System;
using Unity.LiveCapture.CompanionApp;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A struct containing a focal length sample.
    /// </summary>
    struct FocalLengthSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The focal length in millimeters.
        /// </summary>
        public float FocalLength;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
