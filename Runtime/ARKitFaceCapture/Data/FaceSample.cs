using System;
using System.Runtime.InteropServices;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    /// <summary>
    /// The payload sent over the network to transport an animation pose for a face.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FaceSample : ISample
    {
        float m_Timestamp;

        /// <summary>
        /// The face pose.
        /// </summary>
        public FacePose FacePose;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
