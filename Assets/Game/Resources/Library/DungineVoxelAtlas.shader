// v3 (agent A): the lit URP shader for the library's game export (pixel3d/export/game, agent N).
// Albedo = the point-sampled atlas (_BaseMap) x the voxel AO baked in COLOR_0 (linear grey; pow(ao, _VoxAOPower)).
// Metal (blue) and roughness (green) come from the metallic-roughness atlas, as glTF has them; glow from the emission
// atlas x _SpecColor (the material's glow factor), plus _EmissionColor (left free for v2's hover highlight).
// The atlas is read at its top mip only: the export has one texel per voxel face, with no mipmaps.
//
// Lighting is URP's, with two global dials for the dark sides of the voxel steps (agent N's note on the streaks):
//   _VoxWrap  wrap diffuse: (N.L + w) / (1 + w); 0 is plain Lambert, 1 is half-Lambert's reach
//   _VoxFill  a multiplier on the ambient (sky) light
// Shadows, SSAO, fog and Forward+ lights work as for URP Lit; the shadow and depth passes are URP Lit's own code, compiled here.
Shader "Dungine/VoxelAtlas"
{
    Properties
    {
        [MainTexture] _BaseMap("Atlas", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        _MetallicGlossMap("Metal (B) Roughness (G)", 2D) = "white" {}
        _Metallic("Metallic factor", Range(0.0, 1.0)) = 1.0
        _Smoothness("1 - roughness factor", Range(0.0, 1.0)) = 0.0
        _EmissionMap("Glow", 2D) = "black" {}
        [HDR] _SpecColor("Glow factor", Color) = (0,0,0,1)
        [HDR] _EmissionColor("Highlight", Color) = (0,0,0,1)
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex VoxelVertex
            #pragma fragment VoxelFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #define _EMISSION 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // global dials (Shader.SetGlobalFloat), outside the material buffer
            half _VoxWrap;
            half _VoxFill;
            half _VoxAOPower;
            half _VoxNoShadow;   // diagnostic: 1 ignores the shadow map on the figure

            struct VoxelAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VoxelVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                half   ao         : TEXCOORD3;
                half   fogFactor  : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            VoxelVaryings VoxelVertex(VoxelAttributes input)
            {
                VoxelVaryings o = (VoxelVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = n.normalWS;
                o.uv = input.uv;
                o.ao = input.color.r;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half3 VoxLight(BRDFData brdf, Light light, half3 N, half3 V)
            {
                half ndl = dot(N, light.direction);
                half w = max(_VoxWrap, 0.0h);
                half diffuse = saturate((ndl + w) / (1.0h + w));
                half3 c = brdf.diffuse * diffuse;
                c += brdf.specular * DirectBRDFSpecular(brdf, N, light.direction, V) * saturate(ndl);
                return c * light.color * (light.distanceAttenuation * lerp(light.shadowAttenuation, 1.0h, saturate(_VoxNoShadow)));
            }

            half4 VoxelFragment(VoxelVaryings input) : SV_Target0
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // (LIGHT_LOOP_BEGIN reads a variable called inputData)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                half4 base = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_BaseMap, input.uv, 0) * _BaseColor;
                half4 mr = SAMPLE_TEXTURE2D_LOD(_MetallicGlossMap, sampler_MetallicGlossMap, input.uv, 0);
                half3 glow = SAMPLE_TEXTURE2D_LOD(_EmissionMap, sampler_EmissionMap, input.uv, 0).rgb;
                half aoPow = _VoxAOPower > 0 ? _VoxAOPower : 1.0h;
                half ao = pow(max(input.ao, 0.001h), aoPow);

                SurfaceData s = (SurfaceData)0;
                s.albedo = base.rgb * ao;
                s.alpha = 1;
                s.metallic = mr.b * _Metallic;
                s.smoothness = 1.0h - mr.g * (1.0h - _Smoothness);
                s.occlusion = 1;
                s.emission = glow * _SpecColor.rgb + _EmissionColor.rgb;
                s.specular = half3(0, 0, 0);

                BRDFData brdf;
                InitializeBRDFData(s, brdf);
                BRDFData coat = (BRDFData)0;
                half4 shadowMask = CalculateShadowMask(inputData);
                AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, s);
                Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);
                MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

                half fill = _VoxFill > 0 ? _VoxFill : 1.0h;
                half3 color = GlobalIllumination(brdf, coat, 0, inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
                                                 inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV) * fill;
                color += VoxLight(brdf, mainLight, inputData.normalWS, inputData.viewDirectionWS);

                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    color += VoxLight(brdf, light, inputData.normalWS, inputData.viewDirectionWS);
                }
                #endif
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
                    color += VoxLight(brdf, light, inputData.normalWS, inputData.viewDirectionWS);
                LIGHT_LOOP_END
                #endif

                color += s.emission;
                color = MixFog(color, inputData.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ _WRITE_SMOOTHNESS
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
    // no fallback to URP Lit and no UsePass from it: sharing Lit's compiled passes made the GPU Resident Drawer
    // treat URP Lit itself as not SRP-batcher compatible ("variant shared by inconsistent other shader fallback")
    FallBack Off
}
