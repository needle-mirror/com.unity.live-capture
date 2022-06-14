using System;
using UnityEngine;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    [Serializable]
    class PlayableDirectorContext : ITakeRecorderContext, ISlate
    {
        const string k_DefaultDirectory = "Assets/Takes";
        const string k_DefaultName = "New Shot";

        [SerializeField]
        UnityObject m_UnityObject;
        [SerializeField]
        string m_Directory = k_DefaultDirectory;
        [SerializeField]
        int m_SceneNumber = 1;
        [SerializeField]
        string m_ShotName = k_DefaultName;
        [SerializeField]
        int m_TakeNumber = 1;
        [SerializeField]
        string m_Description;
        [SerializeField]
        Take m_Take;
        [SerializeField]
        Take m_IterationBase;
        [SerializeField]
        PlayableDirector m_Director;

        public UnityObject UnityObject
        {
            get => m_UnityObject;
            set => m_UnityObject = value;
        }

        public PlayableDirector Director
        {
            get => m_Director;
            set => m_Director = value;
        }

        public string Directory
        {
            get => m_Directory;
            set => m_Directory = value;
        }

        public int SceneNumber
        {
            get => m_SceneNumber;
            set => m_SceneNumber = value;
        }

        public string ShotName
        {
            get => m_ShotName;
            set => m_ShotName = value;
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

        public double GetTimeOffset()
        {
            return 0d;
        }

        public Take Take
        {
            get => m_Take;
            set => SetTake(value, ref m_Take);
        }

        public Take IterationBase
        {
            get => m_IterationBase;
            set => SetTake(value, ref m_IterationBase);
        }

        public IExposedPropertyTable GetResolver()
        {
            return m_Director;
        }

        void SetTake(Take take, ref Take dst)
        {
            if (take == dst)
            {
                return;
            }

            if (dst != null)
            {
                m_Director.ClearSceneBindings(dst.BindingEntries);
            }

            dst = take;

            if (dst != null)
            {
                m_Director.SetSceneBindings(dst.BindingEntries);
            }
        }

        public ISlate GetSlate()
        {
            return this;
        }

        public double GetTime()
        {
            return m_Director.time;;
        }

        public void SetTime(double value)
        {
            PlayableDirectorControls.SetTime(m_Director, value);
        }

        public void Prepare(bool isRecording)
        {
            var take = isRecording ? m_IterationBase : m_Take;
            var timeline = take != null ? take.Timeline : null;

            if (m_Director.playableAsset != timeline)
            {
                m_Director.playableAsset = timeline;
                m_Director.DeferredEvaluate();

                Timeline.SetAsMasterDirector(m_Director);
                Timeline.Repaint();
            }
        }

        public double GetDuration()
        {
            return m_Director.duration;
        }

        public bool IsValid()
        {
            return m_Director != null;
        }

        /// <summary>
        /// Determines whether the <see cref="DefaultContext"/> instances are equal.
        /// </summary>
        /// <param name="other">The other <see cref="DefaultContext"/> to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>        
        
        public bool Equals(PlayableDirectorContext other)
        {
            return m_Director == other.m_Director
                && m_UnityObject == other.m_UnityObject
                && m_Directory == other.m_Directory
                && m_SceneNumber == other.m_SceneNumber
                && m_ShotName == other.m_ShotName
                && m_TakeNumber == other.m_TakeNumber
                && m_Description == other.m_Description
                && m_IterationBase == other.m_IterationBase
                && m_Take == other.m_Take;
        }

        /// <summary>
        /// Determines whether the <see cref="ITakeRecorderContext"/> instances are equal.
        /// </summary>
        /// <param name="context">The other <see cref="ITakeRecorderContext"/> to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>        
        public bool Equals(ITakeRecorderContext context)
        {
            return context is PlayableDirectorContext other && Equals(other);
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is ITakeRecorderContext other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this <see cref="DefaultContext"/>.
        /// </summary>
        /// <returns>
        /// The hash value generated for this <see cref="DefaultContext"/>.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_Director.GetHashCode();
                hashCode = (hashCode * 397) ^ m_UnityObject.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Directory.GetHashCode();
                hashCode = (hashCode * 397) ^ m_SceneNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ m_ShotName.GetHashCode();
                hashCode = (hashCode * 397) ^ m_TakeNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Description.GetHashCode();
                hashCode = (hashCode * 397) ^ m_IterationBase.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Take.GetHashCode();
                return hashCode;
            }
        }
    }
}
