using System.Runtime.InteropServices;

namespace Unity.LiveCapture.VirtualCamera.Networking
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ApertureSampleV0
    {
        float m_Timestamp;
        float m_Aperture;

        public static explicit operator ApertureSampleV0(ApertureSample sample)
        {
            return new ApertureSampleV0
            {
                m_Timestamp = sample.Timestamp,
                m_Aperture = sample.Aperture,
            };
        }

        public static explicit operator ApertureSample(ApertureSampleV0 sample)
        {
            return new ApertureSample
            {
                Timestamp = sample.m_Timestamp,
                Aperture = sample.m_Aperture,
            };
        }
    }
}
