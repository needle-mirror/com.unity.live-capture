using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Timeline track that you can use to play and record a <see cref="Take"/>.
    /// </summary>
    [TrackClipType(typeof(SlatePlayableAsset))]
    [TrackBindingType(typeof(TakeRecorder))]
    [HelpURL(Documentation.baseURL + "take-system-setting-up-timeline" + Documentation.endURL)]
    class TakeRecorderTrack : TrackAsset
    {
#if UNITY_EDITOR
        bool m_GatheringProperties;
#endif

        /// <inheritdoc/>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var director = go.GetComponent<PlayableDirector>();
            var takeRecorder = director.GetGenericBinding(this) as TakeRecorder;

            if (takeRecorder == null)
            {
                return Playable.Create(graph, inputCount);
            }

            var takeRecorderDirector = takeRecorder.GetPlayableDirector();

            if (takeRecorderDirector == director)
            {
                Debug.LogWarning($"{nameof(TakeRecorderTrack)} ({name}) is referencing the same {nameof(TakeRecorder)} component as the one in which it is playing.");

                return Playable.Create(graph, inputCount);
            }

            foreach (var clip in GetClips())
            {
                var slatePlayableAsset = clip.asset as SlatePlayableAsset;

                slatePlayableAsset.Clip = clip;
                slatePlayableAsset.Director = takeRecorderDirector;
            }

            var mixerPlayable = ScriptPlayable<TakeRecorderTrackMixer>.Create(graph, inputCount);
            var mixer = mixerPlayable.GetBehaviour();

            mixer.Director = director;
            mixer.TakeRecorder = takeRecorder;

            return mixerPlayable;
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

            var takeRecorder = director.GetGenericBinding(this) as TakeRecorder;

            if (takeRecorder != null)
            {
                var takePlayerDirector = takeRecorder.GetComponent<PlayableDirector>();
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
