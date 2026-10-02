#ifndef DUNGINE_OCCLUDER_CLIP
#define DUNGINE_OCCLUDER_CLIP

// Screen-door fade for occluders (see DungineOccluder.shader). Needs LitInput.hlsl (for _Parallax/_ClearCoatSmoothness).
void OccClip(float4 positionCS)
{
    float fade = _Parallax;
    if (fade <= 0.002) return;
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float aspect = _ScreenParams.x / _ScreenParams.y;
    float amount = _ClearCoatSmoothness;
    float eye = LinearEyeDepth(positionCS.z, _ZBufferParams);
    [unroll] for (int i = 0; i < 4; i++)
    {
        float4 h = _OccHoles[i];
        // only what stands in front of the character is cut; a wall behind them stays solid
        if (h.w > 0.1 && eye < h.w - 0.35)
        {
            float2 d = (uv - h.xy) * float2(aspect, 1);
            amount = max(amount, 1 - smoothstep(h.z * 0.55, h.z, length(d)));
        }
    }
    uint2 p = (uint2)positionCS.xy & 3;
    float threshold = (OccBayer[p.y * 4 + p.x] + 0.5) / 16.0;
    clip(threshold - fade * amount);
}

#endif
