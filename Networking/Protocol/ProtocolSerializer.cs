using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Unity.LiveCapture.Networking.Protocols
{
    partial class Protocol
    {
        enum MessageType : byte
        {
            NonGeneric = 0,
            Generic = 1,
        }

        static readonly Dictionary<Type, ConstructorInfo> s_ConstructorCache = new Dictionary<Type, ConstructorInfo>();
        static readonly Dictionary<Type, int> s_TypeHashCache = new Dictionary<Type, int>();

        /// <summary>
        /// Deserializes a <see cref="Protocol"/> instance from a data stream.
        /// </summary>
        /// <remarks>
        /// Deserialized protocols are read-only. Any messages that fail to deserialize will not be added to
        /// the protocol.
        /// </remarks>
        /// <param name="stream">The stream to read from.</param>
        /// <exception cref="Exception">Thrown if there is an error while deserializing the protocol.</exception>
        public Protocol(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            isReadOnly = true;

            name = stream.ReadString();
            var count = stream.ReadStruct<ushort>();

            for (var i = 0; i < count; i++)
            {
                var messageLength = stream.ReadStruct<int>();
                var messagePosition = stream.Position;

                try
                {
                    // read the type of the message instance to create
                    var type = stream.ReadStruct<MessageType>();
                    var messageTypeStr = stream.ReadString();

                    if (!TryDeserializeType(messageTypeStr, out var messageType))
                    {
                        throw new Exception($"Message type \"{messageTypeStr}\" does not exist!");
                    }
                    if (!typeof(MessageBase).IsAssignableFrom(messageType))
                    {
                        throw new Exception($"Type \"{messageType.FullName}\" does not inherit from {nameof(MessageBase)}!");
                    }

                    // if the message transports data we must read the type information of the data
                    if (type == MessageType.Generic)
                    {
                        var dataTypeStr = stream.ReadString();

                        if (!TryDeserializeType(dataTypeStr, out var dataType))
                        {
                            throw new Exception($"Data type \"{dataTypeStr}\" does not exist!");
                        }

                        var remoteTypeHash = stream.ReadStruct<int>();
                        var localTypeHash = HashType(dataType);

                        if (remoteTypeHash != localTypeHash)
                        {
                            throw new Exception($"The hash for data type \"{dataType.FullName}\" does not match the received hash. There is likely a version miss-match.");
                        }

                        messageType = messageType.MakeGenericType(dataType);
                    }

                    // create an instance of the message type using the stream constructor
                    if (!s_ConstructorCache.TryGetValue(messageType, out var constructor))
                    {
                        constructor = messageType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Stream) }, null);
                        s_ConstructorCache.Add(messageType, constructor);
                    }

                    if (constructor == null)
                    {
                        throw new Exception($"Message type \"{messageType.FullName}\" must define a constructor with signature \"internal {messageType.Name}(Stream stream)\".");
                    }

                    var message = constructor.Invoke(new object[] { stream }) as MessageBase;

                    AddInternal(message, message.code);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to deserialize message: {e}");
                }

                // go to the start of the next message
                stream.Position = messagePosition + messageLength;
            }
        }

        /// <summary>
        /// Serializes this protocol.
        /// </summary>
        /// <param name="stream">The stream to write into.</param>
        public void Serialize(Stream stream)
        {
            var count = (ushort)m_Messages.Count;

            stream.WriteString(name);
            stream.WriteStruct(count);

            // Write each message prefixed by the length of the serialized message.
            // This allows us to skip over messages that fail to deserialize.
            var tempStream = new MemoryStream();

            foreach (var message in m_Messages)
            {
                tempStream.SetLength(0);

                var type = message.GetType();

                // For messages that send data we need to send the data type and message type.
                // Since these must inherit from DataReceiver/DataSender we know there can only
                // be one generic type parameter.
                if (type.IsGenericType)
                {
                    var baseType = type.GetGenericTypeDefinition();
                    var dataType = type.GetGenericArguments()[0];

                    tempStream.WriteStruct(MessageType.Generic);
                    tempStream.WriteString(SerializeType(baseType));
                    tempStream.WriteString(SerializeType(dataType));
                    tempStream.WriteStruct(HashType(dataType));
                }
                else
                {
                    tempStream.WriteStruct(MessageType.NonGeneric);
                    tempStream.WriteString(SerializeType(type));
                }

                message.Serialize(tempStream);

                stream.WriteStruct((int)tempStream.Length);
                stream.Write(tempStream.GetBuffer(), 0, (int)tempStream.Length);
            }
        }

        static string SerializeType(Type type)
        {
            var typeName = type.FullName;
            var assemblyName = type.Assembly.GetName().Name;

            return $"{typeName},{assemblyName}";
        }

        static bool TryDeserializeType(string serializedType, out Type type)
        {
            try
            {
                type = Type.GetType(serializedType);
                return type != null;
            }
            catch
            {
                type = null;
                return false;
            }
        }

        /// <summary>
        /// Generates a hash for a type that can be used to compare if a type has the same definition
        /// in the local runtime as that same type in a remote runtime.
        /// </summary>
        /// <remarks>
        /// In general, the hash takes into account type name, field names/types, size, struct layout
        /// and marshalling attributes. The hash does not account for all possible aspects of the type,
        /// but these aspects should be sufficient to catch most errors.
        /// </remarks>
        /// <param name="type">The type to hash.</param>
        /// <returns>The generated hash code.</returns>
        static int HashType(Type type)
        {
            if (s_TypeHashCache.TryGetValue(type, out var hash))
                return hash;

            hash = type.FullName.GetHashCode();

            if (type.IsPrimitive)
            {
                // just the type name is sufficient, since primitives are always the same in any runtime
            }
            else if (type.IsEnum)
            {
                // for enums we can check that their size and defined values match
                HashCombine(ref hash, HashType(type.GetEnumUnderlyingType()));

                foreach (var value in Enum.GetValues(type))
                {
                    HashCombine(ref hash, value.ToString().GetHashCode());
                }
            }
            else if (type.IsArray && type.HasElementType)
            {
                // for arrays we can just check that the element type matches
                HashCombine(ref hash, HashType(type.GetElementType()));
            }
            else if (type.IsValueType || type.IsClass)
            {
                // hash the layout specification
                var layout = type.StructLayoutAttribute;

                if (type.IsValueType && layout != null)
                {
                    HashCombine(ref hash, (int)layout.Value);
                    HashCombine(ref hash, layout.Pack);
                    HashCombine(ref hash, (int)layout.CharSet);

                    // the default layout size field differs per platform, so we get it indirectly
                    HashCombine(ref hash, Marshal.SizeOf(type));
                }

                // get the fields sorted by declaration order
                var fields = type
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .OrderBy(field => field.MetadataToken);

                foreach (var field in fields)
                {
                    // check that the field names and types match
                    HashCombine(ref hash, field.Name.GetHashCode());
                    HashCombine(ref hash, HashType(field.FieldType));
                }

                // make sure to include the fields in any base types
                if (type.IsClass)
                {
                    var baseType = type.BaseType;

                    while (baseType != null)
                    {
                        HashCombine(ref hash, HashType(baseType));

                        baseType = baseType.BaseType;
                    }
                }
            }

            s_TypeHashCache.Add(type, hash);
            return hash;
        }

        static void HashCombine(ref int a, int b)
        {
            a = RotateLeft(a, 15) ^ b;
        }

        static int RotateLeft(int value, int count)
        {
            return (int)(((uint)value << count) | ((uint)value >> (32 - count)));
        }
    }
}
