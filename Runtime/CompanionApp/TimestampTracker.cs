namespace Unity.LiveCapture.CompanionApp
{
    class TimestampTracker
    {
        const float k_InvalidTime = -1f;

        float m_InitialTimestamp = k_InvalidTime;
        float m_InitialTime;
        float m_LocalTime;

        public float Time { get; set; }
        public float LocalTime => m_LocalTime;

        public void SetTimestamp(float value)
        {
            if (value < m_InitialTimestamp)
            {
                Reset();
            }

            if (m_InitialTimestamp == k_InvalidTime)
            {
                m_InitialTimestamp = value;
                m_InitialTime = Time;
            }

            m_LocalTime = m_InitialTime + value - m_InitialTimestamp;
        }

        public void Reset()
        {
            m_InitialTimestamp = k_InvalidTime;
        }
    }
}
