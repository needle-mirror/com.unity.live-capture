using System;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// A struct containing a gamepad sample.
    /// </summary>
    struct GamepadSample : ISample
    {
        double m_Time;

        /// <summary>
        /// The actuation of movement (X = Truck, Y = Pedestal, Z = Dolly).
        /// </summary>
        public Vector3 Move;

        /// <summary>
        /// The actuation of rotation in degrees (X = Tilt up, Y = Pan right, Z = Roll clockwise).
        /// </summary>
        public Vector3 Look;

        /// <summary>
        /// Returns <see cref="Look"/> converted to degrees about Unity's ZXY rotation order and handedness.
        /// </summary>
        public Vector3 UnityLook => new Vector3(-Look.x, Look.y, -Look.z);

        /// <inheritdoc/>
        public double Time
        {
            get => m_Time;
            set => m_Time = value;
        }
    }
}
