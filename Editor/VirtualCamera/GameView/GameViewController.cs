using System;
using Unity.LiveCapture.VideoStreaming.Server;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if INPUTSYSTEM_1_0_2_OR_NEWER
using UnityEngine.InputSystem;

#endif

namespace Unity.LiveCapture.VirtualCamera
{
    interface ILiveCaptureBridge
    {
        bool TryGetVirtualCameraDevice(out IVirtualCameraDeviceProxy proxy);
    }

    interface IVirtualCameraDeviceProxy
    {
        /// <summary>
        /// Returns whether or not the current focus mode allows for the reticle to be visible.
        /// </summary>
        /// <param name="normalizedPosition"></param>
        /// <returns></returns>
        bool SetReticlePosition(Vector2 normalizedPosition);

        Vector2 GetPreviewResolution();
    }

    class VirtualCameraDeviceProxy : IVirtualCameraDeviceProxy
    {
        public static VirtualCameraDeviceProxy instance { get; } = new VirtualCameraDeviceProxy();

        public VirtualCameraDevice device { get; set; }

        public bool SetReticlePosition(Vector2 normalizedPosition)
        {
            device.SetReticlePosition(normalizedPosition);
            return device.cameraState.focusMode != FocusMode.Disabled;
        }

        public Vector2 GetPreviewResolution()
        {
            return device.GetVideoServer().GetResolution();
        }
    }

    class LiveCaptureBridge : ILiveCaptureBridge
    {
        static readonly VirtualCameraDeviceProxy k_Proxy = new VirtualCameraDeviceProxy();

        /// <summary>
        /// Gives access to a `VirtualCameraDevice` proxy, if a valid device exists.
        /// A `VirtualCameraDevice` is considered valid if it is currently streaming video previewed in the GameView.
        /// </summary>
        /// <param name="proxy">Proxy providing a controlled access to the currently streaming `VirtualCameraDevice`.</param>
        /// <returns>Whether or not a valid proxy can be provided at this point.</returns>
        public bool TryGetVirtualCameraDevice(out IVirtualCameraDeviceProxy proxy)
        {
            proxy = null;

            // Look for the device corresponding to the currently streamed preview if any.
            foreach (var device in VirtualCameraDevice.instances)
            {
                var videoServer = device.GetVideoServer();
                var camera = videoServer.camera;

                if (videoServer.isRunning && camera.isActiveAndEnabled)
                {
                    VirtualCameraDeviceProxy.instance.device = device;
                    proxy = VirtualCameraDeviceProxy.instance;

                    return true;
                }
            }

            return false;
        }
    }

    class GameViewController : ScriptableSingleton<GameViewController>
    {
        [ExecuteAlways]
        internal class InGameBridge : MonoBehaviour
        {
            // Note that we do not provide access to the instance,
            // to prevent a reference to it being cached.
            static InGameBridge s_Instance;

            FocusReticle m_FocusReticle;
            Coroutine m_Animation;
            Canvas m_Canvas;

            public static bool isInstanciated => s_Instance != null;

            public static bool isInActiveScene => s_Instance == null ? false : s_Instance.gameObject.scene.isLoaded;

            public static GameObject root => s_Instance == null ? null : s_Instance.gameObject;

            public static void Dispose()
            {
                if (s_Instance != null)
                {
                    if (Application.isPlaying)
                        Destroy(s_Instance.gameObject);
                    else
                        DestroyImmediate(s_Instance.gameObject);
                }
            }

            public static void AnimateReticle(Vector2 position)
            {
                if (s_Instance != null)
                    s_Instance.DoAnimateReticle(position);
            }

            void DoAnimateReticle(Vector2 position)
            {
                if (m_Animation != null)
                    StopCoroutine(m_Animation);
                m_Canvas.enabled = true;
                m_FocusReticle.transform.transform.position = position;
                m_Animation = StartCoroutine(m_FocusReticle.Animate(true));
            }

            void OnAnimationComplete()
            {
                m_Canvas.enabled = false;
            }

