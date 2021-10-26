using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.LiveCapture.Editor
{
    static class ObjectCreatorUtilities
    {
        [MenuItem("GameObject/Live Capture/Take Recorder", false, 10)]
        public static void CreateTakeRecorder()
        {
            var name = "Take Recorder";
            var undoName = "Create Take Recorder";
            var selectedTransform = Selection.activeTransform;
            var go = new GameObject(name, typeof(TakeRecorder));

            Undo.RegisterCreatedObjectUndo(go, undoName);

            if (selectedTransform != null)
            {
                Undo.SetTransformParent(go.transform, selectedTransform, undoName);
            }

            GameObjectUtility.EnsureUniqueNameForSibling(go);

            StageUtility.PlaceGameObjectInCurrentStage(go);

            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/Live Capture/Timecode Synchronizer", isValidateFunction: false, priority: 10)]
        public static void CreateSynchronizer()
        {
            var name = "Timecode Synchronizer";
            var undoName = "Create Timecode Synchronizer";
            var selectedTransform = Selection.activeTransform;
            var go = new GameObject(name, typeof(SynchronizerComponent));

            Undo.RegisterCreatedObjectUndo(go, undoName);

            if (selectedTransform != null)
            {
                Undo.SetTransformParent(go.transform, selectedTransform, undoName);
            }

            GameObjectUtility.EnsureUniqueNameForSibling(go);

            StageUtility.PlaceGameObjectInCurrentStage(go);

            Selection.activeGameObject = go;
        }
    }
}
