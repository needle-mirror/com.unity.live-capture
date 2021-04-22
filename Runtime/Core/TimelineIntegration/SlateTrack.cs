using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Timeline track that you can use to play a recorded <see cref="Take"/>.
    /// </summary>
    [TrackClipType(typeof(SlatePlayableAsset))]
    [TrackBindingType(typeof(SlateDatabase))]
    public class SlateTrack : TrackAsset
    {
#if UNITY_EDITOR
        bool m_GatheringProperties;
#endif

        /// <inheritdoc/>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var director = go.GetComponent<PlayableDirector>();
            var slateDatabase = director.GetGenericBinding(this) as SlateDatabase;

            if (slateDatabase != null)
            {
                var slateDatabaseDirector = slateDatabase.GetComponent<PlayableDirector>();

                if (slateDatabaseDirector == director)
                {
                    slateDatabase = null;
                    Debug.LogWarning($"{nameof(SlateTrack)} ({name}) is referencing the same {nameof(SlateDatabase)} component than the one in which it is playing.");
                }
            }

            foreach (var clip in GetClips())
            {
                var slatePlayableAsset = clip.asset as SlatePlayableAsset;

                slatePlayableAsset.clip = clip;
                slatePlayableAsset.director = director;
                slatePlayableAsset.slateDatabase = slateDatabase;
            }

            return Playable.Create(graph, inputCount);
        }

#if UNITY_EDITOR
        /// <inheritdoc/>
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            if (m_GatheringProperties)
            {
                return;
            }

            m_GatheringProperties = true;

            var slateDatabase = director.GetGenericBinding(this) as SlateDatabase;

            if (slateDatabase != null)
            {
                var takePlayerDirector = slateDatabase.GetComponent<PlayableDirector>();
                var timeline = takePlayerDirector.playableAsset as TimelineAsset;

                if (timeline != null)
                {
                    timeline.GatherProperties(takePlayerDirector, driver);
                }
            }

            m_GatheringProperties = false;
        }

#endif
    }
}
