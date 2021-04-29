namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Use this class for tracking changes in a slate.
    /// </summary>
    class SlateChangeTracker
    {
        ISlate m_Slate;
        int m_SceneNumber;
        string m_ShotName;
        int m_TakeNumber;
        string m_Description;
        Take m_Take;
        Take m_IterationBase;

        /// <summary>
        /// Restarts the tracking.
        /// </summary>
        public void Reset()
        {
            m_Slate = null;
            m_SceneNumber = 0;
            m_ShotName = string.Empty;
            m_TakeNumber = 0;
            m_Description = string.Empty;
            m_Take = null;
            m_IterationBase = null;
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
                changed |= m_SceneNumber != slate.SceneNumber;
                changed |= m_ShotName != slate.ShotName;
                changed |= m_TakeNumber != slate.TakeNumber;
                changed |= m_Description != slate.Description;
                changed |= m_Take != slate.Take;
                changed |= m_IterationBase != slate.IterationBase;

                m_Slate = null;
                m_SceneNumber = slate.SceneNumber;
                m_ShotName = slate.ShotName;
                m_TakeNumber = slate.TakeNumber;
                m_Description = slate.Description;
                m_Take = slate.Take;
                m_IterationBase = slate.IterationBase;
            }

            m_Slate = slate;

            return changed;
        }
    }
}
