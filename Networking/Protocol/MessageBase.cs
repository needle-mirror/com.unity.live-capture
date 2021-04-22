using System;
using System.IO;
using UnityEngine;

namespace Unity.LiveCapture.Networking.Protocols
{
    /// <summary>
    /// The base class used by all messages that can be added to a <see cref="Protocol"/>.
    /// </summary>
    public abstract class MessageBase
    {
        /// <summary>
        /// The unique identifier of this message.
        /// </summary>
        public string id { get; }

        /// <summary>
        /// The network channel used to deliver this message.
        /// </summary>
        public ChannelType channel { get; }

        /// <summary>
        /// The protocol instance this message belongs to.
        /// </summary>
        public Protocol protocol { get; private set; }

        /// <summary>
        /// The code used to identify packets associated with this message.
        /// </summary>
        internal ushort code { get; private set; }

        /// <summary>
        /// Creates a new <see cref="MessageBase"/> instance.
        /// </summary>
        /// <param name="id">A unique identifier for this message.</param>
        /// <param name="channel">The network channel used to deliver this message.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="id"/> is null.</exception>
        protected MessageBase(string id, ChannelType channel)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            this.id = id;
            this.channel = channel;
        }

        /// <summary>
        /// Deserializes a <see cref="MessageBase"/> instance from a data stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        protected MessageBase(Stream stream)
        {
            id = stream.ReadString();
            channel = stream.ReadStruct<ChannelType>();
            code = stream.ReadStruct<ushort>();
        }

        /// <summary>
        /// Serializes this message to a data stream.
        /// </summary>
        /// <param name="stream">The stream to write into.</param>
        internal virtual void Serialize(Stream stream)
        {
            stream.WriteString(id);
            stream.WriteStruct(channel);
            stream.WriteStruct(code);
        }

        /// <summary>
        /// Assigns this message to a protocol.
        /// </summary>
        /// <param name="protocol">The protocol this message will belong to.</param>
        /// <param name="code">The code used to identify packets associated with this message.</param>
        internal void SetProtocol(Protocol protocol, ushort code)
        {
            this.protocol = protocol;
            this.code = code;
        }

        internal abstract MessageBase GetInverse();

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => id;
    }
}
