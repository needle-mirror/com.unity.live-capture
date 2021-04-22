using System;
using System.IO;
using UnityEngine.Scripting;

namespace Unity.LiveCapture.Networking.Protocols
{
    /// <summary>
    /// A message used to send blittable structs to a remote.
    /// </summary>
    /// <remarks>
    /// This message type is optimized for smaller messages that are frequently sent.
    /// </remarks>
    /// <typeparam name="T">The type of data to send. It must be a blittable struct.</typeparam>
    public sealed class BinarySender<T> : DataSender<T> where T : struct
    {
        /// <summary>
        /// Creates a new <see cref="BinarySender{T}"/> instance.
        /// </summary>
        /// <param name="id">A unique identifier for this message.</param>
        /// <param name="channel">The network channel used to deliver this message.</param>
        /// <param name="options">The flags used to configure how data is sent.</param>
        public BinarySender(string id, ChannelType channel = ChannelType.ReliableOrdered, DataOptions options = DataOptions.Default)
            : base(id, channel, options)
        {
        }

        /// <inheritdoc />
        [Preserve]
        internal BinarySender(Stream stream) : base(stream)
        {
        }

        /// <inheritdoc />
        internal override MessageBase GetInverse() => new BinaryReceiver<T>(id, channel, m_Options);

        /// <inheritdoc />
        protected override void OnWrite(MemoryStream stream, ref T data)
        {
            stream.WriteStruct(ref data);
        }

        /// <summary>
        /// Gets a <see cref="BinarySender{T}"/> from a protocol by ID.
        /// </summary>
        /// <param name="protocol">The protocol to get the message from.</param>
        /// <param name="id">The ID of the message.</param>
        /// <returns>The message instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="protocol"/>
        /// or <paramref name="id"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if there is no message with the given ID,
        /// or the message is not a <see cref="BinarySender{T}"/>.</exception>
        public static BinarySender<T> Get(Protocol protocol, string id)
        {
            if (protocol == null)
                throw new ArgumentNullException(nameof(protocol));

            return protocol.GetDataSender<T, BinarySender<T>>(id);
        }
    }
}
