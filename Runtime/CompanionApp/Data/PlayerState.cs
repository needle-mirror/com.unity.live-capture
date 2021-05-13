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
    struct PlayerState : IEquatable<PlayerState>
    {
        /// <summary>
        /// The default PlayerState.
        /// </summary>
        public static readonly PlayerState DefaultState = new PlayerState();

        /// <summary>
        /// Is the player playing.
        /// </summary>
        public bool Playing;

        /// <summary>
        /// The time set.
        /// </summary>
        public double Time;

        /// <summary>
        /// The duration of the current playback session.
        /// </summary>
        public double Duration;

        /// <summary>
        /// The player has a Timeline assigned.
        /// </summary>
        public bool HasTimeline;

        /// <summary>
        /// Returns a string that represents the current PlayerState.
        /// </summary>
        /// <returns>
        /// A string that represents the current PlayerState.
        /// </returns>
        public override string ToString()
        {
            return $"(Playing {Playing}, Time {Time}, Duration {Duration}, Has Timeline {HasTimeline})";
        }

        /// <inheritdoc/>
        public bool Equals(PlayerState other)
        {
            return Playing == other.Playing
                && Time == other.Time
                && Duration == other.Duration
                && HasTimeline == other.HasTimeline;
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
                var hashCode = Playing.GetHashCode();
                hashCode = (hashCode * 397) ^ Time.GetHashCode();
                hashCode = (hashCode * 397) ^ Duration.GetHashCode();
                hashCode = (hashCode * 397) ^ HasTimeline.GetHashCode();
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
