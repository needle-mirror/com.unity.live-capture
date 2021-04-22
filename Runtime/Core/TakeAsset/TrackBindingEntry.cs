using System;
using UnityEngine;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    [Serializable]
    class TrackBindingEntry
    {
        [SerializeField]
        TrackAsset m_Track;

        [SerializeReference]
        ITakeBinding m_Binding;

        public TrackAsset track => m_Track;
        public ITakeBinding binding => m_Binding;

        public TrackBindingEntry(TrackAsset track, ITakeBinding binding)
        {
            m_Track = track;
            m_Binding = binding;
        }
    }
}
