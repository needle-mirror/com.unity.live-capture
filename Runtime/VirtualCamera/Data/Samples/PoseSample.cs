using System;
using System.Runtime.InteropServices;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The payload sent over the network to transport camera pose samples.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
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
