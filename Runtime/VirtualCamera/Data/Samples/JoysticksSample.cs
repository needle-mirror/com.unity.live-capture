using System;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A struct containing a joystick position sample.
    /// </summary>
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
