using System.Collections.Generic;
using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dungine.UI
{
    /// <summary>Renders head-and-shoulders portraits of creatures into RenderTextures in a hidden studio.</summary>
    public class PortraitRenderer : MonoBehaviour
    {
        public static PortraitRenderer I;
        Camera cam;
        Transform studio;
        readonly Dictionary<string, RenderTexture> rts = new Dictionary<string, RenderTexture>();
        readonly Dictionary<string, GameObject> doubles = new Dictionary<string, GameObject>();
        readonly Queue<(string key, Creature c)> pending = new Queue<(string, Creature)>();
        int disableFrame = -1;
        static readonly Vector3 Origin = new Vector3(0, -800, 0);

        public static void Ensure()
        {
            if (I) return;
            var go = new GameObject("PortraitStudio");
            DontDestroyOnLoad(go);
            I = go.AddComponent<PortraitRenderer>();
            I.Setup();
        }

        void Setup()
        {
            studio = transform;
            studio.position = Origin;
            var cg = new GameObject("PortraitCam"); cg.transform.SetParent(studio, false);
            cam = cg.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.055f, 0.05f, 1f);
            cam.fieldOfView = 22f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 12f;
            cam.cullingMask = 1 << Layers.Portrait;
            cam.enabled = false;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; data.renderShadows = false;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var key = new GameObject("Key").AddComponent<Light>(); key.transform.SetParent(studio, false);
            key.type = LightType.Point; key.range = 8; key.intensity = 6f; key.color = new Color(1f, .9f, .78f); key.transform.localPosition = new Vector3(1.2f, 2.6f, 1.8f);
            var rim = new GameObject("Rim").AddComponent<Light>(); rim.transform.SetParent(studio, false);
            rim.type = LightType.Point; rim.range = 8; rim.intensity = 5f; rim.color = new Color(.6f, .7f, 1f); rim.transform.localPosition = new Vector3(-1.4f, 2.4f, -1.4f);
            var fill = new GameObject("Fill").AddComponent<Light>(); fill.transform.SetParent(studio, false);
            fill.type = LightType.Point; fill.range = 8; fill.intensity = 1.6f; fill.color = new Color(.9f, .75f, .6f); fill.transform.localPosition = new Vector3(-1.5f, 1.2f, 1.5f);
        }

        static string Key(Creature c) => c.isPC ? c.uid + (c.wildForm ?? "") : "m_" + (c.mdef?.id ?? c.name);

        public RenderTexture Get(Creature c)
        {
            Ensure();
            string k = Key(c);
            if (rts.TryGetValue(k, out var rt) && rt) return rt;
            rt = new RenderTexture(256, 300, 24, RenderTextureFormat.ARGB32) { name = "Portrait_" + c.name, antiAliasing = 1 };
            rt.Create();
            rts[k] = rt;
            pending.Enqueue((k, c));
            return rt;
        }

        public void Invalidate(Creature c)
        {
            if (c == null) return;
            string k = Key(c);
            if (doubles.TryGetValue(k, out var d) && d) Destroy(d);
            doubles.Remove(k);
            if (rts.ContainsKey(k)) pending.Enqueue((k, c));
        }

        public void InvalidateAll() { foreach (var p in Game.I.party) Invalidate(p); }

        void LateUpdate()
        {
            if (disableFrame >= 0 && Time.frameCount > disableFrame) { cam.enabled = false; disableFrame = -1; foreach (var d in doubles.Values) if (d) d.SetActive(false); }
            if (disableFrame >= 0 || pending.Count == 0) return;
            var (k, c) = pending.Dequeue();
            if (!rts.TryGetValue(k, out var rt) || rt == null) return;
            if (!doubles.TryGetValue(k, out var dbl) || !dbl) { dbl = BuildDouble(c); doubles[k] = dbl; }
            foreach (var d in doubles.Values) if (d) d.SetActive(false);
            if (!dbl) return;
            dbl.SetActive(true);
            // frame the head
            float h = 1.8f; float headY = 1.6f; float fz = 0f; float dist;
            var rig = dbl.GetComponent<HumanoidRig>();
            if (rig != null) { h = rig.height; headY = rig.headTop.position.y - Origin.y - rig.headSize * 0.62f; dist = 0.95f + rig.headSize * 1.6f; }
            else
            {
                // beasts and oddities: frame the front-upper part of their combined bounds (they face the camera)
                bool any = false; var b = new Bounds();
                foreach (var r in dbl.GetComponentsInChildren<Renderer>())
                {
                    if (!r || r.name.Contains("Ring") || r.name.Contains("Decal")) continue;
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }
                if (!any) b = new Bounds(Origin + Vector3.up, Vector3.one);
                h = b.size.y;
                headY = b.center.y - Origin.y + b.extents.y * 0.3f;
                fz = b.center.z - Origin.z + b.extents.z * 0.45f;
                dist = Mathf.Max(1.1f, Mathf.Max(b.size.y, b.size.x) * 1.35f);
                // skinned bounds are loose: prefer the animator's own idea of the body's height
                var drv = dbl.GetComponent<IAnimDriver>();
                if (drv != null && drv.Height > 0.2f)
                {
                    h = drv.Height;
                    bool beast = dbl.GetComponent<BeastAnimator>() != null || dbl.GetComponent<QuadrupedAnimator>() != null;
                    headY = h * (beast ? 1.1f : 0.62f);
                    fz = beast ? h * 0.3f : 0f;
                    dist = Mathf.Max(1.8f, h * (beast ? 2.9f : 3.0f));
                }
            }
            // the HUD lays a name plate over the bottom quarter of the portrait: lift the face clear of it
            float drop = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.13f;
            cam.transform.position = Origin + new Vector3(0.18f, headY + 0.02f - drop, fz + dist);
            cam.transform.LookAt(Origin + new Vector3(0, headY - 0.02f - drop, fz));
            cam.targetTexture = rt;
            cam.enabled = true;
            disableFrame = Time.frameCount + 1;
        }

        GameObject BuildDouble(Creature c)
        {
            GameObject go;
            if (c.isPC && c.wildshapeBackup == null)
            {
                var rig = HumanoidBuilder.Build(c.sheet.look, ActorFactory.GearFor(c), "PortraitDouble", 512);
                go = rig.gameObject;
                var anim = go.AddComponent<HumanoidAnimator>(); anim.Init(rig, MotionStyle.Normal);
            }
            else if (c.mdef != null && c.mdef.body == BodyKind.Humanoid)
            {
                var rig = HumanoidBuilder.Build(c.mdef.look ?? new Appearance(), c.mdef.gear ?? new GearLook(), "PortraitDouble", 256);
                go = rig.gameObject;
                var anim = go.AddComponent<HumanoidAnimator>(); anim.Init(rig, c.mdef.motion);
            }
            else
            {
                var fake = Creature.FromMonster(c.mdef);
                var a = ActorFactory.Spawn(fake, Origin, Quaternion.identity);
                go = a.gameObject;
                var agent = go.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (agent) Destroy(agent);
                Destroy(a);
            }
            go.transform.SetParent(studio, false);
            go.transform.position = Origin;
            go.transform.rotation = Quaternion.Euler(0, 180 + 12, 0);
            go.transform.rotation = Quaternion.LookRotation(new Vector3(0.25f, 0, 1));
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Portrait;
            return go;
        }
    }
}
