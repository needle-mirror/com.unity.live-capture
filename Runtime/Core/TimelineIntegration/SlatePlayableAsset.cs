using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Playable Asset class for <see cref="TakeRecorderTrack"/>
    /// </summary>
    class SlatePlayableAsset : PlayableAsset, ITimelineClipAsset, ISlate
    {
        const double k_Tick = 0.016666666d;
        const string k_DefaultDirectory = "Assets/Takes";

        [SerializeField]
        TimelineClip m_Clip;
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

        internal TimelineClip Clip
        {
            get => m_Clip;
            set => m_Clip = value;
        }

        internal PlayableDirector Director { get; set; }

        internal double Start
        {
            get => m_Clip.start;
        }

        public Object UnityObject => this;

        public int SceneNumber
        {
            get => m_SceneNumber;
            set => m_SceneNumber = value;
        }

        public string ShotName
        {
            get => m_Clip.displayName;
            set => m_Clip.displayName = value;
        }

        public int TakeNumber
        {
            get => m_TakeNumber;
            set => m_TakeNumber = value;
        }

        public string Description
        {
            get => m_Description;
            set => m_Description = value;
        }

        public string Directory
        {
            get => m_Directory;
            set => m_Directory = value;
        }

        public Take Take
        {
            get => m_Take;
            set => m_Take = value;
        }

        public Take IterationBase
        {
            get => m_IterationBase;
            set => m_IterationBase = value;
        }

        public double Duration => m_Clip.duration - k_Tick;

        /// <summary>
        /// Describes the timeline features supported by a clip.
        /// </summary>
        ClipCaps ITimelineClipAsset.clipCaps => ClipCaps.Extrapolation;

        /// <summary>
        /// Injects SlatePlayables into the given graph.
        /// </summary>
        /// <param name="graph">The graph to inject playables into.</param>
        /// <param name="owner">The game object which initiated the build.</param>
        /// <returns>
        ///   <para>The playable injected into the graph, or the root playable if multiple playables are injected.</para>
        /// </returns>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            if (Director != null)
            {
                var directorControlPlayable = DirectorControlPlayable.Create(graph, Director);
                var playable = ScriptPlayable<SlatePlayable>.Create(graph);
                var slatePlayable = playable.GetBehaviour();

                slatePlayable.Asset = this;
                directorControlPlayable.SetDuration(Duration);

                playable.AddInput(directorControlPlayable, 0, 1f);
                playable.SetPropagateSetTime(true);

                return playable;
            }

            return Playable.Null;
        }
    }
}
