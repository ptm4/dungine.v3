Shader "Dungine/FX"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1
        _SoftFade ("Soft Fade Distance", Float) = 0.5
        _CamFade ("Camera Fade Distance", Float) = 1.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 10
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        _Scroll ("UV Scroll", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        ZTest [_ZTest]
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Intensity, _SoftFade, _CamFade;
                float4 _Scroll;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float4 screenPos : TEXCOORD1; float fog : TEXCOORD2; float eyeDepth : TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap) + _Scroll.xy * _Time.y;
                o.color = v.color;
                o.screenPos = ComputeScreenPos(p.positionCS);
                o.fog = ComputeFogFactor(p.positionCS.z);
                o.eyeDepth = -p.positionVS.z;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half4 c = tex * _BaseColor * i.color;
                c.rgb *= _Intensity;
                float2 suv = i.screenPos.xy / i.screenPos.w;
                float sceneZ = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float soft = _SoftFade > 0.001 ? saturate((sceneZ - i.eyeDepth) / _SoftFade) : 1;
                float camFade = _CamFade > 0.001 ? saturate((i.eyeDepth - 0.3) / _CamFade) : 1;
                c.a *= soft * camFade;
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}
