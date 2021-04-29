using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif
#if VP_CINEMACHINE_2_4_0
using Cinemachine;
#endif

namespace Unity.LiveCapture.VirtualCamera.Editor
{
    static class VirtualCameraCreatorUtilities
    {
        const string k_CreateObjectUndoNameFmt = "Create {0}";

        static string GetUndoName(string objName)
        {
            return string.Format(k_CreateObjectUndoNameFmt, objName);
        }

        /// <summary>
        /// Creates a new <see cref="VirtualCameraActor"/> with all the required components.
        /// </summary>
        [MenuItem("GameObject/Live Capture/Camera/Virtual Camera Actor", false, 10)]
        public static GameObject CreateVirtualCameraActor()
        {
            var go = CreateVirtualCameraActorInternal(Selection.activeTransform);

            Selection.activeObject = go;

            return go;
        }

        internal static GameObject CreateVirtualCameraActorInternal()
        {
            return CreateVirtualCameraActorInternal(null);
        }

        internal static GameObject CreateVirtualCameraActorInternal(Transform parent)
        {
            var root = new GameObject("Virtual Camera Actor",
                typeof(VirtualCameraActor),
                typeof(PhysicalCameraDriver),
                typeof(FrameLines));

            StageUtility.PlaceGameObjectInCurrentStage(root);
            Undo.RegisterCreatedObjectUndo(root, GetUndoName(root.name));

            if (parent != null)
            {
                Undo.SetTransformParent(root.transform, parent, GetUndoName(root.name));
            }

            GameObjectUtility.EnsureUniqueNameForSibling(root);

            var camera = root.GetComponent<Camera>();
            camera.usePhysicalProperties = true;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000;

#if HDRP_10_2_OR_NEWER
            ConfigureHDCamera(camera);
#endif

            MatchToSceneView(root.transform);

            return root.gameObject;
        }

#if VP_CINEMACHINE_2_4_0
        /// <summary>
        /// Creates a Cinemachine Camera Actor with all the required components.
        /// </summary>
        [MenuItem("GameObject/Live Capture/Camera/Cinemachine Camera Actor", false, 10)]
        public static GameObject CreateCinemachineCameraActor()
        {
            var go = CreateCinemachineCameraActorInternal(Selection.activeTransform);

            Selection.activeObject = go;

            return go;
        }

        internal static GameObject CreateCinemachineCameraActorInternal()
        {
            return CreateCinemachineCameraActorInternal(null);
        }

        internal static GameObject CreateCinemachineCameraActorInternal(Transform parent)
        {
            var stage = StageUtility.GetCurrentStage();
            var name = "Cinemachine Camera Actor";
            var undoName = GetUndoName(name);
            var brains = stage.FindComponentsOfType<CinemachineBrain>()
                .Where(b => b != null && b.isActiveAndEnabled && b.OutputCamera != null);
            var brain = brains.FirstOrDefault();
            var camera = default(Camera);

            if (brain != null)
            {
                camera = brain.OutputCamera;
            }

            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                camera = stage.FindComponentOfType<Camera>();
            }

            if (camera == null)
            {
                var cameraRoot = new GameObject("Camera", typeof(Camera), typeof(CinemachineBrain));
                camera = cameraRoot.GetComponent<Camera>();
                cameraRoot.tag = "MainCamera";
                StageUtility.PlaceGameObjectInCurrentStage(cameraRoot);
                GameObjectUtility.EnsureUniqueNameForSibling(cameraRoot);
                Undo.RegisterCreatedObjectUndo(cameraRoot, undoName);
            }
            else if (brain == null)
            {
                brain = Undo.AddComponent<CinemachineBrain>(camera.gameObject);
            }

#if HDRP_10_2_OR_NEWER
            ConfigureHDCamera(camera);
#endif
            if (camera.GetComponent<FrameLines>() == null)
            {
                Undo.AddComponent<FrameLines>(camera.gameObject);
            }

            var root = new GameObject(name, typeof(CinemachineCameraDriver));
            StageUtility.PlaceGameObjectInCurrentStage(root);
            Undo.RegisterCreatedObjectUndo(root, undoName);

            if (parent != null)
            {
                Undo.SetTransformParent(root.transform, parent, undoName);
            }

            GameObjectUtility.EnsureUniqueNameForSibling(root);

            var virtualCameraRoot = new GameObject("Cinemachine Virtual Camera",
                typeof(CinemachineVirtualCamera));

            Undo.RegisterCreatedObjectUndo(virtualCameraRoot, undoName);
            Undo.SetTransformParent(virtualCameraRoot.transform, root.transform, undoName);

            var virtualCamera = virtualCameraRoot.GetComponent<CinemachineVirtualCamera>();
            virtualCamera.Follow = root.transform;
            virtualCamera.AddCinemachineComponent<CinemachineTransposer>();
            virtualCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();

            var driver = root.GetComponent<CinemachineCameraDriver>();
            driver.CinemachineVirtualCamera = virtualCamera;

            MatchToSceneView(root.transform);

            return root.gameObject;
        }

#endif

        static void MatchToSceneView(Transform t)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                t.position = SceneView.lastActiveSceneView.camera.transform.position;
                t.rotation = SceneView.lastActiveSceneView.camera.transform.rotation;
            }
        }

#if HDRP_10_2_OR_NEWER
        static void ConfigureHDCamera(Camera camera)
        {
            var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
            if (additionalCameraData == null)
                additionalCameraData = Undo.AddComponent<HDAdditionalCameraData>(camera.gameObject);

            additionalCameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            additionalCameraData.taaSharpenStrength = .6f;
            additionalCameraData.dithering = true;
            additionalCameraData.stopNaNs = true;
        }

#endif
    }
}
