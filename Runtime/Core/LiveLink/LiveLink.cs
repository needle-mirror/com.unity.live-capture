using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.Animations;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Live link is used by a device to set the properties of an actor.
    /// </summary>
    /// <remarks>
    /// The playable API is used to create a playable that uses an animation job to sets an actor's properties,
    /// which it injects as an output on a playable graph. This allows using a timeline to drive some or all of the
    /// actor's properties without needing to modify the timeline asset.
    /// </remarks>
    /// <typeparam name="T">The animation job used to apply the animated properties.</typeparam>
    [Serializable]
    public abstract class LiveLink<T> : ILiveLink where T : struct, IAnimationJob
    {
        static readonly ushort k_AnimationOutputPriority = 2000;

        [SerializeField, HideInInspector]
        bool m_Active;
        Animator m_Animator;
        PlayableGraph m_Graph;
        AnimationPlayableOutput m_Output;
        AnimationScriptPlayable m_Playable;

        /// <inheritdoc/>
        public bool IsValid()
        {
            return m_Animator != null &&
                m_Graph.IsValid() &&
                m_Playable.IsValid() &&
                m_Output.IsOutputValid();
        }

        /// <inheritdoc/>
        public bool IsActive()
        {
            return m_Active;
        }

        /// <inheritdoc/>
        public void SetActive(bool value)
        {
            if (m_Active != value)
            {
                m_Active = value;

                RebuildOutput();
            }
        }

        /// <inheritdoc/>
        public Animator GetAnimator()
        {
            return m_Animator;
        }

        /// <inheritdoc/>
        public void SetAnimator(Animator animator)
        {
            if (m_Animator != animator)
            {
                m_Animator = animator;

                RebuildPlayable();
                RebuildOutput();
            }
        }

        /// <inheritdoc/>
        public void Update()
        {
            UpdateJobData();
        }

        /// <inheritdoc/>
        public void Build(PlayableGraph graph)
        {
            DestroyOutput();

            m_Graph = graph;

            RebuildPlayable();
            RebuildOutput();
        }

        void RebuildPlayable()
        {
            if (m_Playable.IsValid())
            {
                m_Playable.Destroy();
            }

            if (m_Graph.IsValid() && m_Animator != null)
            {
                m_Playable = AnimationScriptPlayable.Create(m_Graph, CreateAnimationJob(m_Animator));
                m_Playable.SetDuration(0d);
            }
        }

        void RebuildOutput()
        {
            DestroyOutput();
            BuildOutput();
        }

        void BuildOutput()
        {
            if (m_Active)
            {
                CreateOutput();
            }

            UpdateJobData();
        }

        void UpdateJobData()
        {
            if (IsValid())
            {
                var data = m_Playable.GetJobData<T>();
                data = Update(data);
                m_Playable.SetJobData(data);
            }
        }

        void CreateOutput()
        {
            if (m_Graph.IsValid() && m_Playable.IsValid() && m_Animator != null)
            {
                m_Output = AnimationPlayableOutput.Create(m_Graph, "Live Link Output", m_Animator);
                m_Output.SetAnimationStreamSource(AnimationStreamSource.PreviousInputs);
                m_Output.SetSortingOrder(k_AnimationOutputPriority);
                m_Output.SetSourcePlayable(m_Playable);
            }
        }

        void DestroyOutput()
        {
            if (m_Output.IsOutputValid() && m_Graph.IsValid())
            {
                m_Graph.DestroyOutput(m_Output);
                m_Output = AnimationPlayableOutput.Null;
            }
        }

        /// <summary>
        /// Called to create the animation job used to output a take to the actor via the animator.
        /// </summary>
        /// <param name="animator">The animator the job must output to.</param>
        /// <returns>The new job.</returns>
        protected abstract T CreateAnimationJob(Animator animator);

        /// <summary>
        /// Called to update the contents of the animation job from the current take evaluation.
        /// </summary>
        /// <param name="jobData">The animation job to update.</param>
        /// <returns>The updated job.</returns>
        protected abstract T Update(T jobData);
    }
}
