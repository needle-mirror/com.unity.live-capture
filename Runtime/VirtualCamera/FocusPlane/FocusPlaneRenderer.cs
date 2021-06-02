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
    class FocusPlaneMap : ComponentMap<Camera, FocusPlaneRenderer> {}

    /// <summary>
    /// A component that provides focus plane visualization.
    /// </summary>
    /// <remarks>
    /// Each instance is tied to a specific camera.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Live Capture/Virtual Camera/Focus Plane Renderer")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "ref-component-focus-plane-renderer" + Documentation.endURL)]
    public class FocusPlaneRenderer : MonoBehaviour
    {
        [SerializeField, Tooltip("The camera to use to render the focus plane.")]
        Camera m_Camera;
        [SerializeField]
        FocusPlaneSettings m_Settings = FocusPlaneSettings.GetDefault();

        FocusPlaneSettings m_CachedSettings;

#pragma warning disable 649
        IFocusPlaneImpl m_Impl;
#pragma warning restore 649

        /// <inheritdoc cref="IFocusPlaneImpl.RenderMaterial" />
        internal Material RenderMaterial => m_Impl.RenderMaterial;

        /// <inheritdoc cref="IFocusPlaneImpl.ComposeMaterial" />
        internal Material ComposeMaterial => m_Impl.ComposeMaterial;

        /// <inheritdoc cref="IFocusPlaneImpl.TryGetRenderTarget{T}" />
        internal bool TryGetRenderTarget<T>(out T target)
        {
            return m_Impl.TryGetRenderTarget(out target);
        }

        /// <summary>
        /// Sets the camera to use to render the focus plane.
        /// </summary>
        /// <param name="camera">The camera to set.</param>
        public void SetCamera(Camera camera)
        {
            if (m_Camera != camera)
            {
                m_Camera = camera;
                if (m_Impl != null)
                {
                    m_Impl.SetCamera(m_Camera);
                }

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
            settings.CameraDepthThreshold = value;
            m_Settings = settings;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
        }

        void OnDestroy()
        {
            FocusPlaneMap.Instance.RemoveInstance(this);
        }

        void OnEnable()
        {
            m_CachedSettings = m_Settings;

            // Make sure settings and its cached version are different,
            // so that material properties get updated right away.
            m_CachedSettings.BackgroundOpacity += 1;

#if HDRP_10_2_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset)
            {
                m_Impl = new HdrpFocusPlaneImpl();
            }
#endif

#if URP_10_2_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset)
            {
                m_Impl = new UrpFocusPlaneImpl();
            }
#endif

            if (m_Impl == null)
            {
                Assert.IsNull(GraphicsSettings.renderPipelineAsset,
                    $"{nameof(FocusPlaneRenderer)}: no SRP implementation, yet cannot default to legacy render pipeline.");

                m_Impl = new LegacyFocusPlaneImpl();
            }

            m_Impl.Initialize();
            m_Impl.SetCamera(m_Camera);
        }

        void OnDisable()
        {
            m_Impl.Dispose();
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
                m_Settings.Apply(RenderMaterial);
            }

            m_Impl.Update();
        }

        void Validate()
        {
            FocusPlaneMap.Instance.RemoveInstance(this);

            if (m_Camera != null)
            {
                FocusPlaneMap.Instance.AddUniqueInstance(m_Camera, this);
            }
        }

        /// <inheritdoc cref="IFocusPlaneImpl.AllocateTargetIfNeeded" />
        internal void AllocateTargetIfNeeded(int width, int height)
        {
            m_Impl.AllocateTargetIfNeeded(width, height);
        }
    }
}
