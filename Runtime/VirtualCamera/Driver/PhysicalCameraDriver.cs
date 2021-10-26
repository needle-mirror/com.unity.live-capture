#if URP_10_2_OR_NEWER || HDRP_10_2_OR_NEWER
    #define USING_SCRIPTABLE_RENDER_PIPELINE
#endif

#if !USING_SCRIPTABLE_RENDER_PIPELINE && PP_3_0_3_OR_NEWER
    #define USING_POST_PROCESSING_STACK_V2
#endif

using System;
using UnityEngine;

#if HDRP_10_2_OR_NEWER
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Unity.LiveCapture.VirtualCamera
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(Camera))]
    [HelpURL(Documentation.baseURL + "ref-component-physical-camera-driver" + Documentation.endURL)]
    [ExcludeFromPreset]
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
#if USING_POST_PROCESSING_STACK_V2
        [SerializeField, HideInInspector]
        PostProcessingV2CameraDriverComponent m_PostProcessingV2CameraDriverComponent = new PostProcessingV2CameraDriverComponent();
#endif
#if !USING_POST_PROCESSING_STACK_V2 && !USING_SCRIPTABLE_RENDER_PIPELINE
        [SerializeField, HideInInspector]
        VanillaCameraDriverComponent m_VanillaCameraDriverComponent = new VanillaCameraDriverComponent();
#endif

        Camera m_Camera;
        ICameraDriverImpl m_Impl;

        protected override void Initialize()
        {
            m_Camera = GetComponent<Camera>();
            m_Camera.usePhysicalProperties = true;

#if HDRP_10_2_OR_NEWER
            var hdCameraData = GetComponent<HDAdditionalCameraData>();
            if (hdCameraData == null)
                hdCameraData = gameObject.AddComponent<HDAdditionalCameraData>();

            m_HdrpNoCinemachineCameraDriverComponent.Camera = m_Camera;

            m_HdrpCoreComponent.SetRoot(gameObject);
#endif
#if URP_10_2_OR_NEWER
            m_UrpComponent.SetCamera(m_Camera);
#endif
#if USING_POST_PROCESSING_STACK_V2
            m_PostProcessingV2CameraDriverComponent.SetCamera(m_Camera);
#endif
#if !USING_POST_PROCESSING_STACK_V2 && !USING_SCRIPTABLE_RENDER_PIPELINE
            m_VanillaCameraDriverComponent.Camera = m_Camera;
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
#if USING_POST_PROCESSING_STACK_V2
                    m_PostProcessingV2CameraDriverComponent,
#endif
#if !USING_POST_PROCESSING_STACK_V2 && !USING_SCRIPTABLE_RENDER_PIPELINE
                    m_VanillaCameraDriverComponent,
#endif
                });
            }

            return m_Impl;
        }

        /// <inheritdoc/>
        public override Camera GetCamera() => m_Camera;
    }
}
