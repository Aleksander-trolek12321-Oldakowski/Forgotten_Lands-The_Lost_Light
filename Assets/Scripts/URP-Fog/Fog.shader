Shader "Hidden/URP/Fog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.1, 0.1, 0.1, 1)
        _Density ("Density", Float) = 0.03
        _Start ("Start Distance", Float) = 0
        _End ("End Distance", Float) = 60
        _Height ("Height", Float) = 0
        _HeightDensity ("Height Density", Float) = 1
        _ExcludeSkybox ("Exclude Skybox", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _FogColor;
            float _Density;
            float _Start;
            float _End;
            float _Height;
            float _HeightDensity;
            float _ExcludeSkybox;

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.uv = uv;
                output.positionHCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);
                return output;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                #if UNITY_REVERSED_Z
                bool isSky = rawDepth <= 0.0001;
                #else
                bool isSky = rawDepth >= 0.9999;
                #endif

                if (_ExcludeSkybox > 0.5 && isSky)
                    return col;

                float depth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float distanceFog = saturate((depth - _Start) / max(_End - _Start, 0.0001));
                float heightFog = exp(-max(_Height - depth, 0.0) * max(_HeightDensity, 0.0));
                float fogFactor = saturate(distanceFog * heightFog * _Density);

                return lerp(col, _FogColor, fogFactor);
            }
            ENDHLSL
        }
    }
}
