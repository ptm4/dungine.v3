// The forward pass of Dungine/VoxelAtlas and Dungine/VoxelAtlasOccluder (shared). Include after LitInput.hlsl and
// Lighting.hlsl; define VOXEL_OCCLUDER (and include OccluderClip.hlsl) for the occluder's screen-door holes.
#ifndef DUNGINE_VOXEL_ATLAS_FORWARD
#define DUNGINE_VOXEL_ATLAS_FORWARD

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
    UNITY_SETUP_INSTANCE_ID(input);   // under the GPU Resident Drawer this is what loads the material's values
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#ifdef VOXEL_OCCLUDER
    OccClip(input.positionCS);   // a see-through hole round the party while the building stands in the way
#endif

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

#endif
