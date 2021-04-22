using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// An enum defining the mode the server operates in.
    /// </summary>
    public enum ServerMode : byte
    {
        /// <summary>The server is disabled.</summary>
        None = 0,
        /// <summary>The server is ready for playing recorded takes.</summary>
        Playback = 1,
        /// <summary>The server is ready for receiving live data.</summary>
        LiveStream = 2,
    }

    /// <summary>
    /// A struct that contains the state of a <see cref="CompanionAppServer"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ServerState : IEquatable<ServerState>
    {
        /// <summary>
        /// The default ServerState.
        /// </summary>
        public static readonly ServerState defaultState = new ServerState
        {
            recording = false,
            mode = ServerMode.None,
        };

        /// <summary>
        /// Is a take being recorded.
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool recording;

        /// <summary>
        /// The state the server is in.
        /// </summary>
        public ServerMode mode;

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"(Mode {mode}, Recording {recording}";
        }

        /// <summary>
        /// Determines whether the <see cref="ServerState"/> instances are equal.
        /// </summary>
        /// <param name="other">The other <see cref="ServerState"/> to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(ServerState other)
        {
            return recording == other.recording && mode == other.mode;
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is ServerState other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = recording.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)mode;
                return hashCode;
            }
        }

        /// <summary>
        /// Determines whether the two specified ServerState are equal.
        /// </summary>
        /// <param name="a">The first ServerState.</param>
        /// <param name="b">The second ServerState.</param>
        /// <returns>
        /// true if the specified ServerState are equal; otherwise, false.
        /// </returns>
        public static bool operator==(ServerState a, ServerState b) => a.Equals(b);

        /// <summary>
        /// Determines whether the two specified ServerState are different.
        /// </summary>
        /// <param name="a">The first ServerState.</param>
        /// <param name="b">The second ServerState.</param>
        /// <returns>
        /// true if the specified ServerState are different; otherwise, false.
        /// </returns>
        public static bool operator!=(ServerState a, ServerState b) => !(a == b);
    }
}
