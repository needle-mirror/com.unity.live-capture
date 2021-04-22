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
    public struct PoseSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The transform pose.
        /// </summary>
        public Pose pose;

        /// <summary>
        /// The direction of the joysticks.
        /// </summary>
        public Vector3 joystick;

        /// <inheritdoc/>
        public float timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
