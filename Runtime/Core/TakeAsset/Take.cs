using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    /// <summary>
    /// A take is a recorded performance of one or more actors. The <see cref="TakeRecorder"> stores the performance
    /// as a timeline asset.
    /// </summary>
    public class Take : ScriptableObject
    {
        [SerializeField]
        int m_SceneNumber;
        [SerializeField]
        string m_ShotName;
        [SerializeField]
        int m_TakeNumber;
        [SerializeField, TextArea(2, 4)]
        string m_Description;
        [SerializeField, OnlyStandardFrameRates]
        FrameRate m_FrameRate;
        [SerializeField]
        TimelineAsset m_Timeline;
        [SerializeField]
        List<TrackBindingEntry> m_Entries = new List<TrackBindingEntry>();
        [SerializeField]
        List<TrackMetadataEntry> m_MetadataEntries = new List<TrackMetadataEntry>();

        /// <summary>
        /// The number associated with the scene where the take was captured.
        /// </summary>
        public int sceneNumber
        {
            get => m_SceneNumber;
            internal set => m_SceneNumber = value;
        }

        /// <summary>
        /// The name of the shot where the take was captured.
        /// </summary>
        public string shotName
        {
            get => m_ShotName;
            internal set => m_ShotName = value;
        }

        /// <summary>
        /// The number associated with the take.
        /// </summary>
        public int takeNumber
        {
            get => m_TakeNumber;
            internal set => m_TakeNumber = value;
        }

        /// <summary>
        /// The description of the shot where the take was captured.
        /// </summary>
        public string description
        {
            get => m_Description;
            internal set => m_Description = value;
        }

        /// <summary>
        /// The frame rate used during the recording.
        /// </summary>
        public FrameRate frameRate
        {
            get => m_FrameRate;
            internal set => m_FrameRate = value;
        }

        /// <summary>
        /// The timeline asset containing the recorded performance.
        /// </summary>
        public TimelineAsset timeline
        {
            get => m_Timeline;
            internal set => m_Timeline = value;
        }

        internal IEnumerable<TrackBindingEntry> bindingEntries => m_Entries;

        internal IEnumerable<TrackMetadataEntry> metadataEntries => m_MetadataEntries;

        internal void AddTrackBinding(TrackAsset track, ITakeBinding binding)
        {
            m_Entries.Add(new TrackBindingEntry(track, binding));
        }

        internal void AddTrackMetadata(TrackAsset track, ITrackMetadata metadata)
        {
            m_MetadataEntries.Add(new TrackMetadataEntry(track, metadata));
        }
    }
}
