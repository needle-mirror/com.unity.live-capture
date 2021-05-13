using System;
using System.Runtime.InteropServices;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// The payload sent over the network to transport joysticks samples.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct JoysticksSample : ISample
    {
        float m_Timestamp;
        /// <summary>
        /// The direction of the joysticks.
        /// </summary>
        public Vector3 Joysticks;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
