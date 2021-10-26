using Unity.LiveCapture.Editor;
using UnityEditor;

namespace Unity.LiveCapture.CompanionApp.Editor
{
    [InitializeOnLoad]
    static class CompanionAppRepaintManager
    {
        static CompanionAppRepaintManager()
        {
            CompanionAppServer.ClientConnected += OnClientConnected;
            CompanionAppServer.ClientDisconnected += OnClientDisconnected;
        }

        static void OnClientConnected(ICompanionAppClient client)
        {
            TakeRecorderEditor.RepaintEditors();
        }

        static void OnClientDisconnected(ICompanionAppClient client)
        {
            TakeRecorderEditor.RepaintEditors();
        }
    }
}
