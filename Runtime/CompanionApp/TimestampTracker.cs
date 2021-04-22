namespace Unity.LiveCapture.CompanionApp
{
    internal class TimestampTracker
    {
        const float k_InvalidTime = -1f;

        float m_InitialTimestamp = k_InvalidTime;
        float m_InitialTime;
        float m_LocalTime;

        public float time { get; set; }
        public float localTime => m_LocalTime;

        public void SetTimestamp(float value)
        {
            if (m_InitialTimestamp == k_InvalidTime)
            {
                m_InitialTimestamp = value;
                m_InitialTime = time;
            }

            m_LocalTime = m_InitialTime + value - m_InitialTimestamp;;
        }

        public void Reset()
        {
            m_InitialTimestamp = k_InvalidTime;
        }
    }
}
