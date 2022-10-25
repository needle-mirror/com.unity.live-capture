namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Use this class for tracking changes in a <see cref="IShot"/>.
    /// </summary>
    class ShotChangeTracker
    {
        IShot m_Shot;
        Slate m_Slate;
        Take m_Take;
        Take m_IterationBase;

        /// <summary>
        /// Restarts the tracking.
        /// </summary>
        public void Reset()
        {
            m_Shot = null;
            m_Slate = Slate.Empty;
            m_Take = null;
            m_IterationBase = null;
        }

        /// <summary>
        /// Checks whether the given slate is different from a previous call to this method.
        /// </summary>
        /// <returns> true if different; otherwise, false.</returns>
        public bool Changed(IShot shot)
        {
            var changed = m_Shot != shot;

            if (shot != null)
            {
                var slate = shot.Slate;

                changed |= m_Slate != slate;
                changed |= m_Take != shot.Take;
                changed |= m_IterationBase != shot.IterationBase;

                m_Slate = slate;
                m_Take = shot.Take;
                m_IterationBase = shot.IterationBase;
            }

            m_Shot = shot;

            return changed;
        }
    }
}
