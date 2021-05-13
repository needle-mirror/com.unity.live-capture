using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    [Serializable]
    class TakePlayer
    {
        [SerializeField]
        PlayableAsset m_FallbackPlayableAsset;
        [SerializeField]
        PlayableDirector m_Director;
        [SerializeField]
        Take m_Take;
        IEnumerable<TrackBindingEntry> m_Entries;

        public PlayableDirector Director
        {
            get => m_Director;
            set => m_Director = value;
        }

        public PlayableAsset FallbackPlayableAsset
        {
            get => m_FallbackPlayableAsset;
            set => m_FallbackPlayableAsset = value;
        }

        /// <summary>
        /// The <see cref="LiveCapture.Take"/> to play.
        /// </summary>
        public Take Take
        {
            get => m_Take;
            set
            {
                if (m_Take != value)
                {
                    ClearSceneBindings();

                    m_Take = value;

                    HandlePlayableAssetChange();
                    SetSceneBindings();
                }
            }
        }

        public void Update()
        {
            HandlePlayableAssetChange();
        }

        void HandlePlayableAssetChange()
        {
            Debug.Assert(Director != null);

            var playableAsset = FallbackPlayableAsset;

            if (m_Take != null)
            {
                playableAsset = m_Take.Timeline;
            }

            if (Director.playableAsset != playableAsset)
            {
                Director.playableAsset = playableAsset;
                Director.DeferredEvaluate();
            }
        }

        void ClearSceneBindings()
        {
            if (Director == null || m_Entries == null)
            {
                return;
            }

            foreach (var entry in m_Entries)
            {
                Director.ClearGenericBinding(entry.Track);
            }

            m_Entries = null;
        }

        void SetSceneBindings()
        {
            m_Entries = null;

            if (m_Take == null || Director == null)
            {
                return;
            }

            m_Entries = m_Take.BindingEntries;

            if (m_Entries == null)
            {
                return;
            }

            foreach (var entry in m_Entries)
            {
                var track = entry.Track;
                var binding = entry.Binding;

                if (track == null ||  binding == null)
                {
                    continue;
                }

                var value = binding.GetValue(Director);

                Director.SetGenericBinding(track, value);
            }
        }
    }
}
