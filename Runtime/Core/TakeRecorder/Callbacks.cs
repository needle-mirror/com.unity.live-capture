using System;
using UnityEngine.Playables;

namespace Unity.LiveCapture
{
    static class Callbacks
    {
        public static event Action<ISlate, PlayableDirector> seekOccurred = delegate {};

        internal static void InvokeSeekOccurred(ISlate slate, PlayableDirector director)
        {
            seekOccurred.Invoke(slate, director);
        }
    }
}
