using System.Collections.ObjectModel;

namespace Unity.LiveCapture
{
    interface ITakeRecorderContextProvider
    {
        ReadOnlyCollection<ITakeRecorderContext> Contexts { get; }

        ITakeRecorderContext GetActiveContext();
    }
}
