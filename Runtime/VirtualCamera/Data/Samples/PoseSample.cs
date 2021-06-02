using System;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A struct containing a pose sample.
    /// </summary>
    struct PoseSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The transform pose.
        /// </summary>
        public Pose Pose;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
