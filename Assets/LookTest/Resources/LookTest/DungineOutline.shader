// Look test (phase 3): a one-pixel dark outline round the edges of shapes, found where the depth jumps between
// neighbouring pixels (the near side of the jump gets the line). It runs at the 3D render resolution, so with the
// pixelated look the line is one low-resolution pixel, like hand-drawn pixel-art outlines.
Shader "Hidden/Dungine/Outline"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Outline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _OutlineColor;   // rgb, a = strength
            float _OutlineJump;     // relative depth jump that counts as an edge (0.08 = 8 %)

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                float2 t = _CameraDepthTexture_TexelSize.xy;
                float d = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float far = d;
                far = max(far, LinearEyeDepth(SampleSceneDepth(uv + float2(t.x, 0)), _ZBufferParams));
                far = max(far, LinearEyeDepth(SampleSceneDepth(uv - float2(t.x, 0)), _ZBufferParams));
                far = max(far, LinearEyeDepth(SampleSceneDepth(uv + float2(0, t.y)), _ZBufferParams));
                far = max(far, LinearEyeDepth(SampleSceneDepth(uv - float2(0, t.y)), _ZBufferParams));
                float edge = step(_OutlineJump * d, far - d) * step(d, 200.0);
                col.rgb = lerp(col.rgb, _OutlineColor.rgb, edge * _OutlineColor.a);
                return col;
            }
            ENDHLSL
        }
    }
}
