Shader "Hidden/LiveCapture/FocusPlane/Render/Hdrp"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma multi_compile _ USE_GRID

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "FocusPlane.cginc"

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

        float depth = LoadCameraDepth(varyings.positionCS.xy);

        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        float eyeSpaceDepth = LinearEyeDepth(depth, _ZBufferParams);
        float screenAspect = _ScreenSize.x / _ScreenSize.y;

        return FocusPlane(eyeSpaceDepth, posInput.positionNDC, screenAspect);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
