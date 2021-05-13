using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    class TakeRecorderPlayable : PlayableBehaviour
    {
        Playable m_Playable;

        public TakeRecorder TakeRecorder { get; set; }
        public bool Playing { get; private set; }

        public void Play()
        {
            var slate = TakeRecorder.GetActiveSlate();

            if (!Playing && slate != null)
            {
                Playing = true;

                SetDuration(m_Playable);

                var duration = slate.Duration;

                if (duration == 0)
                {
                    m_Playable.SetTime(0d);
                }
                else
                {
                    m_Playable.SetTime(TakeRecorder.GetPreviewTime());
                }

                var graph = m_Playable.GetGraph();

                graph.Play();
                graph.Evaluate();
            }
        }

        public void Stop()
        {
            if (Playing)
            {
                Playing = false;

                var graph = m_Playable.GetGraph();

                graph.Stop();

                TakeRecorder.OnPreviewEnded();
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
            if (!Playing)
            {
                SetDuration(playable);
            }

            var time = playable.GetTime();

            if (playable.IsDone())
            {
                TakeRecorder.SetPreviewTimeInternal(0d);
                Stop();
            }
            else
            {
                TakeRecorder.SetPreviewTimeInternal(time);
            }
        }

        void SetDuration(Playable playable)
        {
            var slate = TakeRecorder.GetActiveSlate();
            var duration = 0d;

            if (slate != null)
            {
                duration = slate.Duration;
            }

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
