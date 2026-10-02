using System;
using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    /// <summary>Material palette for world geometry, keyed by role.</summary>
    public static class Pal
    {
        public static Material Stone => MatLib.Lit(new Color(.72f, .7f, .68f), TexId.StoneBrick, .2f, 0, 1f, 1.2f);
        public static Material Ashlar => MatLib.Lit(new Color(.75f, .73f, .7f), TexId.Ashlar, .2f, 0, 1f, 1.2f);
        public static Material Cobble => MatLib.Lit(new Color(.8f, .78f, .76f), TexId.Cobble, .35f, 0, 1f, 1.2f);
        public static Material Plaster => MatLib.Lit(new Color(.82f, .78f, .72f), TexId.Plaster, .15f, 0, 1f, 1f);
        public static Material PlasterDark => MatLib.Lit(new Color(.55f, .52f, .48f), TexId.Plaster, .15f, 0, 1f, 1f);
        public static Material Timber => MatLib.Lit(new Color(.36f, .26f, .19f), TexId.WoodPlank, .2f, 0, 1f, 1f);
        public static Material TimberDark => MatLib.Lit(new Color(.2f, .15f, .12f), TexId.WoodPlank, .2f, 0, 1f, 1f);
        public static Material Planks => MatLib.Lit(new Color(.62f, .5f, .4f), TexId.WoodPlank, .25f, 0, 1f, 1f);
        public static Material FloorBoards => MatLib.Lit(new Color(1f, .9f, .8f), TexId.FloorBoards, .3f, 0, 1f, 1f);
        public static Material Shingle => MatLib.Lit(new Color(.8f, .78f, .76f), TexId.Shingle, .3f, 0, 1f, 1.3f);
        public static Material Slate => MatLib.Lit(new Color(.85f, .85f, .88f), TexId.Slate, .35f, 0, 1f, 1.3f);
        public static Material Thatch => MatLib.Lit(new Color(.8f, .75f, .7f), TexId.Thatch, .15f, 0, 1f, 1f);
        public static Material Iron => MatLib.Lit(new Color(.18f, .18f, .19f), TexId.Metal, .45f, .8f, 2f, .5f);
        public static Material Rust => MatLib.Lit(new Color(.32f, .2f, .14f), TexId.Metal, .3f, .5f, 2f, .5f);
        public static Material Gold => MatLib.Lit(new Color(.75f, .58f, .28f), TexId.Metal, .7f, 1f, 1f, .4f);
        public static Material Bark => MatLib.Lit(new Color(.55f, .5f, .46f), TexId.Bark, .15f, 0, 1f, 1.5f);
        public static Material PineNeedles => MatLib.Lit(new Color(.16f, .22f, .16f), TexId.Moss, .1f, 0, 2f, 1f);
        public static Material Rock => MatLib.Lit(new Color(.62f, .6f, .58f), TexId.Rock, .2f, 0, 1f, 1.5f);
        public static Material Wallpaper => MatLib.Lit(new Color(.9f, .85f, .85f), TexId.Wallpaper, .2f, 0, 1f, .8f);
        public static Material Fabric(Color c) => MatLib.Lit(c, TexId.Fabric, .1f, 0, 3f, .7f);
        public static Material Velvet => Fabric(new Color(.35f, .05f, .07f));
        public static Material Candle => MatLib.Lit(new Color(.9f, .87f, .78f), null, .3f);
        public static Material Flame => MatLib.Emissive(new Color(1f, .7f, .3f), new Color(4f, 2.2f, .7f));
        public static Material WindowLit => MatLib.Emissive(new Color(.9f, .6f, .3f), new Color(2.4f, 1.3f, .45f));
        public static Material WindowDark => MatLib.Lit(new Color(.05f, .06f, .08f), null, .9f);
        public static Material Glass => MatLib.Lit(new Color(.08f, .1f, .12f), null, .95f);
        public static Material Bone => MatLib.Lit(new Color(.8f, .77f, .68f), TexId.Bone, .3f);
        public static Material Dirt => MatLib.Lit(new Color(.8f, .75f, .7f), TexId.Dirt, .2f, 0, 1f, 1f);
        public static Material Mud => MatLib.Lit(new Color(.8f, .75f, .7f), TexId.Mud, .45f, 0, 1f, 1f);
        public static Material Moss => MatLib.Lit(new Color(.9f, .95f, .85f), TexId.Moss, .15f, 0, 1f, 1f);
        public static Material Blood => MatLib.Lit(new Color(.25f, .02f, .02f), null, .7f);
        public static Material Black => MatLib.Lit(new Color(.02f, .02f, .02f), null, .2f);
        public static Material StainedGlass => MatLib.Emissive(new Color(.5f, .2f, .3f), new Color(.9f, .35f, .5f));
    }

    /// <summary>Mesh + material caching and object spawning for procedural props.</summary>
    public static class Kit
    {
        static readonly Dictionary<string, (Mesh mesh, Material[] mats)> cache = new Dictionary<string, (Mesh, Material[])>();

        public static void ClearCache() => cache.Clear();

        public static (Mesh, Material[]) Cached(string key, Func<(Mesh, Material[])> make)
        {
            if (cache.TryGetValue(key, out var v) && v.mesh) return v;
            v = make();
            cache[key] = v;
            return v;
        }

        public static GameObject Place(string key, Func<(Mesh, Material[])> make, Transform parent, Vector3 pos, float yaw = 0, float scale = 1f, ColliderKind col = ColliderKind.None, bool shadows = true)
        {
            var (mesh, mats) = Cached(key, make);
            var go = new GameObject(key);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            AddCollider(go, mesh, col);
            return go;
        }

        public enum ColliderKind { None, Box, Mesh, Capsule, Floor }

        public static void AddCollider(GameObject go, Mesh mesh, ColliderKind col)
        {
            switch (col)
            {
                case ColliderKind.Box:
                    var b = go.AddComponent<BoxCollider>(); b.center = mesh.bounds.center; b.size = mesh.bounds.size; break;
                case ColliderKind.Mesh:
                case ColliderKind.Floor:
                    var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; break;
                case ColliderKind.Capsule:
                    var cc = go.AddComponent<CapsuleCollider>(); cc.center = new Vector3(0, mesh.bounds.size.y * 0.5f, 0); cc.height = mesh.bounds.size.y;
                    cc.radius = Mathf.Min(mesh.bounds.extents.x, mesh.bounds.extents.z) * 0.35f; break;
            }
        }

        public static GameObject FromBuilder(MeshBuilder mb, Material[] mats, Transform parent, string name, ColliderKind col = ColliderKind.None, bool shadows = true)
        {
            var mesh = mb.Build(name);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            AddCollider(go, mesh, col);
            return go;
        }

        public static Light PointLight(Transform parent, Vector3 pos, Color c, float intensity, float range, bool shadows = false, bool flicker = true)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.8f;
            if (flicker) go.AddComponent<Flicker>();
            return l;
        }

        public static Light SpotLight(Transform parent, Vector3 pos, Vector3 euler, Color c, float intensity, float range, float angle, bool shadows = true)
        {
            var go = new GameObject("Spot");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot; l.color = c; l.intensity = intensity; l.range = range; l.spotAngle = angle; l.innerSpotAngle = angle * 0.5f;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        /// <summary>Small flame quad(s) for candles and torches, with glow.</summary>
        public static GameObject Flame(Transform parent, Vector3 pos, float size = 0.06f)
        {
            var go = new GameObject("Flame");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var mf = go.AddComponent<MeshFilter>();
            var mb = new MeshBuilder(1);
            mb.AddEllipsoid(0, new Vector3(0, size * 0.9f, 0), new Vector3(size * 0.45f, size, size * 0.45f), 5, 6, p => new Vector3(p.x * (1 - Mathf.Max(0, p.y) / (size * 1.2f)), p.y, p.z * (1 - Mathf.Max(0, p.y) / (size * 1.2f))));
            mf.sharedMesh = mb.Build("flame");
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Pal.Flame; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0, size, 0);
            var q = glow.AddComponent<MeshFilter>(); q.sharedMesh = QuadMesh();
            var gr = glow.AddComponent<MeshRenderer>(); gr.sharedMaterial = MatLib.FX(ProcTex.SoftDisc, new Color(1f, .6f, .25f, .55f), true, 0.2f); gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glow.transform.localScale = Vector3.one * size * 9f;
            glow.AddComponent<Billboard>();
            go.AddComponent<FlameWobble>();
            return go;
        }

        static Mesh _quad;
        public static Mesh QuadMesh()
        {
            if (_quad) return _quad;
            _quad = new Mesh { name = "quad" };
            _quad.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            _quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            _quad.RecalculateNormals(); _quad.RecalculateBounds();
            return _quad;
        }

        /// <summary>Ground-hugging fog volume made of soft particles.</summary>
        public static ParticleSystem FogBank(Transform parent, Vector3 center, Vector3 size, Color color, int count = 60, float puffSize = 7f, float speed = 0.15f)
        {
            var go = new GameObject("Fog");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.layer = Layers.FX;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = true; main.duration = 20f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(18f, 30f);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(puffSize * 0.7f, puffSize * 1.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startColor = color;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission; em.rateOverTime = count / 22f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-speed, speed); vel.z = new ParticleSystem.MinMaxCurve(-speed * 0.5f, speed * 1.5f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0.02f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MatLib.FX(ProcTex.FogPuff, Color.white, false, 1.2f, 1f);
            // Low banks lie flat on the ground so the high tactical camera looks down on the mist
            // instead of through it; tall banks stay camera-facing for distant walls of fog.
            bool flat = size.y < 4f;
            r.renderMode = flat ? ParticleSystemRenderMode.HorizontalBillboard : ParticleSystemRenderMode.Billboard;
            r.sortMode = ParticleSystemSortMode.Distance;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = flat ? 4f : 1.2f;
            ps.Play();
            return ps;
        }

        /// <summary>Drifting embers, dust motes or falling leaves.</summary>
        public static ParticleSystem Motes(Transform parent, Vector3 center, Vector3 size, Color color, int count, float sz, float riseSpeed, bool additive = true)
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(6, 12); main.startSpeed = 0; main.prewarm = true;
            main.startSize = new ParticleSystem.MinMaxCurve(sz * 0.5f, sz); main.startColor = color; main.maxParticles = count; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = count / 9f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = size;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(riseSpeed * 0.5f, riseSpeed); vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.2f; noise.frequency = 0.4f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) }); col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = MatLib.FX(ProcTex.Dot, Color.white, additive, 0.1f, 1.5f); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }
    }

    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main; if (!cam) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }

    public class Flicker : MonoBehaviour
    {
        Light l; float baseI; float seed;
        void Start() { l = GetComponent<Light>(); baseI = l.intensity; seed = UnityEngine.Random.value * 100; }
        void Update() { if (l) l.intensity = baseI * (0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, seed)); }
    }

    public class FlameWobble : MonoBehaviour
    {
        float seed; Vector3 s0;
        void Start() { seed = UnityEngine.Random.value * 100; s0 = transform.localScale; }
        void Update() { float n = Mathf.PerlinNoise(Time.time * 8, seed); transform.localScale = new Vector3(s0.x * (0.9f + 0.2f * n), s0.y * (0.8f + 0.4f * n), s0.z * (0.9f + 0.2f * n)); }
    }

    /// <summary>Slow rotation for spinning/bobbing decoration.</summary>
    public class Spin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up; public float speed = 30f; public float bob; Vector3 p0;
        void Start() => p0 = transform.localPosition;
        void Update() { transform.Rotate(axis, speed * Time.deltaTime, Space.Self); if (bob > 0) transform.localPosition = p0 + Vector3.up * Mathf.Sin(Time.time * 1.5f) * bob; }
    }
}
