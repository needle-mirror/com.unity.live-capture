using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    class TakeRecorderPlayable : PlayableBehaviour
    {
        Playable m_Playable;
        public TakeRecorder takeRecorder { get; set; }
        public bool playing { get; private set; }

        public void Play()
        {
            if (!playing)
            {
                playing = true;

                SetDuration(m_Playable);

                var slate = takeRecorder.slate;
                var duration = slate.duration;

                if (duration == 0)
                {
                    m_Playable.SetTime(0d);
                }
                else
                {
                    m_Playable.SetTime(slate.time);
                }

                var graph = m_Playable.GetGraph();

                graph.Play();
                graph.Evaluate();
            }
        }

        public void Stop()
        {
            if (playing)
            {
                playing = false;

                var graph = m_Playable.GetGraph();

                graph.Stop();

                takeRecorder.OnPreviewEnded();
            }
        }

        public void SetTime(double time)
        {
            Stop();

            m_Playable.SetTime(time);
            m_Playable.GetGraph().Evaluate();
        }

        public override void OnPlayableCreate(Playable playable)
        {
            m_Playable = playable;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            if (!playing)
            {
                SetDuration(playable);
            }

            var time = playable.GetTime();
            var slate = takeRecorder.slate;

            if (playable.IsDone())
            {
                slate.time = 0d;
                Stop();
            }
            else
            {
                slate.time = time;
            }
        }

        void SetDuration(Playable playable)
        {
            var slate = takeRecorder.slate;
            var duration = slate.duration;

            if (duration == 0)
            {
                playable.SetDuration(double.PositiveInfinity);
            }
            else
            {
                playable.SetDuration(duration);
            }
        }
    }
}
