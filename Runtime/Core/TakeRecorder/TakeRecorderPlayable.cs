using UnityEngine;
using UnityEngine.Playables;
using Unity.LiveCapture.Internal;

namespace Unity.LiveCapture
{
    class TakeRecorderPlayer : PlayableBehaviour
    {
        PlayableGraph m_Graph;
        Playable m_Playable;

        public TakeRecorder TakeRecorder { get; set; }
        public bool IsPlaying { get; private set; }
        public PlayableGraph Graph => m_Graph;

        public void Play(double time, double duration)
        {
            if (!IsPlaying)
            {
                IsPlaying = true;

                SetTimeAndDuration(time, duration);

                PlayableDirectorInternal.ResetFrameTiming();

                m_Graph.Play();
                m_Graph.Evaluate();
            }
        }

        public void Stop()
        {
            Pause();

            TakeRecorder.SetPreviewTimeInternal(0d);
        }

        public void Pause()
        {
            if (IsPlaying)
            {
                IsPlaying = false;

                m_Graph.Stop();

                TakeRecorder.OnPreviewEnded();
            }
        }

        public void SetTime(double time, double duration)
        {
            Pause();

            time = MathUtility.Clamp(time, 0d, duration);

            SetTimeAndDuration(time, duration);

            m_Graph.Evaluate();
        }

        public override void OnPlayableCreate(Playable playable)
        {
            m_Graph = playable.GetGraph();
            m_Playable = playable;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (playable.IsDone())
            {
                Stop();
            }
            else
            {
                var time = playable.GetTime();

                TakeRecorder.SetPreviewTimeInternal(time);
            }
        }

        void SetTimeAndDuration(double time, double duration)
        {
            if (duration <= 0d)
                duration = double.PositiveInfinity;

            if (duration == double.PositiveInfinity
                || time < 0d
                || time > duration)
            {
                time = 0d;
            }

            m_Playable.SetDuration(duration);
            m_Playable.SetTime(time);
        }

        public static TakeRecorderPlayer Create(TakeRecorder takeRecorder)
        {
            var graph = PlayableGraph.Create("TakeRecorderPlayer");
            var playable = ScriptPlayable<TakeRecorderPlayer>.Create(graph);
            var player = playable.GetBehaviour();
            var output = ScriptPlayableOutput.Create(graph, "TakeRecorderPlayerOutput");

            graph.SetTimeUpdateMode(GetUpdateMode());
            player.TakeRecorder = takeRecorder;
            output.SetSourcePlayable(playable);

            return player;
        }

        static DirectorUpdateMode GetUpdateMode()
        {
            return Application.isPlaying ? DirectorUpdateMode.GameTime : DirectorUpdateMode.UnscaledGameTime;
        }
    }

    static class TakeRecorderPlayerExtensions
    {
        public static bool IsValid(this TakeRecorderPlayer player)
        {
            return player != null && player.Graph.IsValid();
        }

        public static void Destroy(this TakeRecorderPlayer player)
        {
            if (player != null && player.IsValid())
            {
                player.Graph.Destroy();
            }
        }
    }
}
