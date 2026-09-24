Shader "PRUnitySDK/EntityWire"
{
    Properties
    {
        _WireColor ("Wire Color", Color) = (0.2, 0.9, 1, 0.8)
        _FillColor ("Fill Color", Color) = (0.1, 0.7, 0.9, 0.08)
        _GridScale ("Grid Scale", Float) = 6
        _SurfaceGrid ("Surface Grid", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _WireColor;
                half4 _FillColor;
                float _GridScale;
                float _SurfaceGrid;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_SurfaceGrid < 0.5)
                    return _WireColor;

                float3 coord = input.positionOS * _GridScale;
                float3 distanceToLine = abs(frac(coord + 0.5) - 0.5);
                float3 width = max(fwidth(coord), 0.0001);
                float3 axisLine = 1.0 - saturate(distanceToLine / width);
                float wireAmount = max(axisLine.x, max(axisLine.y, axisLine.z));
                return lerp(_FillColor, _WireColor, wireAmount);
            }
            ENDHLSL
        }
    }
}
