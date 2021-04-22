using System;
using System.Runtime.InteropServices;

namespace Unity.LiveCapture.Networking
{
    /// <summary>
    /// Stores the marshalled size of a struct.
    /// </summary>
    /// <typeparam name="T">The type of struct to get the size of.</typeparam>
    static class SizeOfCache<T> where T : struct
    {
        /// <summary>
        /// The size of the struct in bytes.
        /// </summary>
        public static int size { get; }

        static SizeOfCache()
        {
            var t = typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T);
            size = Marshal.SizeOf(t);
        }
    }
}
