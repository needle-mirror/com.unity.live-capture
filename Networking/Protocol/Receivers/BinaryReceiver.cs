using System;
using System.IO;
using UnityEngine.Scripting;

namespace Unity.LiveCapture.Networking.Protocols
{
    /// <summary>
    /// A message used to receive blittable structs from a remote.
    /// </summary>
    /// <remarks>
    /// This message type is optimized for smaller messages that are frequently sent.
    /// </remarks>
    /// <typeparam name="T">The type of data to receive. It must be a blittable struct.</typeparam>
    public sealed class BinaryReceiver<T> : DataReceiver<T> where T : struct
    {
        /// <summary>
        /// Creates a new <see cref="BinaryReceiver{T}"/> instance.
        /// </summary>
        /// <param name="id">A unique identifier for this message.</param>
        /// <param name="channel">The network channel used to deliver this message.</param>
        /// <param name="options">The flags used to configure how data is sent.</param>
        public BinaryReceiver(string id, ChannelType channel = ChannelType.ReliableOrdered, DataOptions options = DataOptions.Default)
            : base(id, channel, options)
        {
        }

        /// <inheritdoc />
        [Preserve]
        internal BinaryReceiver(Stream stream) : base(stream)
        {
        }

        /// <inheritdoc />
        internal override MessageBase GetInverse() => new BinarySender<T>(id, channel, m_Options);

        /// <inheritdoc />
        protected override T OnRead(MemoryStream stream)
        {
            return stream.ReadStruct<T>();
        }

        /// <summary>
        /// Gets a <see cref="BinaryReceiver{T}"/> from a protocol by ID.
        /// </summary>
        /// <param name="protocol">The protocol to get the message from.</param>
        /// <param name="id">The ID of the message.</param>
        /// <returns>The message instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="protocol"/>
        /// or <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if there is no message with the given ID,
        /// or the message is not a <see cref="BinaryReceiver{T}"/>.</exception>
        public static BinaryReceiver<T> Get(Protocol protocol, string id)
        {
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));

            return protocol.GetDataReceiver<T, BinaryReceiver<T>>(id);
        }
    }
}
