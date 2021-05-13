using System.Linq;
using UnityEditor;
using UnityEngine;
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
        public static void CreateVirtualCameraActor()
        {
            var root = new GameObject("Virtual Camera Actor",
                typeof(PhysicalCameraDriver),
                typeof(FrameLines)).transform;
            GameObjectUtility.EnsureUniqueNameForSibling(root.gameObject);
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create " + root.name);
            Selection.activeObject = root;

            var camera = root.GetComponent<Camera>();
            camera.usePhysicalProperties = true;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 1000;

#if HDRP_10_2_OR_NEWER
            ConfigureHDCamera(camera);
#endif

            MatchToSceneView(root);
        }

#if VP_CINEMACHINE_2_4_0
        /// <summary>
        /// Creates a Cinemachine Camera Actor with all the required components.
        /// </summary>
        [MenuItem("GameObject/Live Capture/Camera/Cinemachine Camera Actor", false, 10)]
        public static void CreateCinemachineCameraActor()
        {
            var name = "Cinemachine Camera Actor";
            var undoName = $"Create {name}";
            var brains = Resources.FindObjectsOfTypeAll<CinemachineBrain>()
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
                camera = Object.FindObjectOfType<Camera>();
            }

            if (camera == null)
            {
                var cameraRoot = new GameObject("Camera", typeof(Camera), typeof(CinemachineBrain));
                camera = cameraRoot.GetComponent<Camera>();
                cameraRoot.tag = "MainCamera";
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

            var root = new GameObject(name, typeof(CinemachineCameraDriver)).transform;
            GameObjectUtility.EnsureUniqueNameForSibling(root.gameObject);
            Undo.RegisterCreatedObjectUndo(root.gameObject, undoName);
            Selection.activeObject = root;

            var virtualCameraRoot = new GameObject("Cinemachine Virtual Camera",
                typeof(CinemachineVirtualCamera)).transform;
            virtualCameraRoot.SetParent(root);

            var virtualCamera = virtualCameraRoot.GetComponent<CinemachineVirtualCamera>();
            virtualCamera.Follow = root;
            virtualCamera.AddCinemachineComponent<CinemachineTransposer>();
            virtualCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();

            var driver = root.GetComponent<CinemachineCameraDriver>();
            driver.CinemachineVirtualCamera = virtualCamera;

            MatchToSceneView(root);
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
