Shader "Dungine/Sky"
{
    Properties
    {
        _Top ("Zenith", Color) = (0.05,0.06,0.09,1)
        _Horizon ("Horizon", Color) = (0.25,0.27,0.30,1)
        _Bottom ("Below", Color) = (0.08,0.08,0.09,1)
        _CloudColor ("Cloud", Color) = (0.2,0.21,0.24,1)
        _CloudLight ("Cloud Light", Color) = (0.5,0.5,0.55,1)
        _CloudTex ("Cloud Noise", 2D) = "white" {}
        _CloudCover ("Cloud Cover", Range(0,1)) = 0.6
        _CloudSpeed ("Cloud Speed", Float) = 0.004
        _MoonDir ("Moon Dir", Vector) = (0.3,0.35,0.9,0)
        _MoonColor ("Moon", Color) = (0.9,0.88,0.8,1)
        _MoonSize ("Moon Size", Float) = 0.035
        _Stars ("Stars", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Top, _Horizon, _Bottom, _CloudColor, _CloudLight, _MoonColor;
                float4 _MoonDir, _CloudTex_ST;
                float _CloudCover, _CloudSpeed, _MoonSize, _Stars;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float hash3(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }

            half4 frag (Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                half3 col = y > 0 ? lerp(_Horizon.rgb, _Top.rgb, pow(saturate(y), 0.55)) : lerp(_Horizon.rgb, _Bottom.rgb, saturate(-y * 4));

                float3 sd = floor(d * 400);
                float st = hash3(sd);
                col += step(0.9975, st) * _Stars * saturate(y * 3) * 0.8;

                float3 md = normalize(_MoonDir.xyz);
                float mdot = dot(d, md);
                float moonDisc = smoothstep(cos(_MoonSize), cos(_MoonSize * 0.93), mdot);
                float glow = pow(saturate(mdot), 180) * 0.6 + pow(saturate(mdot), 12) * 0.12;
                col += _MoonColor.rgb * (moonDisc + glow);

                if (y > -0.05)
                {
                    float2 uv = d.xz / (y + 0.18);
                    float t = _Time.y * _CloudSpeed;
                    float n1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv * 0.35 + float2(t, t * 0.3)).r;
                    float n2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, uv * 0.9 + float2(-t * 1.7, t * 0.8)).r;
                    float n = n1 * 0.65 + n2 * 0.35;
                    float cover = smoothstep(1 - _CloudCover, 1 - _CloudCover + 0.35, n);
                    float lit = saturate(pow(saturate(mdot * 0.5 + 0.5), 6) * (1 - n2 * 0.6));
                    half3 cc = lerp(_CloudColor.rgb, _CloudLight.rgb, lit);
                    float fade = saturate((y + 0.05) * 5);
                    col = lerp(col, cc, cover * fade * 0.95);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
