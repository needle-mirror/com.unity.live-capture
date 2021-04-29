using UnityEditor;
using UnityEditor.Timeline;

namespace Unity.LiveCapture.Editor
{
    [InitializeOnLoad]
    static class CallbackInitializer
    {
        static CallbackInitializer()
        {
            EditorApplication.update += OnUpdate;
            Callbacks.SeekOccurred += SeekOccurred;
        }

        static void OnUpdate()
        {
            ConnectionManager.Instance.Update();
        }

        static void SeekOccurred()
        {
            if (TimelineEditor.inspectedDirector != null)
            {
                TimelineEditor.Refresh(RefreshReason.WindowNeedsRedraw);
            }
        }
    }
}
