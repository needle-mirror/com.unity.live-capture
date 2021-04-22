using System;
using System.Linq;
using System.Net;

namespace Unity.LiveCapture.Networking.Discovery
{
    /// <summary>
    /// A struct containing information about a discovered server.
    /// </summary>
    public struct DiscoveryInfo : IEquatable<DiscoveryInfo>
    {
        /// <summary>
        /// The properties of the discovered server.
        /// </summary>
        public ServerData serverInfo { get; }

        /// <summary>
        /// The end points on which the server is accepting new connections.
        /// </summary>
        public IPEndPoint[] endPoints { get; }

        /// <summary>
        /// Creates a new <see cref="DiscoveryInfo"/> instance.
        /// </summary>
        /// <param name="serverInfo">The properties of the discovered server.</param>
        /// <param name="endPoints">The end points on which the server is accepting new connections.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="endPoints"/> is null.</exception>
        public DiscoveryInfo(ServerData serverInfo, IPEndPoint[] endPoints)
        {
            this.serverInfo = serverInfo;
            this.endPoints = endPoints ?? throw new ArgumentNullException(nameof(endPoints));
        }

        /// <summary>
        /// Determines whether the <see cref="DiscoveryInfo"/> instances are equal.
        /// </summary>
        /// <param name="other">The other <see cref="DiscoveryInfo"/> to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public bool Equals(DiscoveryInfo other)
        {
            if (!serverInfo.Equals(other.serverInfo))
                return false;
            if (endPoints.Length != other.endPoints.Length)
                return false;

            for (var i = 0; i < endPoints.Length; i++)
            {
                if (!endPoints[i].Equals(other.endPoints[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether two object instances are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            return obj is DiscoveryInfo other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (serverInfo.GetHashCode() * 397) ^ endPoints.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{{\n{serverInfo},\nEnd Points: {string.Join(", ", endPoints.Select(e => e.ToString()))}\n}}";
        }
    }
}
