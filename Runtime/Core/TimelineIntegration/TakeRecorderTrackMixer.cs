using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    class TakeRecorderTrackMixer : PlayableBehaviour, ISlatePlayer
    {
        Playable m_Playable;

        public PlayableDirector Director { get; set; }
        public TakeRecorder TakeRecorder { get; set; }

        public int GetSlateCount()
        {
            return m_Playable.GetInputCount();
        }

        public ISlate GetSlate(int index)
        {
            if (index < 0 || index >= GetSlateCount())
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var playable = (ScriptPlayable<SlatePlayable>)m_Playable.GetInput(index);
            var slatePlayable = playable.GetBehaviour();

            return slatePlayable.Asset;
        }

        Playable GetActivePlayable()
        {
            var active = default(Playable);

            for (var i = 0; i < GetSlateCount(); ++i)
            {
                if (m_Playable.GetInputWeight(i) == 1f)
                {
                    active = m_Playable.GetInput(i);
                }
            }

            return active;
        }

        public ISlate GetActiveSlate()
        {
            var slatePlayableAsset = default(SlatePlayableAsset);
            var playable = (ScriptPlayable<SlatePlayable>)GetActivePlayable();

            if (playable.IsValid())
            {
                slatePlayableAsset = playable.GetBehaviour().Asset;
            }

            return slatePlayableAsset;
        }

        public double GetTime()
        {
            var playable = GetActivePlayable();

            if (playable.IsValid())
            {
                return playable.GetTime();
            }

            return Director.time;
        }

        public void SetTime(double value)
        {
            SetTime(GetActiveSlate(), value);
        }

        public void SetTime(ISlate slate, double time)
        {
            var start = 0d;

            if (Contains(slate))
            {
                var slatePlayableAsset = slate as SlatePlayableAsset;
                var duration = slatePlayableAsset.Duration;

                start = slatePlayableAsset.Start;

                if (time < 0d)
                {
                    time = 0d;
                }
                else if (time > duration)
                {
                    time = duration;
                }
            }

            if (Director != null)
            {
                Director.Pause();
                Director.time = start + time;
                Director.DeferredEvaluate();

                Callbacks.InvokeSeekOccurred(slate, Director);
            }
        }

        bool Contains(ISlate slate)
        {
            if (slate == null)
            {
                return false;
            }

            for (var i = 0; i < GetSlateCount(); ++i)
            {
                if (slate == GetSlate(i))
                {
                    return true;
                }
            }

            return false;
        }

        public override void OnPlayableCreate(Playable playable)
        {
            m_Playable = playable;
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            TakeRecorder.RemoveSlatePlayer(this);
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            TakeRecorder.SetSlatePlayer(this);
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            TakeRecorder.Prepare(this);
        }
    }
}
