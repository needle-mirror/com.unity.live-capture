using System;
using Unity.LiveCapture.CompanionApp;
using UnityEngine;

namespace Unity.LiveCapture.ARKitFaceCapture
{
    struct FaceSample : ISample
    {
        float m_Timestamp;

        public FacePose FacePose;

        /// <inheritdoc/>
        public float Timestamp
        {
            get => m_Timestamp;
            set => m_Timestamp = value;
        }
    }
}
