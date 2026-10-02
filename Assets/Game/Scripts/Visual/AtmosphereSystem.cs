using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Dungine.Visual
{
    public class AtmosphereProfile
    {
        public string name;
        public Color sunColor = new Color(.7f, .72f, .8f);
        public float sunIntensity = 0.8f;
        public Vector3 sunEuler = new Vector3(38, -30, 0);
        public float shadowStrength = 0.85f;
        public Color ambientSky = new Color(.28f, .3f, .36f), ambientEquator = new Color(.2f, .21f, .24f), ambientGround = new Color(.08f, .08f, .08f);
        public Color fogColor = new Color(.32f, .34f, .38f);
        public float fogDensity = 0.022f;
        public Color skyTop = new Color(.1f, .11f, .14f), skyHorizon = new Color(.36f, .38f, .42f), cloud = new Color(.26f, .27f, .3f), cloudLight = new Color(.5f, .5f, .55f);
        public float cloudCover = 0.75f, stars = 0f;
        public Vector3 moonDir = new Vector3(0.4f, 0.3f, 0.8f);
        public float exposure = 0f, contrast = 12f, saturation = -18f;
        public Color colorFilter = new Color(.95f, .97f, 1f);
        public float bloom = 0.5f, bloomThreshold = 1.0f, vignette = 0.32f, grain = 0.18f;
        public float temperature = -8f, tint = 0f;
        public Color shadowsTint = new Color(0.95f, 1f, 1.1f), highlightsTint = new Color(1.05f, 1f, 0.95f);
        public bool interior;
    }

    /// <summary>Owns the sun, ambient light, fog, skybox and the global post-processing volume.</summary>
    public class Atmosphere : MonoBehaviour
    {
        public static Atmosphere I;
        public Light sun;
        public Volume volume;
        VolumeProfile profile;
        ColorAdjustments color; Bloom bloom; Vignette vignette; FilmGrain grain; Tonemapping tone; WhiteBalance wb; DepthOfField dof; ShadowsMidtonesHighlights smh; LiftGammaGain lgg;
        Material skyMat;
        public AtmosphereProfile Current { get; private set; }

        public static Atmosphere Ensure()
        {
            if (I) return I;
            var go = new GameObject("Atmosphere");
            if (Application.isPlaying) DontDestroyOnLoad(go);
            I = go.AddComponent<Atmosphere>();
            I.Init();
            return I;
        }

        void Init()
        {
            var sg = new GameObject("Sun");
            sg.transform.SetParent(transform);
            sun = sg.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.04f; sun.shadowNormalBias = 0.4f;
            RenderSettings.sun = sun;

            var vg = new GameObject("PostVolume");
            vg.transform.SetParent(transform);
            volume = vg.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;
            tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            color = profile.Add<ColorAdjustments>(true);
            bloom = profile.Add<Bloom>(true);
            bloom.scatter.Override(0.72f);
            bloom.highQualityFiltering.Override(true);
            vignette = profile.Add<Vignette>(true);
            vignette.smoothness.Override(0.45f);
            grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            wb = profile.Add<WhiteBalance>(true);
            smh = profile.Add<ShadowsMidtonesHighlights>(true);
            lgg = profile.Add<LiftGammaGain>(true);
            dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Off);

            skyMat = MatLib.Sky(Color.black, Color.gray, Color.gray, Color.white, 0.6f, Vector3.forward, 0);
            RenderSettings.skybox = skyMat;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        public void Apply(AtmosphereProfile p)
        {
            Current = p;
            sun.color = p.sunColor;
            sun.intensity = p.sunIntensity;
            sun.transform.rotation = Quaternion.Euler(p.sunEuler);
            sun.shadowStrength = p.shadowStrength;
            sun.enabled = p.sunIntensity > 0.001f;
            sun.shadows = p.shadowStrength > 0.01f ? LightShadows.Soft : LightShadows.None;
            RenderSettings.ambientSkyColor = p.ambientSky;
            RenderSettings.ambientEquatorColor = p.ambientEquator;
            RenderSettings.ambientGroundColor = p.ambientGround;
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogDensity = p.fogDensity;
            skyMat.SetColor("_Top", p.skyTop);
            skyMat.SetColor("_Horizon", p.skyHorizon);
            skyMat.SetColor("_Bottom", p.skyHorizon * 0.5f);
            skyMat.SetColor("_CloudColor", p.cloud);
            skyMat.SetColor("_CloudLight", p.cloudLight);
            skyMat.SetFloat("_CloudCover", p.cloudCover);
            skyMat.SetVector("_MoonDir", p.moonDir.normalized);
            skyMat.SetFloat("_Stars", p.stars);
            color.postExposure.Override(p.exposure);
            color.contrast.Override(p.contrast);
            color.saturation.Override(p.saturation);
            color.colorFilter.Override(p.colorFilter);
            bloom.intensity.Override(p.bloom);
            bloom.threshold.Override(p.bloomThreshold);
            vignette.intensity.Override(p.vignette);
            vignette.color.Override(Color.black);
            grain.intensity.Override(p.grain);
            wb.temperature.Override(p.temperature);
            wb.tint.Override(p.tint);
            smh.shadows.Override(new Vector4(p.shadowsTint.r, p.shadowsTint.g, p.shadowsTint.b, 0));
            smh.highlights.Override(new Vector4(p.highlightsTint.r, p.highlightsTint.g, p.highlightsTint.b, 0));
            DynamicGI.UpdateEnvironment();
        }

        /// <summary>Cinematic depth of field for dialogue close-ups.</summary>
        public void SetDof(bool on, float focus = 3f)
        {
            if (on)
            {
                dof.mode.Override(DepthOfFieldMode.Bokeh);
                dof.focusDistance.Override(focus);
                dof.focalLength.Override(70f);
                dof.aperture.Override(3.2f);
            }
            else dof.mode.Override(DepthOfFieldMode.Off);
        }

        public void Flash(Color c, float amount)
        {
            if (Current == null) return;
            color.colorFilter.Override(Color.Lerp(Current.colorFilter, c, amount));
            color.postExposure.Override(Current.exposure + amount * 1.5f);
        }

        public void SetVignetteBoost(float extra)
        {
            if (Current == null) return;
            vignette.intensity.Override(Current.vignette + extra);
            vignette.color.Override(extra > 0 ? new Color(0.25f, 0, 0) : Color.black);
        }

        // --------------- presets ---------------
        public static AtmosphereProfile BaroviaDay => new AtmosphereProfile
        {
            name = "Barovia overcast",
            sunColor = new Color(.86f, .87f, .92f), sunIntensity = 2.2f, sunEuler = new Vector3(46, -38, 0), shadowStrength = 0.55f,
            ambientSky = new Color(.68f, .7f, .78f), ambientEquator = new Color(.5f, .5f, .53f), ambientGround = new Color(.22f, .21f, .2f),
            fogColor = new Color(.56f, .58f, .62f), fogDensity = 0.011f,
            skyTop = new Color(.26f, .28f, .32f), skyHorizon = new Color(.6f, .62f, .66f), cloud = new Color(.38f, .39f, .42f), cloudLight = new Color(.66f, .67f, .7f), cloudCover = .88f,
            exposure = 0.4f, contrast = 9f, saturation = -14f, colorFilter = new Color(.97f, .98f, 1f), bloom = .35f, bloomThreshold = 1.15f, vignette = .28f, grain = .12f, temperature = -6f
        };

        public static AtmosphereProfile BaroviaNight => new AtmosphereProfile
        {
            name = "Barovia night",
            sunColor = new Color(.5f, .6f, .9f), sunIntensity = 1.0f, sunEuler = new Vector3(38, 150, 0), shadowStrength = 0.7f,
            ambientSky = new Color(.24f, .28f, .42f), ambientEquator = new Color(.15f, .17f, .25f), ambientGround = new Color(.06f, .06f, .08f),
            fogColor = new Color(.1f, .12f, .18f), fogDensity = 0.022f,
            skyTop = new Color(.02f, .025f, .05f), skyHorizon = new Color(.11f, .13f, .19f), cloud = new Color(.07f, .08f, .11f), cloudLight = new Color(.3f, .33f, .42f), cloudCover = .6f, stars = .5f,
            moonDir = new Vector3(-0.35f, 0.4f, -0.85f),
            exposure = 0.55f, contrast = 14f, saturation = -12f, colorFilter = new Color(.88f, .93f, 1.05f), bloom = .75f, bloomThreshold = .9f, vignette = .36f, grain = .18f, temperature = -16f
        };

        public static AtmosphereProfile Dusk => new AtmosphereProfile
        {
            name = "Mists at dusk",
            sunColor = new Color(.95f, .6f, .45f), sunIntensity = 0.65f, sunEuler = new Vector3(12, -70, 0), shadowStrength = 0.7f,
            ambientSky = new Color(.3f, .27f, .33f), ambientEquator = new Color(.22f, .18f, .2f), ambientGround = new Color(.07f, .06f, .06f),
            fogColor = new Color(.34f, .3f, .33f), fogDensity = 0.024f,
            skyTop = new Color(.12f, .11f, .16f), skyHorizon = new Color(.52f, .38f, .34f), cloud = new Color(.24f, .2f, .24f), cloudLight = new Color(.75f, .5f, .4f), cloudCover = .7f, stars = .05f,
            moonDir = new Vector3(0.6f, 0.25f, -0.7f),
            exposure = 0.15f, contrast = 16f, saturation = -10f, colorFilter = new Color(1f, .95f, .92f), bloom = .6f, bloomThreshold = 1f, vignette = .34f, grain = .18f, temperature = 4f
        };

        public static AtmosphereProfile Mists => new AtmosphereProfile
        {
            name = "Svalich mists",
            sunColor = new Color(.95f, .86f, .78f), sunIntensity = 2.6f, sunEuler = new Vector3(40, -30, 0), shadowStrength = 0.6f,
            ambientSky = new Color(.75f, .72f, .8f), ambientEquator = new Color(.55f, .5f, .5f), ambientGround = new Color(.25f, .22f, .2f),
            fogColor = new Color(.5f, .47f, .5f), fogDensity = 0.013f,
            skyTop = new Color(.2f, .2f, .25f), skyHorizon = new Color(.62f, .52f, .5f), cloud = new Color(.33f, .3f, .34f), cloudLight = new Color(.8f, .64f, .56f), cloudCover = .78f, stars = 0f,
            moonDir = new Vector3(0.6f, 0.25f, -0.7f),
            exposure = 0.45f, contrast = 8f, saturation = -4f, colorFilter = new Color(1f, .97f, .95f), bloom = .45f, bloomThreshold = 1.1f, vignette = .28f, grain = .12f, temperature = 2f
        };

        public static AtmosphereProfile Interior => new AtmosphereProfile
        {
            name = "Interior",
            interior = true,
            sunColor = new Color(1f, .88f, .74f), sunIntensity = 0.7f, sunEuler = new Vector3(68, 25, 0), shadowStrength = 0f,
            ambientSky = new Color(.82f, .72f, .62f), ambientEquator = new Color(.62f, .53f, .45f), ambientGround = new Color(.28f, .23f, .19f),
            fogColor = new Color(.05f, .045f, .045f), fogDensity = 0.012f,
            skyTop = Color.black, skyHorizon = new Color(.05f, .05f, .06f), cloud = Color.black, cloudLight = Color.black, cloudCover = 0,
            exposure = 0.55f, contrast = 12f, saturation = -4f, colorFilter = new Color(1f, .96f, .9f), bloom = .7f, bloomThreshold = 1f, vignette = .36f, grain = .16f, temperature = 6f
        };

        public static AtmosphereProfile DeathHouse => new AtmosphereProfile
        {
            name = "Death House",
            interior = true,
            sunColor = new Color(.6f, .65f, .85f), sunIntensity = 0.45f, sunEuler = new Vector3(70, 20, 0), shadowStrength = 0f,
            ambientSky = new Color(.42f, .42f, .52f), ambientEquator = new Color(.3f, .3f, .38f), ambientGround = new Color(.12f, .12f, .15f),
            fogColor = new Color(.03f, .035f, .045f), fogDensity = 0.03f,
            skyTop = Color.black, skyHorizon = new Color(.03f, .03f, .04f), cloud = Color.black, cloudLight = Color.black, cloudCover = 0,
            exposure = 0.9f, contrast = 8f, saturation = -20f, colorFilter = new Color(.92f, .95f, 1f), bloom = .9f, bloomThreshold = .9f, vignette = .38f, grain = .24f, temperature = -4f
        };

        public static AtmosphereProfile Dungeon => new AtmosphereProfile
        {
            name = "Dungeon",
            interior = true,
            sunColor = new Color(.55f, .6f, .8f), sunIntensity = 0.3f, sunEuler = new Vector3(72, 20, 0), shadowStrength = 0f,
            ambientSky = new Color(.32f, .33f, .4f), ambientEquator = new Color(.22f, .22f, .28f), ambientGround = new Color(.09f, .09f, .11f),
            fogColor = new Color(.02f, .025f, .03f), fogDensity = 0.04f,
            skyTop = Color.black, skyHorizon = Color.black, cloud = Color.black, cloudLight = Color.black, cloudCover = 0,
            exposure = 1.0f, contrast = 6f, saturation = -15f, colorFilter = new Color(.95f, .95f, 1f), bloom = 1f, bloomThreshold = .85f, vignette = .36f, grain = .25f, temperature = -2f
        };
    }
}
