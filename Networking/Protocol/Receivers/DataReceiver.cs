using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity.LiveCapture.Networking.Protocols
{
    /// <summary>
    /// The base class for message used to receive data from a remote.
    /// </summary>
    /// <typeparam name="T">The type of data to send.</typeparam>
    public abstract class DataReceiver<T> : MessageBase, IDataReceiver
    {
        /// <summary>
        /// The flags used to configure how data is sent.
        /// </summary>
        protected readonly DataOptions m_Options;

        readonly List<Action<T>> m_Handlers = new List<Action<T>>();

        /// <summary>
        /// Creates a new <see cref="DataReceiver{T}"/> instance.
        /// </summary>
        /// <param name="id">A unique identifier for this message.</param>
        /// <param name="channel">The network channel used to deliver this message.</param>
        /// <param name="options">The flags used to configure how data is sent.</param>
        protected DataReceiver(string id, ChannelType channel, DataOptions options) : base(id, channel)
        {
            m_Options = options;
        }

        /// <inheritdoc/>
        protected DataReceiver(Stream stream) : base(stream)
        {
            m_Options = stream.ReadStruct<DataOptions>();
        }

        /// <summary>
        /// Adds a callback to invoke when data is received.
        /// </summary>
        /// <param name="callback">The callback to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
        public void AddHandler(Action<T> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (!m_Handlers.Contains(callback))
            {
                m_Handlers.Add(callback);
            }
        }

        /// <summary>
        /// Removes a callback.
        /// </summary>
        /// <param name="callback">The callback to remove.</param>
        /// <returns><see langword="true"/> if the callback was removed.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="callback"/> is null.</exception>
        public bool RemoveHandler(Action<T> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            return m_Handlers.Remove(callback);
        }

        /// <inheritdoc/>
        void IDataReceiver.Receive(MemoryStream stream)
        {
            var data = OnRead(stream);

            foreach (var handler in m_Handlers)
            {
                try
                {
                    handler.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <inheritdoc />
        internal override void Serialize(Stream stream)
        {
            base.Serialize(stream);

            stream.WriteStruct(m_Options);
        }

        /// <summary>
        /// Reads data received from the network.
        /// </summary>
        /// <param name="stream">The stream containing the received data.</param>
        /// <returns>The received data instance.</returns>
        protected abstract T OnRead(MemoryStream stream);
    }
}
