using System.Runtime.InteropServices;

namespace Unity.LiveCapture.VirtualCamera.Networking
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FocusDistanceSampleV0
    {
        float m_Timestamp;
        float m_FocusDistance;

        public static explicit operator FocusDistanceSampleV0(FocusDistanceSample sample)
        {
            return new FocusDistanceSampleV0
            {
                m_Timestamp = sample.Timestamp,
                m_FocusDistance = sample.FocusDistance,
            };
        }

        public static explicit operator FocusDistanceSample(FocusDistanceSampleV0 sample)
        {
            return new FocusDistanceSample
            {
                Timestamp = sample.m_Timestamp,
                FocusDistance = sample.m_FocusDistance,
            };
        }
    }
}
