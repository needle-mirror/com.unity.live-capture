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
        /// In that case, the new track becomes an override of the existing one.
        /// You can optionally pass in a <paramref name="startTime"/>, which will be used for alignment by
        /// <see cref="Unity.LiveCapture.TakeBuilder.AlignTracksByStartTimes"/>. If <paramref name="startTime"/> is <c>null</c>, the clip will by
        /// aligned to the start of the track.
        /// </remarks>
        /// <param name="name">The name of the track.</param>
        /// <param name="animator">The target animator component to bind.</param>
        /// <param name="animationClip">The animation clip to set into the new track.</param>
        /// <param name="metadata">The metadata associated with the new track (optional).</param>
        /// <param name="startTime">The time (expressed in seconds) of first sample of animation clip (optional).</param>
        void CreateAnimationTrack(string name, Animator animator, AnimationClip animationClip, ITrackMetadata metadata = null, double? startTime = null);
    }
}
