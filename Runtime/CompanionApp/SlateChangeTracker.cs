namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Use this class for tracking changes in a slate.
    /// </summary>
    class SlateChangeTracker
    {
        ISlate m_Slate;
        Take m_Take;

        /// <summary>
        /// Restarts the tracking.
        /// </summary>
        public void Reset()
        {
            m_Slate = null;
            m_Take = null;
        }

        /// <summary>
        /// Checks whether the given slate is different from a previous call to this method.
        /// </summary>
        /// <returns> true if different; otherwise, false.</returns>
        public bool Changed(ISlate slate)
        {
            var changed = m_Slate != slate;

            if (slate != null)
            {
                changed |= m_Take != slate.take;

                m_Take = slate.take;
            }

            m_Slate = slate;

            return changed;
        }
    }
}
