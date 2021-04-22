using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Playable Asset class for <see cref="SlateTrack"/>
    /// </summary>
    public class SlatePlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        const string k_DefaultDirectory = "Assets/Takes";

        [SerializeField]
        int m_SceneNumber = 1;
        [SerializeField]
        int m_TakeNumber = 1;
        [SerializeField]
        string m_Description;
        [SerializeField]
        string m_Directory = k_DefaultDirectory;
        [SerializeField]
        Take m_Take;
        [SerializeField]
        Take m_IterationBase;

        internal TimelineClip clip { get; set; }

        internal PlayableDirector director { get; set; }

        internal SlateDatabase slateDatabase { get; set; }

        internal int sceneNumber
        {
            get => m_SceneNumber;
            set => m_SceneNumber = value;
        }

        internal int takeNumber
        {
            get => m_TakeNumber;
            set => m_TakeNumber = value;
        }

        internal string description
        {
            get => m_Description;
            set => m_Description = value;
        }

        internal string directory
        {
            get => m_Directory;
            set => m_Directory = value;
        }

        internal Take take
        {
            get => m_Take;
            set => m_Take = value;
        }

        internal Take iterationBase
        {
            get => m_IterationBase;
            set => m_IterationBase = value;
        }

        /// <summary>
        /// Describes the timeline features supported by a clip.
        /// </summary>
        public ClipCaps clipCaps => ClipCaps.Extrapolation;

        /// <summary>
        ///   <para>Injects SlatePlayables into the given graph.</para>
        /// </summary>
        /// <param name="graph">The graph to inject playables into.</param>
        /// <param name="owner">The game object which initiated the build.</param>
        /// <returns>
        ///   <para>The playable injected into the graph, or the root playable if multiple playables are injected.</para>
        /// </returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            if (slateDatabase != null)
            {
                var takePlayerDirector = slateDatabase.GetComponent<PlayableDirector>();
                var directorControlPlayable = DirectorControlPlayable.Create(graph, takePlayerDirector);
                var playable = ScriptPlayable<SlatePlayable>.Create(graph);
                var slate = playable.GetBehaviour();

                slate.asset = this;
                slate.SetSlateDatabase(slateDatabase);
                slate.director = director;

                playable.AddInput(directorControlPlayable, 0, 1f);
                playable.SetPropagateSetTime(true);

                return playable;
            }
            else
            {
                return Playable.Null;
            }
        }
    }
}
