using System.Runtime.InteropServices;

namespace Unity.LiveCapture.VirtualCamera.Networking
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct FocalLengthSampleV0
    {
        float m_Timestamp;
        float m_FocalLength;

        public static explicit operator FocalLengthSampleV0(FocalLengthSample sample)
        {
            return new FocalLengthSampleV0
            {
                m_Timestamp = sample.Timestamp,
                m_FocalLength = sample.FocalLength,
            };
        }

        public static explicit operator FocalLengthSample(FocalLengthSampleV0 sample)
        {
            return new FocalLengthSample
            {
                Timestamp = sample.m_Timestamp,
                FocalLength = sample.m_FocalLength,
            };
        }
    }
}
