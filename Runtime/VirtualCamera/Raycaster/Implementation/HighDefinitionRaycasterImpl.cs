#if HDRP_10_2_OR_NEWER
#if !HDRP_12_0_OR_NEWER
#define SET_GLOBAL_SHADER_VARIABLES
using System.Reflection;
#endif
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Unity.LiveCapture.VirtualCamera.Raycasting
{
    class HighDefinitionRaycasterImpl : BaseScriptableRenderPipelineRaycasterImpl
    {
#if SET_GLOBAL_SHADER_VARIABLES
        // *Must* match _ShaderVariablesGlobal in HDRP's HDStringConstants.
        static readonly int k_ShaderVariablesGlobalProp = Shader.PropertyToID("ShaderVariablesGlobal");
        const string k_HdrpRuntimeDll = "Unity.RenderPipelines.HighDefinition.Runtime.dll";
        const string k_ShaderVariablesGlobalFullTypeName = "UnityEngine.Rendering.HighDefinition.ShaderVariablesGlobal";
        static readonly Type k_ShaderVariablesGlobalType = Type.GetType($"{k_ShaderVariablesGlobalFullTypeName}, {k_HdrpRuntimeDll}");

        static readonly MethodInfo k_UpdateShaderVariablesGlobalMethodInfo = typeof(HDCamera)
            .GetMethod(
            "UpdateShaderVariablesGlobalCB",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { k_ShaderVariablesGlobalType.MakeByRefType(), typeof(int) },
            null);

        static readonly MethodInfo k_PushGlobalShaderVariablesMethodInfo = typeof(ConstantBuffer)
            .GetMethod(
            "PushGlobal",
            BindingFlags.Public | BindingFlags.Static)
            .MakeGenericMethod(k_ShaderVariablesGlobalType);

        // Instance of ShaderVariablesGlobal created using reflection since the type is internal.
        object[] m_UpdateShaderVariablesArgs = new object[2];
        object[] m_PushGlobalArgs = new object[3];
        int m_FrameCount;
#endif

        FrameSettings m_FrameSettings;

        public override void Initialize()
        {
            base.Initialize();

#if SET_GLOBAL_SHADER_VARIABLES
            // Constant args for reflection.
            m_UpdateShaderVariablesArgs[0] = Activator.CreateInstance(k_ShaderVariablesGlobalType);
            m_PushGlobalArgs[2] = k_ShaderVariablesGlobalProp;
#endif

            // by labeling the camera as a preview camera it will render using animated materials
            // correctly in edit mode for HDRP 7.1.x
            m_Camera.cameraType = CameraType.Preview;

            var data = m_Raycaster.AddComponent<HDAdditionalCameraData>();

            // Avoid an HDRP 10.2.2 Volumetric System leak.
            data.customRenderingSettings = true;
            data.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.ReprojectionForVolumetrics, false);
            data.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.ReprojectionForVolumetrics] = true;

            data.customRender += Render;
        }

        public override void Dispose()
        {
            var data = m_Raycaster.GetComponent<HDAdditionalCameraData>();
            data.customRender -= Render;

            base.Dispose();
        }

#if SET_GLOBAL_SHADER_VARIABLES
        void SetGlobalShaderVariables(CommandBuffer cmd, HDCamera camera)
        {
            m_UpdateShaderVariablesArgs[1] = m_FrameCount++;
            k_UpdateShaderVariablesGlobalMethodInfo.Invoke(camera, m_UpdateShaderVariablesArgs);

            m_PushGlobalArgs[0] = cmd;
            m_PushGlobalArgs[1] = m_UpdateShaderVariablesArgs[0];
            k_PushGlobalShaderVariablesMethodInfo.Invoke(null, m_PushGlobalArgs);
        }

#endif

        void Render(ScriptableRenderContext context, HDCamera camera)
        {
            context.SetupCameraProperties(m_Camera);

            var cmd = CommandBufferPool.Get("Graphics Raycast");

#if SET_GLOBAL_SHADER_VARIABLES
            SetGlobalShaderVariables(cmd, camera);
#endif

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            DoRender(cmd, context, m_Camera);

            CommandBufferPool.Release(cmd);
        }
    }
}
#endif
