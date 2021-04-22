using System;
using System.Collections.Generic;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// A class that keeps track of which devices any connected clients are currently assigned to.
    /// </summary>
    static class ClientMappingDatabase
    {
        static readonly Dictionary<ICompanionAppClient, ICompanionAppDevice> s_ClientToDevice = new Dictionary<ICompanionAppClient, ICompanionAppDevice>();

        /// <summary>
        /// Adds a device mapping for a client.
        /// </summary>
        /// <param name="client">A client that is assigned to a device.</param>
        /// <param name="device">The device the client is assigned to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="device"/> is null.</exception>
        public static void RegisterClientAssociation(ICompanionAppClient client, ICompanionAppDevice device)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            s_ClientToDevice[client] = device;
        }

        /// <summary>
        /// Clears the device mapping for a client.
        /// </summary>
        /// <param name="client">The client to remove the mapping for.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is null.</exception>
        public static void DeregisterClientAssociation(ICompanionAppClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            s_ClientToDevice.Remove(client);
        }

        /// <summary>
        /// Gets the device mapped to by a client.
        /// </summary>
        /// <param name="client">The client to get the device for.</param>
        /// <param name="device">Returns the mapped device, if there is one for this client.</param>
        /// <returns>True if there is a mapping for this client; false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> is null.</exception>
        public static bool TryGetDevice(ICompanionAppClient client, out ICompanionAppDevice device)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            return s_ClientToDevice.TryGetValue(client, out device);
        }
    }
}
