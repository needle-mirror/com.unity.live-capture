using System;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    static class Callbacks
    {
        public static event Action<ISlate, PlayableDirector> SeekOccurred = delegate {};

        internal static void InvokeSeekOccurred(ISlate slate, PlayableDirector director)
        {
            SeekOccurred.Invoke(slate, director);
        }
    }
}
