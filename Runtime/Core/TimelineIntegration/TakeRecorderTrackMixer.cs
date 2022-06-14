using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    class TakeRecorderTrackMixer : PlayableBehaviour, ITakeRecorderContextProvider
    {
        bool m_Initialized;
        Playable m_Playable;
        PlayableDirector m_Director;
        TakeRecorder m_TakeRecorder;
        List<PlayableAssetContext> m_Contexts = new List<PlayableAssetContext>();
        Dictionary<Playable, float> m_Weights = new Dictionary<Playable, float>();

        public override void OnPlayableCreate(Playable playable)
        {
            m_Playable = playable;
        }

        public override void OnGraphStart(Playable playable)
        {
            Initialize();
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (m_TakeRecorder != null)
            {
                m_TakeRecorder.RemoveContextProvider(this);
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            for (var i = 0; i < m_Playable.GetInputCount(); ++i)
            {
                var slatePlayable = m_Playable.GetInput(i);
                var weight = m_Playable.GetInputWeight(i);

                if (slatePlayable.IsValid() && slatePlayable.GetInputCount() > 0)
                {
                    m_Weights[slatePlayable.GetInput(0)] = weight;
                }
            }

            var graph = playable.GetGraph();
            var rootPlayable = graph.GetRootPlayable(0);
            var outputCount = graph.GetOutputCount();

            for (var i = 0; i < outputCount; ++i)
            {
                var output = graph.GetOutput(i);
                var sourcePlayable = output.GetSourcePlayable();

                if (m_Weights.TryGetValue(sourcePlayable, out var weight))
                {
                    output.SetWeight(weight);
                    sourcePlayable.SetDone(rootPlayable.IsDone());
                }
            }
        }

        void Initialize()
        {
            if (m_Initialized)
            {
                return;
            }

            if (m_TakeRecorder != null)
            {
                m_TakeRecorder.AddContextProvider(this);
            }

            Debug.Assert(m_Contexts.Count == m_Playable.GetInputCount());

            var isRecording = false;
            var activeContext = default(ITakeRecorderContext);
            
            if (m_TakeRecorder != null)
            {
                isRecording = m_TakeRecorder.IsRecording();
                activeContext = m_TakeRecorder.GetContext();
            }

            for (var i = 0; i < m_Playable.GetInputCount(); ++i)
            {
                var context = m_Contexts[i];
                var clip = context.GetClip();
                var slateAsset = clip.asset as SlatePlayableAsset;

                slateAsset.Migrate(clip.displayName);

                if (slateAsset.AutoClipName)
                {
                    clip.displayName = slateAsset.ShotName;
                }

                var inputPlayable = (ScriptPlayable<NestedTimelinePlayable>)m_Playable.GetInput(i);
                var nestedTimeline = inputPlayable.GetBehaviour();
                var isContextRecording = isRecording && context.Equals(activeContext);
                var take = isContextRecording ? slateAsset.IterationBase : slateAsset.Take;
                
                if (take != null)
                {
                    SetTrackBindingsIfNeeded(take);

                    nestedTimeline.SetTimeline(take.Timeline);
                }
            }

            m_Director.RebindPlayableGraphOutputs();

            m_Initialized = true;
        }

        public void Construct(PlayableDirector director,
            TakeRecorder takeRecorder,
            IEnumerable<TimelineClip> clips)
        {
            Debug.Assert(director != null);
            Debug.Assert(clips != null);
            Debug.Assert(m_Contexts.Count == 0);

            m_Director = director;
            m_TakeRecorder = takeRecorder;

            var hierarchyContext = TimelineHierarchyContextUtility.FromContext(new TimelineContext(director));

            foreach (var clip in clips)
            {
                m_Contexts.Add(new PlayableAssetContext(clip, hierarchyContext));
            }
        }

        public ITakeRecorderContext GetActiveContext()
        {
            Debug.Assert(m_Playable.GetInputCount() == m_Contexts.Count);

            for (var i = 0; i < m_Playable.GetInputCount(); ++i)
            {
                if (m_Playable.GetInputWeight(i) == 1f)
                {
                    return m_Contexts[i];
                }
            }

            return null;
        }

        void SetTrackBindingsIfNeeded(Take take)
        {
            Debug.Assert(take != null);

            var requiresLoadingTrackBindings = false;

            foreach (var entry in take.BindingEntries)
            {
                var track = entry.Track;
                var binding = entry.Binding;
                
                if (track == null)
                    continue;

                var exposedProperty = binding.GetValue(m_Director);
                var value = m_Director.GetGenericBinding(track);

                if (exposedProperty != null && value == null)
                {
                    requiresLoadingTrackBindings = true;

                    break;
                }
            }

            if (requiresLoadingTrackBindings)
            {
                m_Director.SetSceneBindings(take.BindingEntries);
            }
        }
    }
}
