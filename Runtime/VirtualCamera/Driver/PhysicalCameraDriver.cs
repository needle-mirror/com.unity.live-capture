using System;
using UnityEngine;
#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Live Capture/Virtual Camera/Physical Camera Driver")]
    class PhysicalCameraDriver : BaseCameraDriver
    {
#if HDRP_10_2_OR_NEWER
        [SerializeField, Tooltip("High Definition Render Pipeline camera driver component.")]
        HdrpCoreCameraDriverComponent m_HdrpCoreComponent = new HdrpCoreCameraDriverComponent();

        HdrpNoCinemachineCameraDriverComponent m_HdrpNoCinemachineCameraDriverComponent = new HdrpNoCinemachineCameraDriverComponent();
#endif
#if URP_10_2_OR_NEWER
        [SerializeField, Tooltip("Universal Render Pipeline camera driver component.")]
        UrpCameraDriverComponent m_UrpComponent = new UrpCameraDriverComponent();
#endif

        Camera m_Camera;
        ICameraDriverImpl m_Impl;

        protected override void OnAwake()
        {
            m_Camera = GetComponent<Camera>();
            m_Camera.usePhysicalProperties = true;

#if HDRP_10_2_OR_NEWER
            var hdCameraData = GetComponent<HDAdditionalCameraData>();
            if (hdCameraData == null)
                hdCameraData = gameObject.AddComponent<HDAdditionalCameraData>();

            m_HdrpNoCinemachineCameraDriverComponent.camera = m_Camera;

            m_HdrpCoreComponent.SetRoot(gameObject);
#endif
#if URP_10_2_OR_NEWER
            m_UrpComponent.SetCamera(m_Camera);
#endif
        }

        protected override ICameraDriverImpl GetImplementation()
        {
            if (m_Impl == null)
            {
                m_Impl = new CompositeCameraDriverImpl(new ICameraDriverComponent[]
                {
#if HDRP_10_2_OR_NEWER
                    m_HdrpNoCinemachineCameraDriverComponent,
                    m_HdrpCoreComponent,
#endif
#if URP_10_2_OR_NEWER
                    m_UrpComponent,
#endif
                });
            }

            return m_Impl;
        }

        /// <inheritdoc/>
        public override Camera GetCamera() => m_Camera;
    }
}
