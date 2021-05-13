Shader "Hidden/LiveCapture/FocusPlane/Render/Urp"
{
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Blend Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ USE_GRID

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FocusPlane.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                half4 positionCS : SV_POSITION;
                half2 uv : TEXCOORD0;
            };

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv);
                float eyeSpaceDepth = LinearEyeDepth(depth, _ZBufferParams);
                float screenAspect = _ScreenParams.x / _ScreenParams.y;
                return FocusPlane(eyeSpaceDepth, input.uv, screenAspect);
            }
            ENDHLSL
        }
    }
}
