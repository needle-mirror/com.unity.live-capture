using System;
using UnityEngine;

namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// The message sent by a client to the server immediately upon connecting.
    /// </summary>
    public class ClientInitialization
    {
        /// <inheritdoc cref="IClient.name"/>
        public string name;

        /// <inheritdoc cref="IClient.id"/>
        public string id;

        /// <summary>
        /// The type of client that has connected to the server.
        /// </summary>
        public string type;

        /// <inheritdoc cref="ICompanionAppClient.screenResolution"/>
        public Vector2Int screenResolution;
    }
}
