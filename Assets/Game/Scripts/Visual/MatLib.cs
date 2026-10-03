using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dungine.Visual
{
    /// <summary>Creates and caches every material used at runtime. Templates in Resources/Mat keep shader variants alive in builds.</summary>
    public static class MatLib
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Material litTemplate, unlitTemplate, terrainTemplate;
        static Shader fx, ghost, sky;

        public static Shader FXShader => fx ? fx : (fx = Shader.Find("Dungine/FX"));
        public static Shader GhostShader => ghost ? ghost : (ghost = Shader.Find("Dungine/Ghost"));
        public static Shader SkyShader => sky ? sky : (sky = Shader.Find("Dungine/Sky"));

        static Material LitTemplate
        {
            get
            {
                if (litTemplate) return litTemplate;
                litTemplate = Resources.Load<Material>("Mat/LitTemplate");
                if (!litTemplate) litTemplate = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                return litTemplate;
            }
        }

        static Material UnlitTemplate
        {
            get
            {
                if (unlitTemplate) return unlitTemplate;
                unlitTemplate = Resources.Load<Material>("Mat/UnlitTemplate");
                if (!unlitTemplate) unlitTemplate = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                return unlitTemplate;
            }
        }

        public static Material TerrainTemplate
        {
            get
            {
                if (terrainTemplate) return terrainTemplate;
                terrainTemplate = Resources.Load<Material>("Mat/TerrainTemplate");
                if (!terrainTemplate) terrainTemplate = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                return terrainTemplate;
            }
        }

        static string Key(Color c) => ColorUtility.ToHtmlStringRGBA(c);

        /// <summary>Opaque PBR material. tex may be null for flat colour.</summary>
        public static Material Lit(Color color, TexId? tex = null, float smooth = 0.25f, float metal = 0f, float tiling = 1f, float bump = 1f)
        {
            string k = $"lit|{Key(color)}|{tex}|{smooth:F2}|{metal:F2}|{tiling:F2}|{bump:F2}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(LitTemplate) { name = k };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            m.SetColor("_EmissionColor", Color.black);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (tex.HasValue)
            {
                var t = ProcTex.Get(tex.Value);
                m.SetTexture("_BaseMap", t.albedo);
                m.SetTextureScale("_BaseMap", Vector2.one * tiling);
                m.SetTexture("_BumpMap", t.normal);
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            else
            {
                m.SetTexture("_BaseMap", ProcTex.White);
                m.SetTexture("_BumpMap", null);
                m.DisableKeyword("_NORMALMAP");
            }
            cache[k] = m;
            return m;
        }

        /// <summary>Per-character face material with a painted albedo.</summary>
        public static Material Face(Texture2D face, bool scaled)
        {
            var m = new Material(LitTemplate) { name = "Face" };
            m.SetColor("_BaseColor", Color.white);
            m.SetTexture("_BaseMap", face);
            var n = ProcTex.Get(scaled ? TexId.Scales : TexId.Skin);
            m.SetTexture("_BumpMap", n.normal);
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetFloat("_BumpScale", scaled ? 0.5f : 0.25f);
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_Smoothness", scaled ? .4f : .26f);
            m.SetFloat("_Metallic", 0);
            m.SetColor("_EmissionColor", Color.black);
            m.EnableKeyword("_EMISSION");
            return m;
        }

        public static Material Emissive(Color color, Color emission, TexId? tex = null)
        {
            string k = $"emi|{Key(color)}|{Key(emission)}|{tex}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(Lit(color, tex)) { name = k };
            m.SetColor("_EmissionColor", emission);
            m.EnableKeyword("_EMISSION");
            cache[k] = m;
            return m;
        }

        /// <summary>Alpha-tested, double-sided material (grass, leaves, cobwebs).</summary>
        public static Material Cutout(Texture2D tex, Color color, float cutoff = 0.4f)
        {
            string k = $"cut|{tex.name}|{Key(color)}|{cutoff:F2}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            var t = Resources.Load<Material>("Mat/LitTemplate_Cutout");
            m = t ? new Material(t) : new Material(LitTemplate);
            m.name = k;
            m.SetTexture("_BaseMap", tex);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_AlphaClip", 1);
            m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_NORMALMAP");
            m.SetFloat("_Cull", 0);
            m.SetFloat("_Smoothness", 0.05f);
            m.SetColor("_EmissionColor", Color.black);
            m.renderQueue = 2450;
            cache[k] = m;
            return m;
        }

        public static Material LitTransparent(Color color, float smooth = 0.6f)
        {
            string k = $"litT|{Key(color)}|{smooth:F2}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(LitTemplate) { name = k };
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", ProcTex.White);
            m.SetFloat("_Smoothness", smooth);
            MakeTransparent(m);
            cache[k] = m;
            return m;
        }

        public static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        public static Material Unlit(Color color, Texture tex = null)
        {
            string k = $"unlit|{Key(color)}|{(tex ? tex.name : "")}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(UnlitTemplate) { name = k };
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex ? tex : ProcTex.White);
            cache[k] = m;
            return m;
        }

        /// <summary>Soft blended FX material (particles, fog cards, decals, glows).</summary>
        public static Material FX(Texture tex, Color color, bool additive, float soft = 0.4f, float intensity = 1f, bool overlay = false)
        {
            string k = $"fx|{(tex ? tex.name + tex.GetHashCode() : "")}|{Key(color)}|{additive}|{soft:F2}|{intensity:F2}|{overlay}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(FXShader) { name = k };
            m.SetTexture("_BaseMap", tex ? tex : ProcTex.White);
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Intensity", intensity);
            m.SetFloat("_SoftFade", soft);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZTest", overlay ? (float)CompareFunction.Always : (float)CompareFunction.LessEqual);
            if (overlay) m.renderQueue = 3100;
            cache[k] = m;
            return m;
        }

        /// <summary>Uncached FX instance (for per-object fading).</summary>
        public static Material FXInstance(Texture tex, Color color, bool additive, float soft = 0.4f)
        {
            var m = new Material(FX(tex, color, additive, soft));
            return m;
        }

        public static Material Ghost(Color baseC, Color rim)
        {
            string k = $"ghost|{Key(baseC)}|{Key(rim)}";
            if (cache.TryGetValue(k, out var m) && m) return m;
            m = new Material(GhostShader) { name = k };
            m.SetColor("_BaseColor", baseC);
            m.SetColor("_RimColor", rim);
            cache[k] = m;
            return m;
        }

        static Texture2D _cloud;
        public static Texture2D CloudNoise
        {
            get
            {
                if (_cloud) return _cloud;
                int s = 256; _cloud = new Texture2D(s, s, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, name = "CloudNoise" };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                    {
                        float n = Noise.Fbm((x + .5f) / s, (y + .5f) / s, 4, 6, 999);
                        byte b = (byte)(Mathf.Clamp01(n) * 255);
                        px[y * s + x] = new Color32(b, b, b, 255);
                    }
                _cloud.SetPixels32(px); _cloud.Apply(true, true);
                return _cloud;
            }
        }

        public static Material Sky(Color top, Color horizon, Color cloud, Color cloudLight, float cover, Vector3 moonDir, float stars)
        {
            var m = new Material(SkyShader) { name = "Sky" };
            m.SetColor("_Top", top);
            m.SetColor("_Horizon", horizon);
            m.SetColor("_Bottom", horizon * 0.5f);
            m.SetColor("_CloudColor", cloud);
            m.SetColor("_CloudLight", cloudLight);
            m.SetTexture("_CloudTex", CloudNoise);
            m.SetFloat("_CloudCover", cover);
            m.SetVector("_MoonDir", moonDir.normalized);
            m.SetFloat("_Stars", stars);
            return m;
        }

        public static void ClearCache() => cache.Clear();
    }
}
