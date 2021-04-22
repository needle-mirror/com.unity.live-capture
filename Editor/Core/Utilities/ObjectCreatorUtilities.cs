using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture
{
    static class ObjectCreatorUtilities
    {
        [MenuItem("GameObject/Live Capture/Take Recorder", false, 10)]
        public static void CreateTakeRecorder()
        {
            var name = "TakeRecorder";
            var undoName = "Create Take Recorder";
            var selectedTransform = Selection.activeTransform;
            var go = new GameObject(name, typeof(TakeRecorder));

            Undo.RegisterCreatedObjectUndo(go, undoName);

            if (selectedTransform != null)
            {
                Undo.SetTransformParent(go.transform, selectedTransform, undoName);
            }

            GameObjectUtility.EnsureUniqueNameForSibling(go);

            Selection.activeGameObject = go;
        }
    }
}
