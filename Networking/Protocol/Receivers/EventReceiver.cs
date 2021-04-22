using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.LiveCapture.Networking.Protocols
{
    /// <summary>
    /// A message used to receive events from a remote.
    /// </summary>
    public sealed class EventReceiver : MessageBase, IDataReceiver
    {
        readonly List<Action> m_Handlers = new List<Action>();

        /// <summary>
        /// Creates a new <see cref="EventSender"/> instance.
        /// </summary>
        /// <param name="id">A unique identifier for this message.</param>
        public EventReceiver(string id) : base(id, ChannelType.ReliableOrdered)
        {
        }

        [Preserve]
        internal EventReceiver(Stream stream) : base(stream)
        {
        }

        /// <summary>
        /// Adds a callback to invoke when this event is raised.
        /// </summary>
        /// <param name="callback">The callback to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
        public void AddHandler(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!m_Handlers.Contains(callback))
            {
                m_Handlers.Add(callback);
            }
        }

        /// <summary>
        /// Removes a callback from this event.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        /// <returns><see langword="true"/> if the callback was removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
        public bool RemoveHandler(Action callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return m_Handlers.Remove(callback);
        }

        /// <inheritdoc />
        void IDataReceiver.Receive(MemoryStream stream)
        {
            foreach (var handler in m_Handlers)
            {
                try
                {
                    handler.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <inheritdoc />
        internal override MessageBase GetInverse() => new EventSender(id);

        /// <summary>
        /// Gets a <see cref="EventReceiver"/> from a protocol by ID.
        /// </summary>
        /// <param name="protocol">The protocol to get the message from.</param>
        /// <param name="id">The ID of the message.</param>
        /// <returns>The message instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="protocol"/>
        /// or <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if there is no message with the given ID,
        /// or the message is not a <see cref="EventReceiver"/>.</exception>
        public static EventReceiver Get(Protocol protocol, string id)
        {
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));

            return protocol.GetEventReceiver(id);
        }
    }
}
