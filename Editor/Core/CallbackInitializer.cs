using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Timeline;

namespace Unity.LiveCapture
{
    [InitializeOnLoad]
    static class CallbackInitializer
    {
        static CallbackInitializer()
        {
            EditorApplication.update += OnUpdate;
            Callbacks.seekOccurred += SeekOccurred;
        }

        static void OnUpdate()
        {
            UpdateServers();
        }

        static void UpdateServers()
        {
            foreach (var server in ServerManager.instance.servers)
            {
                server.OnUpdate();
            }
        }

        static void SeekOccurred(ISlate slate, PlayableDirector director)
        {
            if (TimelineEditor.inspectedDirector == director)
            {
                TimelineEditor.Refresh(RefreshReason.WindowNeedsRedraw);
            }
        }
    }
}
