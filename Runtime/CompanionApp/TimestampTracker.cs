namespace Unity.LiveCapture.CompanionApp
{
    class TimestampTracker
    {
        const double k_InvalidTime = -1f;

        double m_InitialTimestamp = k_InvalidTime;
        double m_InitialTime;

        public double Time { get; set; }
        public double LocalTime { get; private set; }

        public void SetTimestamp(double value)
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

            LocalTime = m_InitialTime + value - m_InitialTimestamp;
        }

        public void Reset()
        {
            m_InitialTimestamp = k_InvalidTime;
        }
    }
}
