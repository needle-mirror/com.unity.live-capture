using System;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Given a sparse set of time samples from an external device, maintain
    /// an estimate of the this external time using dead reckoning.
    /// </summary>
    class ClientTimeEstimator
    {
        bool m_IsCurrent;
        public double Now { get; private set; }
        public void Mark(double newTime)
        {
            Now = newTime;
            m_IsCurrent = true;
        }

        public void Update(double deltaTime)
        {
            if (!m_IsCurrent)
            {
                Now += deltaTime;
            }

            m_IsCurrent = false;
        }

        public void Reset()
        {
            Now = 0;
            m_IsCurrent = false;
        }
    }
}
