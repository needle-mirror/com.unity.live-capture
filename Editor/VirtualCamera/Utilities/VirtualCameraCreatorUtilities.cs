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
        /// <summary>
        /// Creates a new <see cref="VirtualCameraActor"/> with all the required components.
        /// </summary>
        [MenuItem("GameObject/Live Capture/Camera/Virtual Camera Actor", false, 10)]
        public static GameObject CreateVirtualCameraActor()
        {
            var go = CreateVirtualCameraActorInternal();

            Selection.activeObject = go;

            return go;
        }

        internal static GameObject CreateVirtualCameraActorInternal()
        {
            var root = new GameObject("Virtual Camera Actor",
                typeof(VirtualCameraActor),
                typeof(PhysicalCameraDriver),
                typeof(FrameLines));
            GameObjectUtility.EnsureUniqueNameForSibling(root);
            Undo.RegisterCreatedObjectUndo(root, "Create " + root.name);

            var camera = root.GetComponent<Camera>();
            camera.usePhysicalProperties = true;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000;

#if HDRP_10_2_OR_NEWER
            ConfigureHDCamera(camera);
#endif

            MatchToSceneView(root.transform);

            StageUtility.PlaceGameObjectInCurrentStage(root);

            return root.gameObject;
        }

#if VP_CINEMACHINE_2_4_0
        /// <summary>
        /// Creates a Cinemachine Camera Actor with all the required components.
        /// </summary>
        [MenuItem("GameObject/Live Capture/Camera/Cinemachine Camera Actor", false, 10)]
        public static GameObject CreateCinemachineCameraActor()
        {
            var go = CreateCinemachineCameraActorInternal();

            Selection.activeObject = go;

            return go;
        }

        internal static GameObject CreateCinemachineCameraActorInternal()
        {
            var stage = StageUtility.GetCurrentStage();
            var name = "Cinemachine Camera Actor";
            var undoName = $"Create {name}";
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
                GameObjectUtility.EnsureUniqueNameForSibling(cameraRoot);
                StageUtility.PlaceGameObjectInCurrentStage(cameraRoot);
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
            GameObjectUtility.EnsureUniqueNameForSibling(root);
            Undo.RegisterCreatedObjectUndo(root.gameObject, undoName);

            var virtualCameraRoot = new GameObject("Cinemachine Virtual Camera",
                typeof(CinemachineVirtualCamera));

            virtualCameraRoot.transform.SetParent(root.transform);

            var virtualCamera = virtualCameraRoot.GetComponent<CinemachineVirtualCamera>();
            virtualCamera.Follow = root.transform;
            virtualCamera.AddCinemachineComponent<CinemachineTransposer>();
            virtualCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();

            var driver = root.GetComponent<CinemachineCameraDriver>();
            driver.CinemachineVirtualCamera = virtualCamera;

            MatchToSceneView(root.transform);

            StageUtility.PlaceGameObjectInCurrentStage(root);

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
