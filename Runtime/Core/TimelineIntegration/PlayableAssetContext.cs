using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityObject = UnityEngine.Object;

namespace Unity.LiveCapture
{
    class PlayableAssetContext : ITakeRecorderContext
    {
        const double k_Tick = 0.016666666d;

        TrackAsset m_Track;
        UnityObject m_Asset;
        TimelineHierarchyContext m_HierarchyContext;

        public PlayableAssetContext(TimelineClip clip, TimelineHierarchyContext hierarchyContext)
        {
            m_Track = clip.GetParentTrack();
            m_Asset = clip.asset;
            m_HierarchyContext = hierarchyContext;
        }

        public TimelineClip GetClip()
        {
            TryGetClip(out var clip);

            return clip;
        }

        public TimelineHierarchyContext GetHierarchyContext()
        {
            return m_HierarchyContext;
        }

        public IExposedPropertyTable GetResolver()
        {
            return GetDirector();
        }

        public PlayableDirector GetDirector()
        {
            return m_HierarchyContext.GetLeaf().Director;
        }

        public ISlate GetSlate()
        {
            return m_Asset as ISlate;
        }

        public double GetTimeOffset()
        {
            if (TryGetClip(out var clip))
            {
                return clip.clipIn;
            }

            return 0d;
        }

        public double GetTime()
        {
            if (TryGetClip(out var clip))
            {
                return m_HierarchyContext.GetLocalTime() - clip.start;
            }

            return 0d;
        }

        public void SetTime(double value)
        {
            if (TryGetClip(out var clip))
            {
                value = MathUtility.Clamp(value, 0, GetDuration());
                
                m_HierarchyContext.SetLocalTime(value + clip.start);
            }
        }

        public double GetDuration()
        {
            if (TryGetClip(out var clip))
            {
                return clip.duration - k_Tick;
            }

            return 0d;
        }

        public void Prepare(bool isRecording)
        {
            if (IsValid())
            {
                var director = GetDirector();

                director.RebuildGraph();
                director.Evaluate();

                // Prepare might be called after DirectorUpdateAnimationEnd. Calling DeferredEvaluate
                // forces the Editor to do one extra update loop evaluation before the end of the frame.
                director.DeferredEvaluate();
            }
        }

        public bool IsValid()
        {
            return m_HierarchyContext.IsValid();
        }

        public override string ToString()
        {
            return m_HierarchyContext.ToString();
        }

        /// <summary>
        /// Determines whether the <see cref="IPlayableAssetContext"/> instances are equal.
        /// </summary>
        /// <param name="other">The other <see cref="IPlayableAssetContext"/> to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>        
        public bool Equals(PlayableAssetContext other)
        {
            if (other == null)
                return false;

            return m_Track == other.m_Track
                && m_Asset == other.m_Asset
                && m_HierarchyContext.Equals(other.m_HierarchyContext);
        }

        /// <summary>
        /// Determines whether the <see cref="ITakeRecorderContext"/> instances are equal.
        /// </summary>
        /// <param name="context">The other <see cref="ITakeRecorderContext"/> to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>        
        public bool Equals(ITakeRecorderContext context)
        {
            return context is PlayableAssetContext other && Equals(other);
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is PlayableAssetContext other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for this <see cref="PlayableAssetContext"/>.
        /// </summary>
        /// <returns>
        /// The hash value generated for this <see cref="PlayableAssetContext"/>.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_HierarchyContext.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Track.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Asset.GetHashCode();
                return hashCode;
            }
        }

        bool TryGetClip(out TimelineClip clip)
        {
            clip = null;

            if (m_Track != null && m_Asset != null)
            {
                clip = m_Track.GetClips().FirstOrDefault(c => c.asset == m_Asset);
            }

            return clip != null;
        }
    }
}