            void Awake()
            {
                if (s_Instance != null)
                    throw new InvalidOperationException(
                        $"Multiple instances of {nameof(InGameBridge)} detected.");

                s_Instance = this;

                // Build UI.
                m_Canvas = gameObject.AddComponent<Canvas>();
                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                m_Canvas.enabled = false;

                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;

                var prefab = Resources.Load<GameObject>(k_FocusReticlePrefabPath);
                Assert.IsNotNull(prefab, $"Could not load Focus Reticle prefab form Resources at [{k_FocusReticlePrefabPath}]");

                var reticle = Instantiate(prefab, gameObject.transform);
                m_FocusReticle = reticle.GetComponent<FocusReticle>();
                Assert.IsNotNull(m_FocusReticle, $"Could not fetch {nameof(FocusReticle)} component from prefab.");

                m_FocusReticle.animationComplete += OnAnimationComplete;

                // Must set flag on parent AND its children.
                gameObject.hideFlags = HideFlags.HideAndDontSave;
                foreach (var child in GetComponentsInChildren<Transform>(true))
                    child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            void OnDestroy()
            {
                if (s_Instance != this)
                    throw new InvalidOperationException(
                        $"{nameof(InGameBridge)} current instance does not match static instance.");

                s_Instance = null;

                m_FocusReticle.animationComplete -= OnAnimationComplete;

                if (m_Animation != null)
                {
                    StopCoroutine(m_Animation);
                    m_Animation = null;
                }
            }

            void Update()
            {
#if INPUTSYSTEM_1_0_2_OR_NEWER
                var pointer = Mouse.current;
                if (pointer != null && pointer.leftButton.wasPressedThisFrame)
                    instance.OnMouseDown(pointer.position.ReadValue());
#else
                if (Input.GetMouseButtonDown(0))
                    instance.OnMouseDown(Input.mousePosition);
#endif
            }
        }

        const string k_GameObjectName = "Live Capture In Game Components";
        const string k_FocusReticlePrefabPath = "FocusReticle";

        static readonly ILiveCaptureBridge s_DefaultBridge = new LiveCaptureBridge();

        bool m_IsActive;
        [SerializeReference]
        ILiveCaptureBridge m_CurrentBridge;

        public bool isActive => m_IsActive;

        public void Enable()
        {
            Enable(s_DefaultBridge);
        }

        internal void Enable(ILiveCaptureBridge bridge)
        {
            if (m_IsActive)
                return;

            m_IsActive = true;
            m_CurrentBridge = bridge;
        }

        public void Disable()
        {
            if (!m_IsActive)
                return;

            InGameBridge.Dispose();

            m_CurrentBridge = null;
            m_IsActive = false;
        }

        [InitializeOnLoadMethod]
        static void RegisterCallbacks()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        static void OnEditorUpdate()
        {
            if (instance.m_IsActive)
                instance.OnUpdate();
        }

        void OnUpdate()
        {
            // Injected objects are marked [DontSave]. When switching scenes,
            // those objects may find themselves not destroyed, but belonging to an unloaded scene.
            if (!(InGameBridge.isInstanciated && InGameBridge.isInActiveScene))
            {
                if (InGameBridge.isInstanciated)
                {
                    var activeScene = SceneManager.GetActiveScene();
                    Assert.IsTrue(activeScene.isLoaded, "No active scene loaded.");
                    SceneManager.MoveGameObjectToScene(InGameBridge.root, activeScene);
                }
                else
                {
                    var go = new GameObject(k_GameObjectName);
                    go.AddComponent<InGameBridge>();
                }
            }
        }

        void OnMouseDown(Vector2 position)
        {
            // Destruction of the injected gameObject is deferred on runtime.
            if (!m_IsActive)
                return;

            if (m_CurrentBridge.TryGetVirtualCameraDevice(out var cameraDeviceProxy))
            {
                var screenSize = getScreenSize();
                var normalizedPosition = position / screenSize;

                if (cameraDeviceProxy.SetReticlePosition(normalizedPosition))
                {
                    InGameBridge.AnimateReticle(position);
                }
            }
        }

        // Using Func to make our coordinate conversion code testable.
        internal static Func<Vector2> getScreenSize = () =>
        {
            return new Vector2(Screen.width, Screen.height);
        };
    }
}
