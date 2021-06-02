using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace Unity.LiveCapture.VirtualCamera.Editor
{
    /// <summary>
    /// A proxy to the VirtualCameraDevice, introduced for testability,
    /// as well as decoupling from the VirtualCameraDevice API.
    /// </summary>
    interface IVirtualCameraDeviceProxy
    {
        /// <summary>
        /// Sets the reticle position.
        /// </summary>
        /// <param name="normalizedPosition">Normalized position of the reticle.</param>
        /// <returns>True if the reticle should be visible based on the current focus mode, false otherwise.</returns>
        bool SetReticlePosition(Vector2 normalizedPosition);

        /// <summary>
        /// Returns the video stream preview resolution.
        /// </summary>
        /// <returns>The preview resolution.</returns>
        Vector2 GetPreviewResolution();
    }

    /// <summary>
    /// Provides access to an IVirtualCameraDeviceProxy, introduced for testing purposes.
    /// </summary>
    interface ILiveCaptureBridge
    {
        /// <summary>
        /// Provides access to a VirtualCameraDevice proxy, if a valid device exists.
        /// A VirtualCameraDevice is considered valid if it is currently streaming video previewed in the GameView.
        /// </summary>
        /// <param name="proxy">Proxy providing a controlled access to the currently streaming VirtualCameraDevice.</param>
        /// <returns>True if a valid proxy is available, false otherwise.</returns>
        bool TryGetVirtualCameraDevice(out IVirtualCameraDeviceProxy proxy);
    }

    /// <summary>
    /// Production implementation of IVirtualCameraDeviceProxy (as opposed to testing ones).
    /// </summary>
    class VirtualCameraDeviceProxy : IVirtualCameraDeviceProxy
    {
        // The VirtualCameraDevice accessed by this proxy.
        public VirtualCameraDevice Device { get; set; }

        /// <inheritdoc />
        public bool SetReticlePosition(Vector2 normalizedPosition)
        {
            Device.SetReticlePosition(normalizedPosition);
            return Device.Settings.FocusMode != FocusMode.Clear;
        }

        /// <inheritdoc />
        public Vector2 GetPreviewResolution()
        {
            return Device.GetVideoServer().GetResolution();
        }
    }

    /// <summary>
    /// Production implementation of ILiveCaptureBridge (as opposed to testing ones).
    /// </summary>
    class LiveCaptureBridge : ILiveCaptureBridge
    {
        static readonly VirtualCameraDeviceProxy k_Proxy = new VirtualCameraDeviceProxy();

        /// <inheritdoc />
        public bool TryGetVirtualCameraDevice(out IVirtualCameraDeviceProxy proxy)
        {
            // Look for the device corresponding to the currently streamed preview if any.
            foreach (var device in VirtualCameraDevice.instances)
            {
                var videoServer = device.GetVideoServer();
                var camera = videoServer.Camera;

                if (videoServer.IsRunning && camera.isActiveAndEnabled)
                {
                    k_Proxy.Device = device;
                    proxy = k_Proxy;

                    return true;
                }
            }

            proxy = null;
            return false;
        }
    }

    /// <summary>
    /// Component responsible for managing the reticle UI and its animation.
    /// </summary>
    [ExecuteAlways]
    class ReticleManager : MonoBehaviour
    {
        const string k_FocusReticlePrefabPath = "Packages/com.unity.live-capture/Runtime/VirtualCamera/FocusReticle/FocusReticle.prefab";

        public event Action<Vector2> OnMouseDown = delegate {};

        FocusReticle m_FocusReticle;
        Coroutine m_Animation;
        Canvas m_Canvas;
        bool m_PendingMouseDown;
        Vector2 m_MousePosition;

        // Used for tests only
        internal void SendMouseDown(Vector2 position)
        {
            m_MousePosition = position;
            m_PendingMouseDown = true;
        }

        void OnGUI()
        {
            // Using IMGUI events saves us the hassle of dealing with multiple input systems.
            if (Event.current.type == EventType.MouseDown)
            {
                m_PendingMouseDown = true;
                m_MousePosition = Event.current.mousePosition;

                // Flip Y axis, IMGUI coords are zero at top.
                m_MousePosition.y = Screen.height - m_MousePosition.y;
            }
        }

        void Awake()
        {
            // Build UI.
            m_Canvas = gameObject.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.enabled = false;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_FocusReticlePrefabPath);
            Assert.IsNotNull(prefab, $"Could not load Focus Reticle prefab at [{k_FocusReticlePrefabPath}]");

            var reticle = Instantiate(prefab, gameObject.transform);

            // Since we use constant physical size with default settings we apply scaling to the reticle.
            reticle.transform.localScale = Vector3.one * .5f;
            m_FocusReticle = reticle.GetComponent<FocusReticle>();
            Assert.IsNotNull(m_FocusReticle, $"Could not fetch {nameof(FocusReticle)} component from prefab.");
        }

        void OnEnable()
        {
            m_FocusReticle.AnimationComplete += OnAnimationComplete;
        }

        void OnDisable()
        {
            m_FocusReticle.AnimationComplete -= OnAnimationComplete;

            if (m_Animation != null)
            {
                StopCoroutine(m_Animation);
                m_Animation = null;
            }

            OnAnimationComplete();
        }

        void Update()
        {
            if (m_PendingMouseDown)
            {
                OnMouseDown.Invoke(m_MousePosition);
                m_PendingMouseDown = false;
            }
        }

        public void AnimateReticle(Vector2 position)
        {
            if (m_Animation != null)
            {
                StopCoroutine(m_Animation);
            }

            m_Canvas.enabled = true;
            m_FocusReticle.transform.transform.position = position;
            m_Animation = StartCoroutine(m_FocusReticle.Animate(true));
        }

        void OnAnimationComplete()
        {
            m_Canvas.enabled = false;
        }
    }

    /// <summary>
    /// Singleton responsible for managing the GameView reticle.
    /// Manages the injected scene UI and communication with the VirtualCameraDevice.
    /// </summary>
    class GameViewController : ScriptableSingleton<GameViewController>
    {
        const string k_GameObjectName = "Live Capture In Game Components";
        static readonly ILiveCaptureBridge k_DefaultBridge = new LiveCaptureBridge();

        [SerializeReference]
        ILiveCaptureBridge m_CurrentBridge;

        ReticleManager m_ReticleManager;
        bool m_IsActive;

        public bool IsActive => m_IsActive;

        [InitializeOnLoadMethod]
        static void RegisterCallbacks()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            switch (playModeStateChange)
            {
                // Disable on playmode changes to prevent the interruption of an animation
                // leaving the reticle on the screen (OnDisable is not called due to HideFlags.HideAndDontSave being used)
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                {
                    instance.Disable();
                }
                break;
            }
        }

        static void OnBeforeAssemblyReload()
        {
            instance.Disable();
        }

        public void Enable()
        {
            Enable(k_DefaultBridge);
        }

        internal void Enable(ILiveCaptureBridge bridge)
        {
            if (m_IsActive)
            {
                return;
            }

            m_IsActive = true;
            m_CurrentBridge = bridge;

            InjectSceneUI();
        }

        public void Disable()
        {
            if (!m_IsActive)
            {
                return;
            }

            DisposeSceneUI();

            m_CurrentBridge = null;
            m_IsActive = false;
        }

        void InjectSceneUI()
        {
            Assert.IsNull(m_ReticleManager);
            var go = new GameObject(k_GameObjectName, typeof(ReticleManager));
            SetHideFlagsRecursively(go, HideFlags.HideAndDontSave);
            m_ReticleManager = go.GetComponent<ReticleManager>();
            m_ReticleManager.OnMouseDown += OnMouseDown;
        }

        void DisposeSceneUI()
        {
            Assert.IsNotNull(m_ReticleManager);

            m_ReticleManager.OnMouseDown -= OnMouseDown;

            if (Application.isPlaying)
            {
                Destroy(m_ReticleManager.gameObject);
            }
            else
            {
                DestroyImmediate(m_ReticleManager.gameObject);
            }

            m_ReticleManager = null;
        }

        void OnMouseDown(Vector2 position)
        {
            Assert.IsTrue(m_IsActive);

            if (m_CurrentBridge.TryGetVirtualCameraDevice(out var cameraDeviceProxy))
            {
                var screenSize = GetScreenSize();
                var normalizedPosition = position / screenSize;

                if (cameraDeviceProxy.SetReticlePosition(normalizedPosition))
                {
                    if (cameraDeviceProxy.SetReticlePosition(normalizedPosition))
                    {
                        m_ReticleManager.AnimateReticle(position);
                    }
                }
            }
        }

        // Using Func to make our coordinate conversion code testable.
        internal static Func<Vector2> GetScreenSize = () =>
        {
            return new Vector2(Screen.width, Screen.height);
        };

        static void SetHideFlagsRecursively(GameObject gameObject, HideFlags hideFlags)
        {
            gameObject.hideFlags = hideFlags;
            foreach (Transform child in gameObject.transform)
            {
                SetHideFlagsRecursively(child.gameObject, hideFlags);
            }
        }
    }
}
