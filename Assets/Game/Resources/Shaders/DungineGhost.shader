Shader "Dungine/Ghost"
{
    Properties
    {
        _BaseColor ("Color", Color) = (0.5,0.8,1,0.35)
        _RimColor ("Rim", Color) = (0.7,0.95,1,1)
        _RimPower ("Rim Power", Float) = 2.5
        _Flicker ("Flicker", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _RimColor;
                float _RimPower, _Flicker;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float fog : TEXCOORD2; float3 wp : TEXCOORD3; };
            Varyings vert (Attributes a)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(a.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.n = TransformObjectToWorldNormal(a.normalOS);
                o.v = GetWorldSpaceViewDir(p.positionWS);
                o.wp = p.positionWS;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float3 n = normalize(i.n), v = normalize(i.v);
                float rim = pow(1 - saturate(dot(n, v)), _RimPower);
                float flick = 0.85 + 0.15 * sin(_Time.y * 7 + i.wp.y * 6) * _Flicker;
                half3 c = _BaseColor.rgb * _BaseColor.a + _RimColor.rgb * rim;
                float a = saturate(_BaseColor.a + rim) * flick;
                return half4(MixFog(c * flick, i.fog), a);
            }
            ENDHLSL
        }
    }
}
