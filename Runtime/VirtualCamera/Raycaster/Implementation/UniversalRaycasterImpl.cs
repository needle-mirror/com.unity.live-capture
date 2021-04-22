#if URP_10_2_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.LiveCapture.Rendering;

namespace Unity.LiveCapture.VirtualCamera.Raycasting
{
    class UniversalRaycasterImpl : BaseScriptableRenderPipelineRaycasterImpl
    {
        public override void Initialize()
        {
            base.Initialize();
            RenderPipelineBridge.RequestRenderFeature<InjectionPointRenderFeature>();
            InjectionPointRenderPass.onExecute += OnExecute;
        }

        public override void Dispose()
        {
            InjectionPointRenderPass.onExecute -= OnExecute;
            base.Dispose();
        }

        void OnExecute(ScriptableRenderContext context, RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;

            if (camera != m_Camera)
                return;

            context.SetupCameraProperties(camera);

            var cmd = CommandBufferPool.Get("Graphics Raycast");

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            DoRender(cmd, context, m_Camera);

            CommandBufferPool.Release(cmd);
            context.Submit();
        }
    }
}
#endif
