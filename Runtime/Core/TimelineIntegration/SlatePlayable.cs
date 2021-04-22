using UnityEngine;
using UnityEngine.Playables;
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    class SlatePlayable : PlayableBehaviour, ISlate
    {
        const double k_Tick = 0.016666666d;

        Playable m_Playable;
        SlateDatabase m_SlateDatabase;

        internal SlatePlayableAsset asset { get; set; }

        internal PlayableDirector director { get; set; }

        internal double start { get => asset.clip.start; }

        public UnityObject unityObject => asset;

        public string directory
        {
            get => asset.directory;
            set => asset.directory = value;
        }

        public int sceneNumber
        {
            get => asset.sceneNumber;
            set => asset.sceneNumber = value;
        }

        public string shotName
        {
            get => asset.clip.displayName;
            set => asset.clip.displayName = value;
        }

        public int takeNumber
        {
            get => asset.takeNumber;
            set => asset.takeNumber = value;
        }

        public string description
        {
            get => asset.description;
            set => asset.description = value;
        }

        public Take take
        {
            get => asset.take;
            set => asset.take = value;
        }

        public Take iterationBase
        {
            get => asset.iterationBase;
            set => asset.iterationBase = value;
        }

        public double time
        {
            get => m_Playable.GetTime();
            set
            {
                if (value < 0d)
                {
                    value = 0d;
                }
                else if (value > duration)
                {
                    value = duration;
                }

                if (director != null)
                {
                    director.Pause();
                    director.time = start + value;
                    director.DeferredEvaluate();

                    Callbacks.InvokeSeekOccurred(this, director);
                }
            }
        }

        // Avoiding reaching the end of the clip to avoid jumping into the neighbouring one.
        public double duration => asset.clip.duration - k_Tick;

        internal void SetSlateDatabase(SlateDatabase slateDatabase)
        {
            Debug.Assert(m_SlateDatabase == null);

            m_SlateDatabase = slateDatabase;
            m_SlateDatabase.AddSlate(this);
        }

        bool IsTimeInRange()
        {
            var localTime = director.time - start;

            return localTime >= 0d && localTime <= duration;
        }

        public override void OnPlayableCreate(Playable playable)
        {
            m_Playable = playable;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            m_SlateDatabase.RemoveSlate(this);
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            playable.GetInput(0).SetDuration(playable.GetDuration());

            // DirectorControlPlayable expects the correct Director's duration OnBehaviourPlay.
            // By setting the slate here, we set the timeline asset immediately before
            // DirectorControlPlayable's OnBehaviourPlay is processed.
            m_SlateDatabase.slate = this;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            m_SlateDatabase.slate = this;
        }
    }
}
