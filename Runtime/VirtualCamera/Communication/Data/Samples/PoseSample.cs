using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.VirtualCamera.Networking
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PoseSampleV0
    {
        float m_Timestamp;
        Pose m_Pose;

        public static explicit operator PoseSampleV0(PoseSample sample)
        {
            return new PoseSampleV0
            {
                m_Timestamp = sample.Timestamp,
                m_Pose = sample.Pose,
            };
        }

        public static explicit operator PoseSample(PoseSampleV0 sample)
        {
            return new PoseSample
            {
                Timestamp = sample.m_Timestamp,
                Pose = sample.m_Pose,
            };
        }
    }
}
