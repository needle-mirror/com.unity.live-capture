using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Unity.LiveCapture
{
    class TakeRecorderUpdateManager
    {
        struct TakeRecorderLiveUpdate
        {
        }

        public static TakeRecorderUpdateManager Instance { get; } = new TakeRecorderUpdateManager();

        static TakeRecorderUpdateManager()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();

            if (!playerLoop.TryFindSubSystem<PreLateUpdate>(out var preLateUpdate))
            {
                Debug.LogError($"{nameof(PreLateUpdate)} system not found.");

                return;
            }

            var index = preLateUpdate.IndexOf<PreLateUpdate.ScriptRunBehaviourLateUpdate>();

            if (index == -1)
            {
                Debug.LogError($"{nameof(PreLateUpdate.ScriptRunBehaviourLateUpdate)} system not found.");

                return;
            }

            preLateUpdate.AddSubSystem<TakeRecorderLiveUpdate>(index, LiveUpdate);

            if (playerLoop.TryUpdate(preLateUpdate))
            {
                PlayerLoop.SetPlayerLoop(playerLoop);
            }
        }

        static void LiveUpdate()
        {
            foreach (var takeRecorder in Instance.TakeRecorders)
            {
                takeRecorder.LiveUpdate();
            }
        }

        List<TakeRecorder> m_TakeRecorders = new List<TakeRecorder>();

        public IEnumerable<TakeRecorder> TakeRecorders => m_TakeRecorders;

        public void Register(TakeRecorder takeRecorder)
        {
            m_TakeRecorders.AddUnique(takeRecorder);
        }

        public void Unregister(TakeRecorder takeRecorder)
        {
            m_TakeRecorders.Remove(takeRecorder);
        }
    }
}
