using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Combat;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>Spell and combat visual effects: particles, projectiles, area blasts, flashes and status auras.</summary>
    public class FX : MonoBehaviour
    {
        public static FX I;
        readonly Dictionary<(Creature, Cond), GameObject> auras = new Dictionary<(Creature, Cond), GameObject>();
        readonly List<GameObject> spawned = new List<GameObject>();
        float auraScan;

        void Awake() { I = this; }

        public static Color ColorOf(FxKind k)
        {
            switch (k)
            {
                case FxKind.Fire: return new Color(1f, .5f, .15f);
                case FxKind.Frost: return new Color(.55f, .85f, 1f);
                case FxKind.Lightning: return new Color(.6f, .7f, 1f);
                case FxKind.Radiant: case FxKind.Holy: return new Color(1f, .88f, .5f);
                case FxKind.Necrotic: return new Color(.4f, .95f, .55f);
                case FxKind.Force: return new Color(.75f, .5f, 1f);
                case FxKind.Acid: return new Color(.6f, 1f, .25f);
                case FxKind.Poison: return new Color(.45f, .85f, .3f);
                case FxKind.Psychic: return new Color(1f, .45f, .85f);
                case FxKind.Thunder: return new Color(.75f, .78f, 1f);
                case FxKind.Heal: return new Color(.45f, 1f, .6f);
                case FxKind.Buff: return new Color(1f, .85f, .45f);
                case FxKind.Debuff: return new Color(.7f, .3f, .8f);
                case FxKind.Shadow: return new Color(.35f, .2f, .45f);
                case FxKind.Nature: return new Color(.5f, .9f, .35f);
                case FxKind.Blood: return new Color(.55f, .05f, .05f);
                case FxKind.Smoke: return new Color(.75f, .75f, .8f);
                default: return Color.white;
            }
        }

        static FxKind KindFor(DamageType t)
        {
            switch (t)
            {
                case DamageType.Fire: return FxKind.Fire;
                case DamageType.Cold: return FxKind.Frost;
                case DamageType.Lightning: return FxKind.Lightning;
                case DamageType.Radiant: return FxKind.Radiant;
                case DamageType.Necrotic: return FxKind.Necrotic;
                case DamageType.Force: return FxKind.Force;
                case DamageType.Acid: return FxKind.Acid;
                case DamageType.Poison: return FxKind.Poison;
                case DamageType.Psychic: return FxKind.Psychic;
                case DamageType.Thunder: return FxKind.Thunder;
                default: return FxKind.Blood;
            }
        }

        // ---------------------------------------------------------------- particle factory
        public static ParticleSystem MakePS(string name, Vector3 pos, Texture tex, bool additive, int burst, float life, float speed, float size, Color c0, Color c1, float gravity = 0, float shapeRadius = 0.1f, ParticleSystemShapeType shape = ParticleSystemShapeType.Sphere, float duration = 0.1f, bool loop = false, float rate = 0, bool worldSpace = true)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.layer = Layers.FX;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = duration; main.loop = loop; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = c0;
            main.gravityModifier = gravity;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = Mathf.Max(burst, 16) * (loop ? 8 : 1);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var em = ps.emission; em.rateOverTime = rate;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0, (short)burst) });
            var sh = ps.shape; sh.shapeType = shape; sh.radius = shapeRadius;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c0, 0), new GradientColorKey(c1, 1) }, new[] { new GradientAlphaKey(c0.a, 0), new GradientAlphaKey(c0.a * 0.8f, 0.4f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.6f, 1, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MatLib.FX(tex, Color.white, additive, 0.25f, additive ? 2.2f : 1f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            if (!loop) Object.Destroy(go, duration + life + 0.5f);
            return ps;
        }

        static void Flash(Vector3 pos, Color c, float intensity, float range, float dur)
        {
            var go = new GameObject("Flash");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            go.AddComponent<FadeLight>().dur = dur;
        }

        public static void Burst(Vector3 pos, FxKind k, float scale)
        {
            Color c = ColorOf(k);
            switch (k)
            {
                case FxKind.Fire:
                    MakePS("fire", pos, ProcTex.FogPuff, true, (int)(26 * scale), 0.7f, 3.2f * scale, 0.5f * scale, c, new Color(.4f, .08f, .02f, 0), -0.4f, 0.2f * scale);
                    MakePS("embers", pos, ProcTex.Spark, true, (int)(18 * scale), 1.2f, 4f * scale, 0.08f, new Color(1f, .7f, .3f), new Color(1f, .3f, .05f, 0), 0.4f, 0.15f);
                    MakePS("smoke", pos + Vector3.up * 0.3f, ProcTex.FogPuff, false, (int)(8 * scale), 1.8f, 0.8f, 0.9f * scale, new Color(.15f, .13f, .12f, .45f), new Color(.1f, .1f, .1f, 0), -0.1f, 0.3f);
                    Flash(pos, c, 5f * scale, 7f * scale, 0.35f);
                    break;
                case FxKind.Frost:
                    MakePS("frost", pos, ProcTex.Spark, true, (int)(30 * scale), 0.9f, 2.6f * scale, 0.18f * scale, c, new Color(.8f, .95f, 1f, 0), 0.3f, 0.2f);
                    MakePS("mist", pos, ProcTex.FogPuff, true, (int)(8 * scale), 1.2f, 0.8f, 0.8f * scale, new Color(.6f, .8f, 1f, .35f), new Color(.8f, .9f, 1f, 0), 0, 0.3f);
                    Flash(pos, c, 2.5f * scale, 5f, 0.3f);
                    break;
                case FxKind.Lightning:
                    MakePS("sparks", pos, ProcTex.Spark, true, (int)(34 * scale), 0.35f, 7f * scale, 0.12f, new Color(.8f, .85f, 1f), new Color(.4f, .5f, 1f, 0), 0.5f, 0.1f);
                    Flash(pos, c, 8f * scale, 9f, 0.18f);
                    break;
                case FxKind.Heal:
                    MakePS("heal", pos, ProcTex.Spark, true, (int)(22 * scale), 1.4f, 1.2f, 0.16f * scale, c, new Color(.8f, 1f, .8f, 0), -0.35f, 0.45f);
                    MakePS("glow", pos, ProcTex.SoftDisc, true, 3, 0.7f, 0.1f, 1.4f * scale, new Color(.4f, 1f, .6f, .5f), new Color(.4f, 1f, .6f, 0), 0, 0.05f);
                    Flash(pos, c, 2f, 4f, 0.5f);
                    break;
                case FxKind.Holy:
                case FxKind.Radiant:
                case FxKind.Buff:
                    MakePS("rays", pos, ProcTex.Spark, true, (int)(26 * scale), 1.0f, 2.2f * scale, 0.2f * scale, c, new Color(1f, 1f, .8f, 0), -0.2f, 0.25f);
                    MakePS("glow", pos, ProcTex.SoftDisc, true, 2, 0.6f, 0.1f, 1.8f * scale, new Color(c.r, c.g, c.b, .6f), new Color(c.r, c.g, c.b, 0), 0, 0.05f);
                    Flash(pos, c, 4f * scale, 6f, 0.4f);
                    break;
                case FxKind.Blood:
                    MakePS("blood", pos, ProcTex.Dot, false, (int)(16 * scale), 0.6f, 3f * scale, 0.1f * scale, new Color(.45f, .02f, .02f, 1f), new Color(.25f, 0f, 0f, 0), 1.6f, 0.1f);
                    break;
                case FxKind.Smoke:
                    MakePS("smoke", pos, ProcTex.FogPuff, false, (int)(14 * scale), 1.3f, 1.2f * scale, 1.0f * scale, new Color(.8f, .82f, .9f, .6f), new Color(.7f, .72f, .8f, 0), -0.05f, 0.35f);
                    break;
                default:
                    MakePS(k.ToString(), pos, ProcTex.Spark, true, (int)(26 * scale), 0.8f, 2.6f * scale, 0.16f * scale, c, new Color(c.r, c.g, c.b, 0), 0.1f, 0.2f);
                    MakePS("glow", pos, ProcTex.SoftDisc, true, 2, 0.5f, 0.1f, 1.3f * scale, new Color(c.r, c.g, c.b, .5f), new Color(c.r, c.g, c.b, 0), 0, 0.05f);
                    Flash(pos, c, 3f * scale, 5f, 0.3f);
                    break;
            }
        }

        public void Impact(Vector3 pos, ActionDef a, DamageType t, bool crit)
        {
            var k = a.fx != FxKind.None && a.IsSpell ? a.fx : KindFor(t);
            Burst(pos, k, crit ? 1.4f : 0.9f);
            if (t == DamageType.Slashing || t == DamageType.Piercing || t == DamageType.Bludgeoning)
            {
                MakePS("hitspark", pos, ProcTex.Spark, true, crit ? 16 : 8, 0.25f, 5f, 0.14f, new Color(1f, .9f, .7f), new Color(1f, .6f, .3f, 0), 0.3f, 0.05f);
            }
        }

        public void CastGlow(Actor a, Color c, float dur)
        {
            if (a == null) return;
            Transform hand = a.rig != null ? a.rig.socketHandR : a.transform;
            var ps = MakePS("castglow", hand.position, ProcTex.Spark, true, 0, 0.5f, 0.4f, 0.12f, c, new Color(c.r, c.g, c.b, 0), -0.2f, 0.12f, ParticleSystemShapeType.Sphere, dur, false, 40f);
            ps.transform.SetParent(hand, true);
            var glow = MakePS("handglow", hand.position, ProcTex.SoftDisc, true, 0, 0.3f, 0f, 0.5f, new Color(c.r, c.g, c.b, .5f), new Color(c.r, c.g, c.b, 0), 0, 0.02f, ParticleSystemShapeType.Sphere, dur, false, 12f);
            glow.transform.SetParent(hand, true);
            var l = new GameObject("castlight"); l.transform.SetParent(hand, false);
            var li = l.AddComponent<Light>(); li.type = LightType.Point; li.color = c; li.intensity = 2.5f; li.range = 3.5f;
            l.AddComponent<FadeLight>().dur = dur + 0.3f;
            if (a.rig != null && a.anim is HumanoidAnimator) { }
            var rune = MakePS("rune", a.transform.position + Vector3.up * 0.05f, ProcTex.Ring, true, 1, dur + 0.4f, 0f, 2.2f, new Color(c.r, c.g, c.b, .7f), new Color(c.r, c.g, c.b, 0), 0, 0.01f);
            var rr = rune.GetComponent<ParticleSystemRenderer>(); rr.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        }

        public IEnumerator Projectile(Vector3 from, Vector3 to, ProjKind kind, Color c)
        {
            var go = new GameObject("Projectile");
            go.transform.position = from;
            float speed = kind == ProjKind.Arrow || kind == ProjKind.Bolt ? 38f : kind == ProjKind.Flask ? 14f : 20f;
            bool physical = kind == ProjKind.Arrow || kind == ProjKind.Bolt || kind == ProjKind.Flask;
            if (physical)
            {
                var mb = new MeshBuilder(2);
                if (kind == ProjKind.Flask) { mb.AddEllipsoid(0, Vector3.zero, Vector3.one * 0.07f, 5, 7); mb.AddCylinder(1, new Vector3(0, 0.05f, 0), 0.02f, 0.02f, 0.06f, 5); }
                else { mb.Push(); mb.Rotate(Quaternion.Euler(90, 0, 0)); mb.AddCylinder(0, new Vector3(0, -0.4f, 0), 0.008f, 0.008f, 0.75f, 4); mb.AddCone(1, new Vector3(0, 0.35f, 0), 0.02f, 0.06f, 4); mb.Pop(); }
                go.AddComponent<MeshFilter>().sharedMesh = mb.Build("proj");
                go.AddComponent<MeshRenderer>().sharedMaterials = new[] { kind == ProjKind.Flask ? MatLib.Emissive(c, c * 0.6f) : MatLib.Lit(new Color(.4f, .3f, .2f), null, .3f), MatLib.Lit(new Color(.6f, .6f, .62f), null, .7f, .8f) };
            }
            else
            {
                var head = MakePS("head", from, ProcTex.SoftDisc, true, 0, 0.12f, 0, 0.55f, new Color(c.r, c.g, c.b, .9f), new Color(c.r, c.g, c.b, 0), 0, 0.02f, ParticleSystemShapeType.Sphere, 5f, true, 60f);
                head.transform.SetParent(go.transform, true);
                var core = MakePS("core", from, ProcTex.Spark, true, 0, 0.1f, 0, 0.35f, Color.white, new Color(c.r, c.g, c.b, 0), 0, 0.01f, ParticleSystemShapeType.Sphere, 5f, true, 40f);
                core.transform.SetParent(go.transform, true);
                var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.intensity = 3f; l.range = 5f;
            }
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = physical ? 0.12f : 0.35f;
            trail.widthMultiplier = physical ? 0.03f : 0.22f;
            trail.widthCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
            trail.material = MatLib.FX(ProcTex.SoftDisc, Color.white, true, 0, 1.5f);
            trail.startColor = physical ? new Color(1, 1, 1, .5f) : new Color(c.r, c.g, c.b, .9f);
            trail.endColor = new Color(c.r, c.g, c.b, 0);
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float dist = Vector3.Distance(from, to);
            float dur = Mathf.Max(0.12f, dist / speed);
            float arc = kind == ProjKind.Flask ? dist * 0.25f : kind == ProjKind.Arrow ? dist * 0.04f : 0f;
            float t = 0;
            Vector3 prev = from;
            if (kind == ProjKind.Lightning)
            {
                Destroy(go);
                yield return LightningBolt(from, to, c);
                yield break;
            }
            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                Vector3 p = Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * arc;
                go.transform.position = p;
                if ((p - prev).sqrMagnitude > 1e-6f) go.transform.rotation = Quaternion.LookRotation(p - prev);
                if (kind == ProjKind.Flask) go.transform.Rotate(Vector3.right, 720 * Time.deltaTime);
                prev = p;
                yield return null;
            }
            go.transform.position = to;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Stop();
            var li = go.GetComponent<Light>(); if (li) li.enabled = false;
            var mr = go.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false;
            Destroy(go, 0.6f);
        }

        IEnumerator LightningBolt(Vector3 from, Vector3 to, Color c)
        {
            var go = new GameObject("Bolt");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = MatLib.FX(ProcTex.SoftDisc, Color.white, true, 0, 3f);
            lr.widthMultiplier = 0.12f;
            lr.startColor = lr.endColor = new Color(.8f, .85f, 1f, 1f);
            int n = 14;
            lr.positionCount = n;
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < n; i++)
                {
                    float k = i / (float)(n - 1);
                    Vector3 p = Vector3.Lerp(from, to, k);
                    if (i > 0 && i < n - 1) p += Random.insideUnitSphere * 0.25f;
                    lr.SetPosition(i, p);
                }
                yield return null;
            }
            Destroy(go);
            Flash(to, c, 6f, 8f, 0.2f);
        }

        public void AoE(ActionDef a, Vector3 casterPos, Vector3 point, Vector3 forward)
        {
            var c = a.color;
            switch (a.target)
            {
                case TargetKind.Point:
                case TargetKind.Aura:
                    {
                        Vector3 p = a.target == TargetKind.Aura ? casterPos : point;
                        float r = a.radius;
                        if (a.fx == FxKind.Fire && r >= 5)
                        {
                            CameraRig.I.Shake(0.25f);
                            MakePS("fireball", p + Vector3.up * 0.8f, ProcTex.FogPuff, true, 90, 1.1f, r * 1.3f, r * 0.45f, new Color(1f, .6f, .2f), new Color(.5f, .05f, 0, 0), -0.2f, 0.5f);
                            MakePS("fbsmoke", p + Vector3.up * 1f, ProcTex.FogPuff, false, 30, 2.6f, r * 0.6f, r * 0.6f, new Color(.12f, .1f, .09f, .6f), new Color(.1f, .1f, .1f, 0), -0.15f, 1f);
                            MakePS("fbembers", p, ProcTex.Spark, true, 60, 1.6f, r * 1.5f, 0.12f, new Color(1f, .7f, .3f), new Color(1f, .2f, 0, 0), 0.6f, 0.5f);
                            Flash(p + Vector3.up, new Color(1f, .6f, .3f), 25f, r * 3f, 0.7f);
                        }
                        else if (a.fx == FxKind.Radiant && a.special == null && a.target == TargetKind.Point)
                        {
                            MakePS("beam", p + Vector3.up * 3f, ProcTex.SoftDisc, true, 30, 0.9f, 0.2f, 0.8f, new Color(1f, .95f, .7f, .8f), new Color(1f, .9f, .5f, 0), 1.5f, r * 0.8f, ParticleSystemShapeType.Circle);
                            Burst(p + Vector3.up * 0.3f, FxKind.Radiant, r * 0.6f);
                        }
                        else
                        {
                            var ring = MakePS("ring", p + Vector3.up * 0.1f, ProcTex.Ring, true, 1, 0.6f, 0, r * 2.2f, new Color(c.r, c.g, c.b, .9f), new Color(c.r, c.g, c.b, 0), 0, 0.01f);
                            ring.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
                            MakePS("area", p + Vector3.up * 0.4f, a.fx == FxKind.Smoke || a.fx == FxKind.Poison || a.fx == FxKind.Shadow ? ProcTex.FogPuff : ProcTex.Spark, true, (int)(20 + r * 10), 1.0f, 1.2f, a.fx == FxKind.Shadow ? 1.5f : 0.25f, new Color(c.r, c.g, c.b, .8f), new Color(c.r, c.g, c.b, 0), -0.1f, r * 0.8f, ParticleSystemShapeType.Circle);
                            Flash(p + Vector3.up, c, 5f, r * 2.5f, 0.4f);
                        }
                        break;
                    }
                case TargetKind.Cone:
                    {
                        var ps = MakePS("cone", casterPos + Vector3.up * 1.2f + forward * 0.4f, a.fx == FxKind.Fire ? ProcTex.FogPuff : ProcTex.Spark, true, 70, a.radius / 8f + 0.3f, a.radius * 1.8f, a.fx == FxKind.Fire ? 0.6f : 0.25f, new Color(c.r, c.g, c.b, .9f), new Color(c.r * .5f, c.g * .3f, c.b * .2f, 0), 0, 0.1f, ParticleSystemShapeType.Cone);
                        ps.transform.rotation = Quaternion.LookRotation(forward);
                        var sh = ps.shape; sh.angle = a.coneAngle * 0.45f;
                        Flash(casterPos + forward * a.radius * 0.5f + Vector3.up, c, 6f, a.radius * 1.5f, 0.4f);
                        if (a.fx == FxKind.Thunder) CameraRig.I.Shake(0.15f);
                        break;
                    }
                case TargetKind.Line:
                    {
                        StartCoroutine(LightningBolt(casterPos + Vector3.up * 1.2f, casterPos + forward * a.radius + Vector3.up * 1.0f, c));
                        var ps = MakePS("line", casterPos + Vector3.up * 1.1f, ProcTex.Spark, true, 60, 0.6f, 1f, 0.2f, new Color(c.r, c.g, c.b, .9f), new Color(c.r, c.g, c.b, 0), 0, 0.1f, ParticleSystemShapeType.SingleSidedEdge);
                        var sh = ps.shape; sh.radius = a.radius * 0.5f;
                        ps.transform.position = casterPos + forward * a.radius * 0.5f + Vector3.up;
                        ps.transform.rotation = Quaternion.LookRotation(Vector3.Cross(forward, Vector3.up));
                        break;
                    }
            }
        }

        // ---------------------------------------------------------------- status auras
        void Update()
        {
            auraScan -= Time.deltaTime;
            if (auraScan > 0) return;
            auraScan = 0.3f;
            var list = Targeting.Candidates().ToList();
            foreach (var c in list)
            {
                if (c.actor == null) continue;
                Ensure(c, Cond.SpiritGuardians, () => Loop(c.actor.transform, Vector3.up * 1f, ProcTex.Spark, new Color(1f, .9f, .5f, .8f), 40, 1.6f, 4.5f, ParticleSystemShapeType.Circle));
                Ensure(c, Cond.Raging, () => Loop(c.actor.transform, Vector3.up * c.actor.Height * 0.6f, ProcTex.FogPuff, new Color(1f, .15f, .1f, .35f), 12, 0.6f, 0.35f, ParticleSystemShapeType.Sphere));
                Ensure(c, Cond.Burning, () => Loop(c.actor.transform, Vector3.up * c.actor.Height * 0.5f, ProcTex.FogPuff, new Color(1f, .5f, .15f, .8f), 25, 0.5f, 0.3f, ParticleSystemShapeType.Sphere));
                Ensure(c, Cond.Hasted, () => Loop(c.actor.transform, Vector3.up * c.actor.Height * 0.5f, ProcTex.Spark, new Color(.6f, .7f, 1f, .8f), 15, 0.5f, 0.4f, ParticleSystemShapeType.Sphere));
                Ensure(c, Cond.Blessed, () => Loop(c.actor.transform, Vector3.up * (c.actor.Height + 0.2f), ProcTex.Spark, new Color(1f, .9f, .5f, .8f), 6, 0.8f, 0.2f, ParticleSystemShapeType.Circle));
                Ensure(c, Cond.FaerieFire, () => Loop(c.actor.transform, Vector3.up * c.actor.Height * 0.5f, ProcTex.Spark, new Color(.8f, .4f, 1f, .9f), 20, 0.7f, 0.45f, ParticleSystemShapeType.Sphere));
                Ensure(c, Cond.Hexed, () => Loop(c.actor.transform, Vector3.up * (c.actor.Height + 0.1f), ProcTex.FogPuff, new Color(.3f, .9f, .4f, .4f), 5, 1f, 0.15f, ParticleSystemShapeType.Circle));
                Ensure(c, Cond.Turned, () => Loop(c.actor.transform, Vector3.up * c.actor.Height * 0.8f, ProcTex.Spark, new Color(1f, .9f, .6f, .6f), 8, 0.6f, 0.3f, ParticleSystemShapeType.Sphere));
                Ensure(c, Cond.Asleep, () => Loop(c.actor.transform, Vector3.up * (c.actor.Height * 0.4f), ProcTex.Dot, new Color(.7f, .8f, 1f, .7f), 3, 1.5f, 0.2f, ParticleSystemShapeType.Circle));
            }
            foreach (var k in auras.Keys.ToList())
            {
                if (k.Item1 == null || k.Item1.actor == null || !k.Item1.Has(k.Item2) || k.Item1.dead || auras[k] == null)
                {
                    if (auras[k]) Destroy(auras[k]);
                    auras.Remove(k);
                }
            }
        }

        void Ensure(Creature c, Cond cond, System.Func<GameObject> make)
        {
            if (!c.Has(cond) || c.dead) return;
            if (auras.TryGetValue((c, cond), out var go) && go) return;
            auras[(c, cond)] = make();
        }

        GameObject Loop(Transform parent, Vector3 offset, Texture tex, Color col, float rate, float life, float radius, ParticleSystemShapeType shape)
        {
            var ps = MakePS("aura", parent.position + offset, tex, true, 0, life, 0.3f, tex == ProcTex.FogPuff ? 0.6f : 0.12f, col, new Color(col.r, col.g, col.b, 0), -0.05f, radius, shape, 5f, true, rate, false);
            ps.transform.SetParent(parent, true);
            ps.transform.localRotation = shape == ParticleSystemShapeType.Circle ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
            return ps.gameObject;
        }

        public void StopAura(Creature c)
        {
            foreach (var k in auras.Keys.Where(k => k.Item1 == c).ToList()) { if (auras[k]) Destroy(auras[k]); auras.Remove(k); }
        }

        public void ClearAll()
        {
            foreach (var a in auras.Values) if (a) Destroy(a);
            auras.Clear();
        }
    }

    public class FadeLight : MonoBehaviour
    {
        public float dur = 0.3f; Light l; float i0, t;
        void Start() { l = GetComponent<Light>(); i0 = l.intensity; }
        void Update() { t += Time.deltaTime; if (l) l.intensity = i0 * Mathf.Clamp01(1 - t / dur); if (t >= dur) Destroy(gameObject); }
    }
}
