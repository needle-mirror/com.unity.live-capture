using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// Stores the state of a media player.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PlayerState : IEquatable<PlayerState>
    {
        /// <summary>
        /// The default PlayerState.
        /// </summary>
        public static readonly PlayerState defaultState = new PlayerState();

        /// <summary>
        /// Is the player playing.
        /// </summary>
        public bool playing;

        /// <summary>
        /// The time set.
        /// </summary>
        public double time;

        /// <summary>
        /// The duration of the current playback session.
        /// </summary>
        public double duration;

        /// <summary>
        /// The player has a Timeline assigned.
        /// </summary>
        public bool hasTimeline;

        /// <summary>
        /// Returns a string that represents the current PlayerState.
        /// </summary>
        /// <returns>
        /// A string that represents the current PlayerState.
        /// </returns>
        public override string ToString()
        {
            return $"(Playing {playing}, Time {time}, Duration {duration}, Has Timeline {hasTimeline})";
        }

        /// <inheritdoc/>
        public bool Equals(PlayerState other)
        {
            return playing == other.playing
                && time == other.time
                && duration == other.duration
                && hasTimeline == other.hasTimeline;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current PlayerState.
        /// </summary>
        /// <param name="obj">The object to compare with the current PlayerState.</param>
        /// <returns>
        /// true if the specified object is equal to the current PlayerState; otherwise, false.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is PlayerState other && Equals(other);
        }

        /// <summary>
        /// Gets the hash code for the PlayerState.
        /// </summary>
        /// <returns>
        /// The hash value generated for this PlayerState.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = playing.GetHashCode();
                hashCode = (hashCode * 397) ^ time.GetHashCode();
                hashCode = (hashCode * 397) ^ duration.GetHashCode();
                hashCode = (hashCode * 397) ^ hasTimeline.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the two specified PlayerState are equal.
        /// </summary>
        /// <param name="left">The first PlayerState.</param>
        /// <param name="right">The second PlayerState.</param>
        /// <returns>
        /// true if the specified PlayerState are equal; otherwise, false.
        /// </returns>
        public static bool operator==(PlayerState left, PlayerState right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether the two specified PlayerState are different.
        /// </summary>
        /// <param name="left">The first PlayerState.</param>
        /// <param name="right">The second PlayerState.</param>
        /// <returns>
        /// true if the specified PlayerState are different; otherwise, false.
        /// </returns>
        public static bool operator!=(PlayerState left, PlayerState right)
        {
            return !left.Equals(right);
        }
    }
}
