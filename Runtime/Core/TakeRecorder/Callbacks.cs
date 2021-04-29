using System;

namespace Unity.LiveCapture
{
    static class Callbacks
    {
        public static event Action SeekOccurred = delegate {};

        internal static void InvokeSeekOccurred()
        {
            SeekOccurred.Invoke();
        }
    }
}
