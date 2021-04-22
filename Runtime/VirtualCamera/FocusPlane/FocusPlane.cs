using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;

#endif
#if URP_10_2_OR_NEWER
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    /// <summary>
    /// Map between Camera and FocusPlane components.
    /// </summary>
    class FocusPlaneMap : ComponentMap<Camera, FocusPlane> {}

    /// <summary>
    /// A component that provides focus plane visualization.
    /// </summary>
    /// <remarks>
    /// Each instance is tied to a specific camera.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Live Capture/Virtual Camera/Focus Plane")]
    public class FocusPlane : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera to use to render the focus plane")]
        Camera m_Camera;
        [SerializeField]
        FocusPlaneSettings m_Settings = FocusPlaneSettings.GetDefault();

        FocusPlaneSettings m_CachedSettings;
#pragma warning disable 649
        IFocusPlaneImpl m_Impl;
#pragma warning restore 649

        /// <inheritdoc cref="IFocusPlaneImpl.renderMaterial" />
        internal Material renderMaterial => m_Impl.renderMaterial;

        /// <inheritdoc cref="IFocusPlaneImpl.composeMaterial" />
        internal Material composeMaterial => m_Impl.composeMaterial;

        /// <inheritdoc cref="IFocusPlaneImpl.TryGetRenderTarget" />
        internal bool TryGetRenderTarget<T>(out T target)
        {
            return m_Impl.TryGetRenderTarget(out target);
        }

        /// <summary>
        /// Sets the camera to use to render the focus plane.
        /// </summary>
        /// <param name="camera">The camera to set</param>
        public void SetCamera(Camera camera)
        {
            if (m_Camera != camera)
            {
                m_Camera = camera;
                Validate();
            }
        }

        /// <summary>
        /// Sets the focus distance.
        /// </summary>
        /// <param name="value">The focus distance</param>
        public void SetFocusDistance(float value)
        {
            var settings = m_Settings;
            settings.cameraDepthThreshold = value;
            m_Settings = settings;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
        }

        void OnDestroy()
        {
            FocusPlaneMap.instance.RemoveInstance(this);
        }

        void OnEnable()
        {
            m_CachedSettings = m_Settings;

            // Make sure settings and its cached version are different,
            // so that material properties get updated right away.
            m_CachedSettings.backgroundOpacity += 1;

#if HDRP_10_2_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset)
            {
                m_Impl = new HdrpFocusPlaneImpl();
                m_Impl.Initialize();
            }
#endif

#if URP_10_2_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset)
            {
                m_Impl = new UrpFocusPlaneImpl();
                m_Impl.Initialize();
            }
#endif

            if (m_Impl == null)
            {
                Debug.LogWarning($"{nameof(FocusPlane)} does not support the current graphics pipeline.");
            }
        }

        void OnDisable()
        {
            if (m_Impl != null)
            {
                m_Impl.Dispose();
            }
        }

        void OnValidate()
        {
            Validate();
        }

        void Update()
        {
            if (m_Settings != m_CachedSettings)
            {
                m_CachedSettings = m_Settings;
                m_Settings.Apply(renderMaterial);
            }
        }

        void Validate()
        {
            FocusPlaneMap.instance.RemoveInstance(this);

            if (m_Camera != null)
            {
                FocusPlaneMap.instance.AddUniqueInstance(m_Camera, this);
            }
        }

        /// <inheritdoc cref="IFocusPlaneImpl.AllocateTargetIfNeeded" />
        internal void AllocateTargetIfNeeded(int width, int height)
        {
            m_Impl.AllocateTargetIfNeeded(width, height);
        }
    }
}
