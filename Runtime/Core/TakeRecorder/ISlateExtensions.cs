using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    static class ISlateExtensions
    {
        public static void ClearSceneBindings(this ISlate slate, PlayableDirector director)
        {
            if (director == null)
                throw new ArgumentNullException(nameof(director));

            var iterationBase = slate.IterationBase;
            var take = slate.Take;

            if (iterationBase != null)
                director.ClearSceneBindings(iterationBase.BindingEntries);

            if (take != null)
                director.ClearSceneBindings(take.BindingEntries);
        }

        public static void SetSceneBindings(this ISlate slate, PlayableDirector director)
        {
            if (director == null)
                throw new ArgumentNullException(nameof(director));

            SetSceneBindingsInternal(slate, director);
            SetBindingsFromTimeline(director);
        }

        static void SetSceneBindingsInternal(ISlate slate, PlayableDirector director)
        {
            Debug.Assert(slate != null);
            Debug.Assert(director != null);

            var iterationBase = slate.IterationBase;
            var take = slate.Take;

            if (iterationBase != null)
                director.SetSceneBindings(iterationBase.BindingEntries);

            if (take != null)
                director.SetSceneBindings(take.BindingEntries);
        }

        static void SetBindingsFromTimeline(PlayableDirector director)
        {
            Debug.Assert(director != null);

            var timeline = director.playableAsset as TimelineAsset;

            if (timeline == null)
                return;

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is TakeRecorderTrack)
                {
                    foreach (var clip in track.GetClips())
                    {
                        SetSceneBindingsInternal(clip.asset as ISlate, director);
                    }
                }
            }
        }
    }
}