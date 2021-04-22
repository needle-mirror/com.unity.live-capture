using System;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.LiveCapture.Networking.Protocols
{
    /// <summary>
    /// A message used to send events to a remote.
    /// </summary>
    public sealed class EventSender : MessageBase
    {
        /// <summary>
        /// Creates a new <see cref="EventSender"/> instance.
        /// </summary>
        /// <param name="id">A unique identifier for this message.</param>
        public EventSender(string id) : base(id, ChannelType.ReliableOrdered)
        {
        }

        [Preserve]
        internal EventSender(Stream stream) : base(stream)
        {
        }

        /// <summary>
        /// Invokes this event.
        /// </summary>
        public void Send()
        {
            try
            {
                protocol.SendMessage(this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <inheritdoc />
        internal override MessageBase GetInverse() => new EventReceiver(id);

        /// <summary>
        /// Gets a <see cref="EventSender"/> from a protocol by ID.
        /// </summary>
        /// <param name="protocol">The protocol to get the message from.</param>
        /// <param name="id">The ID of the message.</param>
        /// <returns>The message instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="protocol"/>
        /// or <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if there is no message with the given ID,
        /// or the message is not a <see cref="EventSender"/>.</exception>
        public static EventSender Get(Protocol protocol, string id)
        {
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));

            return protocol.GetEventSender(id);
        }
    }
}
