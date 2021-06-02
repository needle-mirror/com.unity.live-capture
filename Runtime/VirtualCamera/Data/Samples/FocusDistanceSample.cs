using System;
using Unity.LiveCapture.CompanionApp;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A struct containing a focus distance sample.
    /// </summary>
    struct FocusDistanceSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The focus distance in meters.
        /// </summary>
        public float FocusDistance;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
