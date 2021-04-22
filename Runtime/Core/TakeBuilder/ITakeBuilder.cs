using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Represents a <see cref="Take"/> asset creator.
    /// </summary>
    public interface ITakeBuilder
    {
        /// <summary>
        /// Creates an animation track.
        /// </summary>
        /// <remarks>
        /// This method checks if another animation track is using the same binding.
        /// In that case, the new track becomes an onverride of the existing one.
        /// </remarks>
        /// <param name="name">The name of the track.</param>
        /// <param name="animator">The target animator component to bind.</param>
        /// <param name="animationClip">The animation clip to set into the new track.</param>
        void CreateAnimationTrack(string name, Animator animator, AnimationClip animationClip);

        /// <summary>
        /// Creates an animation track.
        /// </summary>
        /// <remarks>
        /// This method checks if another animation track is using the same binding.
        /// In that case, the new track becomes an onverride of the existing one.
        /// </remarks>
        /// <param name="name">The name of the track.</param>
        /// <param name="animator">The target animator component to bind.</param>
        /// <param name="animationClip">The animation clip to set into the new track.</param>
        /// <param name="metadata">The metadata assiciated with the new track.</param>
        void CreateAnimationTrack(string name, Animator animator, AnimationClip animationClip, ITrackMetadata metadata);
    }
}
