using System;
using UnityEngine;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    [Serializable]
    class TrackMetadataEntry
    {
        [SerializeField]
        TrackAsset m_Track;

        [SerializeReference]
        ITrackMetadata m_Metadata;

        public TrackAsset track => m_Track;
        public ITrackMetadata metadata => m_Metadata;

        public TrackMetadataEntry(TrackAsset track, ITrackMetadata metadata)
        {
            m_Track = track;
            m_Metadata = metadata;
        }
    }
}
