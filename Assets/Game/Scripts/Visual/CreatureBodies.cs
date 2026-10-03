using System;
using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>Procedural quadrupeds: wolves, dire wolves, bears, rats.</summary>
    public class QuadrupedRig : MonoBehaviour
    {
        public enum Q { Root, Pelvis, Chest, Neck, Head, Jaw, Tail0, Tail1, Tail2, FLu, FLl, FLp, FRu, FRl, FRp, BLu, BLl, BLp, BRu, BRl, BRp, COUNT }
        public Transform[] bones = new Transform[(int)Q.COUNT];
        public Vector3[] bindPos = new Vector3[(int)Q.COUNT];
        public float height, length;
        public Transform model, mouth;
        public List<Renderer> renderers = new List<Renderer>();
    }

    public static class QuadrupedBuilder
    {
        const int FUR = 0, BELLY = 1, DARK = 2, EYE = 3, TEETH = 4;
        static int I(QuadrupedRig.Q q) => (int)q;

        public static QuadrupedRig Build(BodyKind kind, Color fur, float scale, string name)
        {
            bool bear = kind == BodyKind.Bear, rat = kind == BodyKind.Rat;
            float H = (bear ? 1.05f : rat ? 0.45f : 0.78f);   // shoulder height (before scale)
            float L = (bear ? 1.25f : rat ? 0.7f : 1.05f);    // body length
            float girth = bear ? 0.3f : rat ? 0.14f : 0.17f;
            var go = new GameObject(name);
            var rig = go.AddComponent<QuadrupedRig>();
            rig.height = H * scale; rig.length = L * scale;
            var model = new GameObject("Model").transform; model.SetParent(go.transform, false); rig.model = model;
            model.localScale = Vector3.one * scale;

            var pos = new Vector3[(int)QuadrupedRig.Q.COUNT];
            var par = new int[(int)QuadrupedRig.Q.COUNT];
            float legLen = H - girth * 0.9f;
            float hipY = H - girth * 0.3f;
            Vector3 pelvis = new Vector3(0, hipY, -L * 0.42f), chest = new Vector3(0, H + (bear ? 0.08f : 0f), L * 0.35f);
            void D(QuadrupedRig.Q q, QuadrupedRig.Q p, Vector3 v) { pos[I(q)] = v; par[I(q)] = I(p); }
            par[0] = -1;
            D(QuadrupedRig.Q.Pelvis, QuadrupedRig.Q.Root, pelvis);
            D(QuadrupedRig.Q.Chest, QuadrupedRig.Q.Pelvis, chest);
            D(QuadrupedRig.Q.Neck, QuadrupedRig.Q.Chest, chest + new Vector3(0, girth * 0.4f, girth * 0.7f));
            Vector3 headP = chest + new Vector3(0, girth * (bear ? 0.6f : 1.1f), girth * 2.1f + (rat ? 0.02f : 0.12f));
            D(QuadrupedRig.Q.Head, QuadrupedRig.Q.Neck, headP);
            D(QuadrupedRig.Q.Jaw, QuadrupedRig.Q.Head, headP + new Vector3(0, -girth * 0.25f, girth * 0.3f));
            Vector3 t0 = pelvis + new Vector3(0, girth * 0.3f, -girth * 0.9f);
            float tl = bear ? 0.08f : rat ? 0.25f : 0.2f;
            D(QuadrupedRig.Q.Tail0, QuadrupedRig.Q.Pelvis, t0);
            D(QuadrupedRig.Q.Tail1, QuadrupedRig.Q.Tail0, t0 + new Vector3(0, -tl * 0.5f, -tl));
            D(QuadrupedRig.Q.Tail2, QuadrupedRig.Q.Tail1, t0 + new Vector3(0, -tl * 1.2f, -tl * 1.9f));
            float lx = girth * 0.62f;
            void Leg(QuadrupedRig.Q u, QuadrupedRig.Q l, QuadrupedRig.Q p, QuadrupedRig.Q parent, Vector3 top, bool front)
            {
                D(u, parent, top);
                Vector3 knee = new Vector3(top.x, legLen * 0.5f, top.z + (front ? -0.03f : 0.05f));
                D(l, u, knee);
                D(p, l, new Vector3(top.x, 0.05f * (bear ? 1.5f : 1f), top.z + (front ? 0.01f : -0.02f)));
            }
            Leg(QuadrupedRig.Q.FLu, QuadrupedRig.Q.FLl, QuadrupedRig.Q.FLp, QuadrupedRig.Q.Chest, new Vector3(-lx, chest.y - girth * 0.5f, chest.z + 0.02f), true);
            Leg(QuadrupedRig.Q.FRu, QuadrupedRig.Q.FRl, QuadrupedRig.Q.FRp, QuadrupedRig.Q.Chest, new Vector3(lx, chest.y - girth * 0.5f, chest.z + 0.02f), true);
            Leg(QuadrupedRig.Q.BLu, QuadrupedRig.Q.BLl, QuadrupedRig.Q.BLp, QuadrupedRig.Q.Pelvis, new Vector3(-lx, pelvis.y - girth * 0.4f, pelvis.z), false);
            Leg(QuadrupedRig.Q.BRu, QuadrupedRig.Q.BRl, QuadrupedRig.Q.BRp, QuadrupedRig.Q.Pelvis, new Vector3(lx, pelvis.y - girth * 0.4f, pelvis.z), false);

            for (int i = 0; i < pos.Length; i++) rig.bones[i] = new GameObject(((QuadrupedRig.Q)i).ToString()).transform;
            for (int i = 0; i < pos.Length; i++) rig.bones[i].SetParent(par[i] < 0 ? model : rig.bones[par[i]], false);
            for (int i = 0; i < pos.Length; i++) { rig.bones[i].position = model.TransformPoint(pos[i]); rig.bindPos[i] = rig.bones[i].localPosition; }

            var mb = new MeshBuilder(5, true);
            // body loft pelvis -> chest
            var rings = new List<MeshBuilder.Ring>();
            int n = 8;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector3 c = Vector3.Lerp(pelvis + Vector3.back * girth * 0.6f, chest + Vector3.forward * girth * 0.5f, t);
                float belly = Mathf.Sin(t * Mathf.PI);
                float rx = girth * (0.8f + 0.2f * belly) * (bear ? 1.1f : 1f) * (t > 0.6f ? 1.08f : 1f);
                float ry = girth * (1.0f + 0.15f * belly) * (t > 0.65f ? 1.15f : 1f);
                if (!bear && !rat)
                {
                    // canine silhouette: narrow, deep through the chest, tucked up at the loin
                    float tuck = Mathf.Exp(-Mathf.Pow((t - 0.3f) / 0.18f, 2f));
                    rx = girth * Mathf.Lerp(0.78f, 0.92f, MathX.Smoothstep(0.35f, 0.85f, t));
                    ry = girth * (1.0f - 0.22f * tuck + 0.28f * MathX.Smoothstep(0.5f, 0.9f, t));
                    c.y -= girth * 0.12f * MathX.Smoothstep(0.55f, 0.9f, t) - girth * 0.1f * tuck;
                }
                var w = t < 0.5f ? MeshBuilder.W(I(QuadrupedRig.Q.Pelvis)) : MeshBuilder.W(I(QuadrupedRig.Q.Pelvis), I(QuadrupedRig.Q.Chest), Mathf.Clamp01((t - 0.3f) / 0.5f));
                if (i == 0) c.y -= girth * 0.05f;
                rings.Add(new MeshBuilder.Ring { center = c, rot = Quaternion.Euler(90, 0, 0), rx = rx, rz = ry, weight = w, radial = a => 1f + 0.05f * Mathf.Sin(a * 7 + t * 5) });
            }
            mb.AddLoft(FUR, rings, 14, true, true);
            // neck
            var neck = new List<MeshBuilder.Ring>
            {
                new MeshBuilder.Ring { center = chest + new Vector3(0, girth * 0.1f, girth * 0.3f), rot = Quaternion.FromToRotation(Vector3.up, (headP - chest).normalized), rx = girth * 0.8f, rz = girth * 0.9f, weight = MeshBuilder.W(I(QuadrupedRig.Q.Chest)) },
                new MeshBuilder.Ring { center = Vector3.Lerp(chest, headP, 0.55f), rot = Quaternion.FromToRotation(Vector3.up, (headP - chest).normalized), rx = girth * 0.62f, rz = girth * 0.7f, weight = MeshBuilder.W(I(QuadrupedRig.Q.Chest), I(QuadrupedRig.Q.Neck), 0.7f), radial = a => 1f + (bear ? 0.1f : 0.18f) * Mathf.Max(0, Mathf.Sin(a)) },
                new MeshBuilder.Ring { center = headP + new Vector3(0, -girth * 0.1f, -girth * 0.2f), rot = Quaternion.FromToRotation(Vector3.up, (headP - chest).normalized), rx = girth * 0.5f, rz = girth * 0.55f, weight = MeshBuilder.W(I(QuadrupedRig.Q.Neck), I(QuadrupedRig.Q.Head), 0.6f) },
            };
            mb.AddLoft(FUR, neck, 12, false, false);
            if (!bear && !rat)
            {
                // ruff: a thick collar of fur where the neck meets the chest
                mb.bone = I(QuadrupedRig.Q.Chest); mb.bone2 = I(QuadrupedRig.Q.Neck); mb.bone2Weight = 0.4f;
                var ruffC = Vector3.Lerp(chest, headP, 0.3f) + new Vector3(0, -girth * 0.05f, 0);
                mb.AddEllipsoid(FUR, ruffC, new Vector3(girth * 1.0f, girth * 1.12f, girth * 0.8f), 8, 12, p => p * (1f + 0.12f * Mathf.Sin(p.x * 60f + p.y * 40f)));
                mb.bone2 = -1; mb.bone2Weight = 0;
            }
            // head: skull + muzzle
            mb.bone = I(QuadrupedRig.Q.Head); mb.bone2 = -1;
            float hs = girth * (bear ? 0.9f : 1f);
            if (!bear && !rat)
                mb.AddEllipsoid(FUR, headP, new Vector3(hs * 0.6f, hs * 0.5f, hs * 0.7f), 10, 14, p =>
                {
                    // flat brow, broad cheeks at the back, narrowing toward the snout
                    float f = Mathf.Clamp01((p.z / (hs * 0.7f) + 1f) * 0.5f);
                    return new Vector3(p.x * Mathf.Lerp(1.12f, 0.78f, f), Mathf.Min(p.y, hs * 0.36f) - (p.y < 0 ? 0 : 0.0f), p.z);
                });
            else mb.AddEllipsoid(FUR, headP, new Vector3(hs * 0.62f, hs * 0.58f, hs * 0.68f), 10, 14);
            float muzzle = bear ? 0.55f : rat ? 1.0f : 1.25f;
            Vector3 mz = headP + new Vector3(0, -hs * 0.12f, hs * (0.55f + muzzle * 0.5f));
            mb.AddEllipsoid(FUR, mz, new Vector3(hs * 0.32f, hs * 0.28f, hs * muzzle * 0.6f), 8, 12, p => new Vector3(p.x * (1 - 0.3f * Mathf.Clamp01(p.z / (hs * muzzle * 0.6f))), p.y, p.z));
            mb.AddEllipsoid(DARK, mz + new Vector3(0, hs * 0.1f, hs * muzzle * 0.58f), new Vector3(hs * 0.1f, hs * 0.08f, hs * 0.06f), 5, 8);
            // eyes
            for (int s = -1; s <= 1; s += 2)
            {
                mb.AddEllipsoid(EYE, headP + new Vector3(s * hs * 0.28f, hs * 0.12f, hs * 0.5f), Vector3.one * hs * 0.075f, 5, 8);
                // ears
                mb.Push();
                mb.Translate(headP + new Vector3(s * hs * 0.3f, hs * 0.45f, -hs * 0.05f));
                mb.Rotate(Quaternion.Euler(-10, 0, -s * 15));
                if (bear) mb.AddEllipsoid(FUR, Vector3.zero, new Vector3(hs * 0.15f, hs * 0.13f, hs * 0.07f), 5, 8);
                else if (rat) mb.AddCylinder(FUR, Vector3.zero, hs * 0.2f, hs * 0.01f, hs * 0.25f, 6, true);
                else { mb.Scale(new Vector3(1f, 1f, 0.42f)); mb.AddCylinder(FUR, Vector3.zero, hs * 0.2f, hs * 0.01f, hs * 0.46f, 4, true); }
                mb.Pop();
            }
            // jaw
            mb.bone = I(QuadrupedRig.Q.Jaw);
            mb.AddEllipsoid(BELLY, mz + new Vector3(0, -hs * 0.2f, -hs * 0.05f), new Vector3(hs * 0.26f, hs * 0.1f, hs * muzzle * 0.5f), 6, 10);
            for (int s = -1; s <= 1; s += 2)
            {
                mb.Push(); mb.Translate(mz + new Vector3(s * hs * 0.14f, -hs * 0.1f, hs * muzzle * 0.35f));
                mb.AddCone(TEETH, Vector3.zero, hs * 0.025f, hs * 0.08f, 5); mb.Pop();
            }
            // tail
            var tail = new List<MeshBuilder.Ring>();
            Vector3[] tp = { t0, pos[I(QuadrupedRig.Q.Tail1)], pos[I(QuadrupedRig.Q.Tail2)], pos[I(QuadrupedRig.Q.Tail2)] + (pos[I(QuadrupedRig.Q.Tail2)] - pos[I(QuadrupedRig.Q.Tail1)]) * 0.7f };
            QuadrupedRig.Q[] tb = { QuadrupedRig.Q.Tail0, QuadrupedRig.Q.Tail1, QuadrupedRig.Q.Tail2, QuadrupedRig.Q.Tail2 };
            for (int i = 0; i < tp.Length; i++)
            {
                Vector3 dir = i < tp.Length - 1 ? tp[i + 1] - tp[i] : tp[i] - tp[i - 1];
                float tr = rat ? Mathf.Lerp(0.03f, 0.005f, i / 3f) : bear ? Mathf.Lerp(girth * 0.3f, girth * (i == 3 ? 0.1f : 0.35f), i / 3f) : (i == 0 ? girth * 0.3f : i == 3 ? girth * 0.12f : girth * (0.45f + 0.08f * i));
                tail.Add(new MeshBuilder.Ring { center = tp[i], rot = Quaternion.FromToRotation(Vector3.up, dir.normalized), rx = tr, rz = tr, weight = MeshBuilder.W(I(tb[i])) });
            }
            mb.AddLoft(rat ? BELLY : FUR, tail, 8, true, true);
            // legs
            void LegMesh(QuadrupedRig.Q u, QuadrupedRig.Q l, QuadrupedRig.Q p, bool front)
            {
                Vector3 a = pos[I(u)], b = pos[I(l)], c = pos[I(p)];
                float thick = girth * (bear ? 0.5f : 0.36f) * (front ? 1f : 1.2f);
                var lr = new List<MeshBuilder.Ring>
                {
                    new MeshBuilder.Ring { center = a + Vector3.up * girth * 0.3f, rot = Quaternion.identity, rx = thick * 1.2f, rz = thick * 1.5f, weight = MeshBuilder.W(front ? I(QuadrupedRig.Q.Chest) : I(QuadrupedRig.Q.Pelvis), I(u), 0.6f) },
                    new MeshBuilder.Ring { center = Vector3.Lerp(a, b, 0.4f), rot = Quaternion.identity, rx = thick, rz = thick * 1.3f, weight = MeshBuilder.W(I(u)) },
                    new MeshBuilder.Ring { center = b, rot = Quaternion.identity, rx = thick * 0.62f, rz = thick * 0.7f, weight = MeshBuilder.W(I(u), I(l), 0.5f) },
                    new MeshBuilder.Ring { center = Vector3.Lerp(b, c, 0.7f), rot = Quaternion.identity, rx = thick * (bear ? 0.45f : 0.36f), rz = thick * (bear ? 0.5f : 0.42f), weight = MeshBuilder.W(I(l)) },
                    new MeshBuilder.Ring { center = c + Vector3.up * 0.02f, rot = Quaternion.identity, rx = thick * 0.5f, rz = thick * 0.55f, weight = MeshBuilder.W(I(l), I(p), 0.6f) },
                };
                mb.AddLoft(FUR, lr, 10, false, false);
                mb.bone = I(p);
                mb.AddEllipsoid(FUR, c + new Vector3(0, -0.01f, thick * 0.35f), new Vector3(thick * 0.6f, thick * 0.35f, thick * 0.85f), 6, 8, q => new Vector3(q.x, Mathf.Max(q.y, -0.035f), q.z));
            }
            LegMesh(QuadrupedRig.Q.FLu, QuadrupedRig.Q.FLl, QuadrupedRig.Q.FLp, true);
            LegMesh(QuadrupedRig.Q.FRu, QuadrupedRig.Q.FRl, QuadrupedRig.Q.FRp, true);
            LegMesh(QuadrupedRig.Q.BLu, QuadrupedRig.Q.BLl, QuadrupedRig.Q.BLp, false);
            LegMesh(QuadrupedRig.Q.BRu, QuadrupedRig.Q.BRl, QuadrupedRig.Q.BRp, false);

            var mesh = mb.Build(name);
            var bind = new Matrix4x4[(int)QuadrupedRig.Q.COUNT];
            for (int i = 0; i < bind.Length; i++) bind[i] = rig.bones[i].worldToLocalMatrix * model.localToWorldMatrix;
            mesh.bindposes = bind;
            var smrGo = new GameObject("Body"); smrGo.transform.SetParent(model, false);
            var smr = smrGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh; smr.bones = rig.bones; smr.rootBone = rig.bones[I(QuadrupedRig.Q.Pelvis)];
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 3f);
            Color bellyC = Color.Lerp(fur, new Color(.75f, .7f, .62f), rat ? 0.2f : 0.35f);
            smr.sharedMaterials = new[]
            {
                MatLib.Lit(fur, TexId.Hair, .25f, 0, 2.5f, 1.2f),
                MatLib.Lit(bellyC, TexId.Hair, .25f, 0, 2.5f, 1f),
                MatLib.Lit(new Color(.05f, .04f, .04f), null, .5f),
                MatLib.Emissive(new Color(.9f, .75f, .2f), new Color(1f, .6f, .1f) * (kind == BodyKind.DireWolf ? 2.5f : 0.6f)),
                MatLib.Lit(new Color(.9f, .88f, .8f), null, .5f),
            };
            rig.renderers.Add(smr);
            rig.mouth = rig.bones[I(QuadrupedRig.Q.Jaw)];
            return rig;
        }
    }

    [DefaultExecutionOrder(100)]
    public class QuadrupedAnimator : MonoBehaviour, IAnimDriver
    {
        QuadrupedRig rig;
        Vector3 lastPos; float speed, phase, t, seed;
        AnimAct act; float actT, actDur, impactAt; Action onImpact; bool fired;
        LifeState life = LifeState.Alive; float lifeT = 1;
        readonly Vector3[] rot = new Vector3[(int)QuadrupedRig.Q.COUNT];
        Vector3 pelvisOff;
        public bool Busy => act != AnimAct.None;
        public LifeState Life => life;
        public float Height => rig ? rig.height : 0.8f;

        public void Init(QuadrupedRig r) { rig = r; lastPos = transform.position; seed = UnityEngine.Random.value * 10; }
        public void Play(AnimAct a, Action impact = null, float sp = 1)
        {
            if (life != LifeState.Alive) { impact?.Invoke(); return; }
            if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); }
            act = a; actT = 0; actDur = a == AnimAct.Hit ? 0.4f : a == AnimAct.Roar ? 1.6f : 0.8f; impactAt = actDur * 0.45f; onImpact = impact; fired = false;
        }
        public void SetCombat(bool on) { }
        public void SetLife(LifeState s, bool instant = false) { if (s == life) return; life = s; lifeT = instant ? 1 : 0; act = AnimAct.None; if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); } }
        public void LookAt(Vector3? p) { }
        public void SetTalking(bool on) { }

        public bool manual;

        void LateUpdate() { if (!manual) Tick(Time.deltaTime); }

        public void Tick(float dt)
        {
            if (!rig) return;
            if (dt <= 0) return;
            t += dt;
            Vector3 v = (transform.position - lastPos) / dt; v.y = 0; lastPos = transform.position;
            float sp = v.magnitude; if (sp > 25) sp = 0;
            speed = Mathf.Lerp(speed, sp, 1 - Mathf.Exp(-dt * 8));
            lifeT = Mathf.Min(1, lifeT + dt / 0.9f);
            if (act != AnimAct.None)
            {
                actT += dt;
                if (!fired && actT >= impactAt) { fired = true; var c = onImpact; onImpact = null; c?.Invoke(); }
                if (actT >= actDur) act = AnimAct.None;
            }
            for (int i = 0; i < rot.Length; i++) rot[i] = Vector3.zero;
            pelvisOff = Vector3.zero;
            float scale = rig.model.localScale.x;
            // idle breathing
            rot[(int)QuadrupedRig.Q.Chest].x += Mathf.Sin(t * 1.6f + seed) * 1.5f;
            rot[(int)QuadrupedRig.Q.Head].y += Mathf.Sin(t * 0.4f + seed) * 10f;
            rot[(int)QuadrupedRig.Q.Tail0].y += Mathf.Sin(t * 1.3f + seed) * 12f;
            rot[(int)QuadrupedRig.Q.Tail1].y += Mathf.Sin(t * 1.3f + seed - 0.6f) * 15f;
            rot[(int)QuadrupedRig.Q.Tail0].x += 20;
            // gait
            float mw = Mathf.Clamp01(speed / 0.4f);
            if (mw > 0.01f && life == LifeState.Alive)
            {
                float stride = rig.length * 1.6f;
                phase += speed * dt / stride;
                float ph = phase * Mathf.PI * 2;
                float run = Mathf.Clamp01((speed - 3f) / 3f);
                float amp = Mathf.Lerp(28, 45, run) * mw;
                void LegA(QuadrupedRig.Q u, QuadrupedRig.Q l, QuadrupedRig.Q p, float off, bool front)
                {
                    float s = Mathf.Sin(ph + off), c = Mathf.Cos(ph + off);
                    rot[(int)u].x += -amp * s;
                    float lift = Mathf.Max(0, c);
                    rot[(int)l].x += (front ? -1 : 1) * lift * 55 * mw;
                    rot[(int)p].x += (front ? 1 : -1) * lift * 25 * mw;
                }
                LegA(QuadrupedRig.Q.FLu, QuadrupedRig.Q.FLl, QuadrupedRig.Q.FLp, 0, true);
                LegA(QuadrupedRig.Q.BRu, QuadrupedRig.Q.BRl, QuadrupedRig.Q.BRp, run > 0.5f ? 0.8f : 0, false);
                LegA(QuadrupedRig.Q.FRu, QuadrupedRig.Q.FRl, QuadrupedRig.Q.FRp, Mathf.PI, true);
                LegA(QuadrupedRig.Q.BLu, QuadrupedRig.Q.BLl, QuadrupedRig.Q.BLp, Mathf.PI + (run > 0.5f ? 0.8f : 0), false);
                pelvisOff.y += Mathf.Abs(Mathf.Sin(ph)) * 0.03f * mw * (1 + run);
                rot[(int)QuadrupedRig.Q.Chest].x += Mathf.Sin(ph * 2) * 3 * mw;
                rot[(int)QuadrupedRig.Q.Head].x += Mathf.Sin(ph * 2 + 1) * 5 * mw - 6 * run;
                rot[(int)QuadrupedRig.Q.Tail0].x += -10 * run;
            }
            // actions
            if (act != AnimAct.None && life == LifeState.Alive)
            {
                float u = actT / actDur;
                switch (act)
                {
                    case AnimAct.Hit:
                        { float w = Anim.Env(u, 0, .2f, .3f, 1f); rot[(int)QuadrupedRig.Q.Chest].x += -12 * w; rot[(int)QuadrupedRig.Q.Head].x -= 20 * w; pelvisOff.z -= 0.08f * w; break; }
                    case AnimAct.Roar:
                        { float w = Anim.Env(u, 0, .3f, .8f, 1f); rot[(int)QuadrupedRig.Q.Neck].x -= 45 * w; rot[(int)QuadrupedRig.Q.Head].x -= 25 * w; rot[(int)QuadrupedRig.Q.Jaw].x += 25 * w; break; }
                    default:
                        {
                            float wind = Anim.Env(u, 0, .35f, .38f, .48f), strike = Anim.Env(u, .38f, .48f, .6f, 1f);
                            rot[(int)QuadrupedRig.Q.Chest].x += 10 * wind - 12 * strike;
                            rot[(int)QuadrupedRig.Q.Neck].x += -20 * wind + 25 * strike;
                            rot[(int)QuadrupedRig.Q.Jaw].x += 35 * (wind + strike * 0.3f);
                            rot[(int)QuadrupedRig.Q.FLu].x -= 30 * strike; rot[(int)QuadrupedRig.Q.FRu].x -= 30 * strike;
                            pelvisOff.z += 0.35f * strike * rig.length / scale; pelvisOff.y -= 0.05f * wind;
                            if (act == AnimAct.Claw) { rot[(int)QuadrupedRig.Q.FLu].x -= 60 * strike; rot[(int)QuadrupedRig.Q.Chest].x -= 25 * strike; }
                            break;
                        }
                }
            }
            // death: roll onto side
            float roll = 0, drop = 0;
            if (life != LifeState.Alive || lifeT < 1)
            {
                float k = life == LifeState.Alive ? 1 - lifeT : lifeT;
                float f = Anim.Smooth(0, 1, k);
                roll = 85 * f; drop = 0;
                foreach (var q in new[] { QuadrupedRig.Q.FLu, QuadrupedRig.Q.FRu, QuadrupedRig.Q.BLu, QuadrupedRig.Q.BRu }) rot[(int)q].x += -15 * f;
                rot[(int)QuadrupedRig.Q.Head].x += 20 * f; rot[(int)QuadrupedRig.Q.Jaw].x += 15 * f;
            }
            for (int i = 1; i < rot.Length; i++) rig.bones[i].localRotation = Quaternion.Euler(rot[i]);
            rig.bones[(int)QuadrupedRig.Q.Pelvis].localPosition = rig.bindPos[(int)QuadrupedRig.Q.Pelvis] + pelvisOff;
            rig.model.localRotation = Quaternion.Euler(0, 0, roll);
            rig.model.localPosition = new Vector3(0, roll > 0 ? (roll / 85f) * rig.height * 0.1f : 0, 0);
        }
    }

    /// <summary>Swarms of bats: many little flapping bodies around a moving centre.</summary>
    public class SwarmAnimator : MonoBehaviour, IAnimDriver
    {
        readonly List<Transform> bats = new List<Transform>();
        readonly List<Vector3> seeds = new List<Vector3>();
        LifeState life; float lifeT; AnimAct act; float actT; Action onImpact; bool fired;
        public bool Busy => act != AnimAct.None;
        public LifeState Life => life;
        public float Height => 1.6f;
        public List<Renderer> renderers = new List<Renderer>();

        public static SwarmAnimator Build(string name, int count, Color c)
        {
            var go = new GameObject(name);
            var sw = go.AddComponent<SwarmAnimator>();
            var mb = new MeshBuilder(1);
            mb.AddEllipsoid(0, Vector3.zero, new Vector3(0.03f, 0.03f, 0.05f), 4, 6);
            for (int s = -1; s <= 1; s += 2)
            {
                mb.AddQuad(0, new Vector3(0, 0, 0.03f), new Vector3(s * 0.14f, 0.02f, 0.0f), new Vector3(s * 0.12f, 0, -0.05f), new Vector3(0, 0, -0.03f), false);
                mb.AddQuad(0, new Vector3(0, 0, -0.03f), new Vector3(s * 0.12f, 0, -0.05f), new Vector3(s * 0.14f, 0.02f, 0.0f), new Vector3(0, 0, 0.03f), false);
            }
            var mesh = mb.Build("bat");
            var mat = MatLib.Lit(c, null, .2f);
            var rnd = new System.Random(name.GetHashCode());
            for (int i = 0; i < count; i++)
            {
                var b = new GameObject("bat").transform;
                b.SetParent(go.transform, false);
                b.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = b.gameObject.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; sw.renderers.Add(r);
                sw.bats.Add(b);
                sw.seeds.Add(new Vector3((float)rnd.NextDouble() * 10, (float)rnd.NextDouble() * 10, 0.6f + (float)rnd.NextDouble() * 0.8f));
            }
            return sw;
        }

        public void Play(AnimAct a, Action impact = null, float sp = 1) { act = a; actT = 0; onImpact = impact; fired = false; }
        public void SetCombat(bool on) { }
        public void SetLife(LifeState s, bool instant = false) { life = s; lifeT = instant ? 1 : 0; if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); } }
        public void LookAt(Vector3? p) { }
        public void SetTalking(bool on) { }

        void Update()
        {
            float t = Time.time; float dt = Time.deltaTime;
            if (act != AnimAct.None) { actT += dt; if (!fired && actT > 0.35f) { fired = true; var c = onImpact; onImpact = null; c?.Invoke(); } if (actT > 0.7f) act = AnimAct.None; }
            lifeT = Mathf.Min(1, lifeT + dt);
            float lunge = act != AnimAct.None ? Anim.Env(actT / 0.7f, 0, .4f, .5f, 1f) : 0;
            for (int i = 0; i < bats.Count; i++)
            {
                var s = seeds[i]; var b = bats[i];
                if (life == LifeState.Dead)
                {
                    b.localPosition = Vector3.Lerp(b.localPosition, new Vector3(b.localPosition.x, 0.02f, b.localPosition.z), dt * 3);
                    b.localScale = Vector3.one * (1 - lifeT);
                    continue;
                }
                float r = s.z * (1 - lunge * 0.4f);
                Vector3 p = new Vector3(Mathf.Sin(t * (1.3f + s.x * 0.1f) + s.x) * r, 1.0f + Mathf.Sin(t * 2.1f + s.y) * 0.45f, Mathf.Cos(t * (1.1f + s.y * 0.1f) + s.y) * r + lunge * 1.2f);
                b.localPosition = p;
                Vector3 vel = new Vector3(Mathf.Cos(t * 1.3f + s.x), 0, -Mathf.Sin(t * 1.1f + s.y));
                b.localRotation = Quaternion.LookRotation(vel.normalized + Vector3.up * 0.01f) * Quaternion.Euler(0, 0, Mathf.Sin(t * 30 + s.x * 5) * 35);
                b.localScale = new Vector3(1, 1 + Mathf.Sin(t * 30 + s.x * 5) * 0.4f, 1);
            }
        }
    }

    /// <summary>Lorghoth: a heaving mound of vines and rot with two crushing vine-arms.</summary>
    public class MoundAnimator : MonoBehaviour, IAnimDriver
    {
        Transform body, armL, armR; float t; LifeState life; float lifeT = 1; AnimAct act; float actT; Action onImpact; bool fired; Vector3 baseScale;
        public bool Busy => act != AnimAct.None;
        public LifeState Life => life;
        public float Height => 2.4f;
        public List<Renderer> renderers = new List<Renderer>();

        public static MoundAnimator Build(string name, Color c, float scale)
        {
            var go = new GameObject(name);
            var m = go.AddComponent<MoundAnimator>();
            m.body = new GameObject("Mound").transform; m.body.SetParent(go.transform, false);
            var mb = new MeshBuilder(3);
            mb.AddEllipsoid(0, new Vector3(0, 0.95f, 0), new Vector3(0.95f, 0.9f, 0.85f), 14, 18, p => p * (1f + 0.12f * Mathf.Sin(p.x * 9) * Mathf.Sin(p.y * 7 + p.z * 5)));
            var rnd = new System.Random(7);
            for (int i = 0; i < 26; i++)
            {
                var pts = new List<Vector3>(); var rr = new List<float>();
                float a0 = (float)rnd.NextDouble() * Mathf.PI * 2; float e0 = (float)rnd.NextDouble() * 1.4f - 0.3f;
                for (int k = 0; k < 7; k++)
                {
                    float a = a0 + k * 0.35f; float e = e0 - k * 0.12f;
                    float r = 0.98f + 0.05f * Mathf.Sin(k);
                    pts.Add(new Vector3(Mathf.Cos(a) * Mathf.Cos(e) * r, 0.95f + Mathf.Sin(e) * 0.9f * r, Mathf.Sin(a) * Mathf.Cos(e) * r * 0.9f));
                    rr.Add(0.05f + 0.03f * (float)rnd.NextDouble());
                }
                mb.AddTube(i % 3 == 0 ? 1 : 0, pts, rr, 6);
            }
            for (int i = 0; i < 10; i++) mb.AddEllipsoid(2, new Vector3((float)rnd.NextDouble() * 1.4f - 0.7f, 1.2f + (float)rnd.NextDouble() * 0.6f, (float)rnd.NextDouble() * 0.6f + 0.3f), Vector3.one * 0.07f, 4, 6);
            m.body.gameObject.AddComponent<MeshFilter>().sharedMesh = mb.Build("mound");
            var mr = m.body.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { MatLib.Lit(c, TexId.Moss, .35f, 0, 2f, 1.2f), MatLib.Lit(new Color(.25f, .2f, .12f), TexId.Bark, .2f, 0, 1f, 1f), MatLib.Emissive(new Color(.8f, .9f, .3f), new Color(.8f, 1f, .2f) * 1.5f) };
            m.renderers.Add(mr);
            Transform Arm(int s)
            {
                var a = new GameObject(s < 0 ? "ArmL" : "ArmR").transform; a.SetParent(m.body, false); a.localPosition = new Vector3(s * 0.8f, 1.35f, 0.1f);
                var amb = new MeshBuilder(1);
                var pts = new List<Vector3> { Vector3.zero, new Vector3(s * 0.25f, -0.3f, 0.2f), new Vector3(s * 0.3f, -0.75f, 0.3f), new Vector3(s * 0.25f, -1.1f, 0.35f) };
                amb.AddTube(0, pts, new List<float> { 0.2f, 0.18f, 0.16f, 0.22f }, 8);
                a.gameObject.AddComponent<MeshFilter>().sharedMesh = amb.Build("arm");
                var ar = a.gameObject.AddComponent<MeshRenderer>(); ar.sharedMaterial = MatLib.Lit(c * 0.8f, TexId.Moss, .3f, 0, 2f, 1f); m.renderers.Add(ar);
                return a;
            }
            m.armL = Arm(-1); m.armR = Arm(1);
            go.transform.localScale = Vector3.one * scale;
            m.baseScale = Vector3.one;
            return m;
        }

        public void Play(AnimAct a, Action impact = null, float sp = 1) { if (life != LifeState.Alive) { impact?.Invoke(); return; } act = a; actT = 0; onImpact = impact; fired = false; }
        public void SetCombat(bool on) { }
        public void SetLife(LifeState s, bool instant = false) { life = s; lifeT = instant ? 1 : 0; if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); } }
        public void LookAt(Vector3? p) { }
        public void SetTalking(bool on) { }

        void Update()
        {
            float dt = Time.deltaTime; t += dt; lifeT = Mathf.Min(1, lifeT + dt / 2f);
            if (act != AnimAct.None) { actT += dt; if (!fired && actT > 0.5f) { fired = true; var c = onImpact; onImpact = null; c?.Invoke(); } if (actT > 1.1f) act = AnimAct.None; }
            float breathe = 1 + Mathf.Sin(t * 1.3f) * 0.04f;
            float u = act != AnimAct.None ? actT / 1.1f : 0;
            float raise = Anim.Env(u, 0, .4f, .45f, .55f), slam = Anim.Env(u, .45f, .55f, .65f, 1f);
            body.localScale = new Vector3(breathe, 1 / breathe, breathe);
            body.localRotation = Quaternion.Euler(-10 * raise + 18 * slam, Mathf.Sin(t * 0.5f) * 6, 0);
            armL.localRotation = Quaternion.Euler(-140 * raise + 40 * slam + Mathf.Sin(t * 1.1f) * 8, 0, 0);
            armR.localRotation = Quaternion.Euler(-140 * raise + 40 * slam + Mathf.Sin(t * 1.2f + 1) * 8, 0, 0);
            if (life == LifeState.Dead)
            {
                float k = Anim.Smooth(0, 1, lifeT);
                body.localScale = new Vector3(1 + k * 0.4f, 1 - k * 0.75f, 1 + k * 0.4f);
            }
        }
    }

    /// <summary>A grick: segmented worm with a tentacled beak; also used for the animated broom.</summary>
    public class SimpleBodyAnimator : MonoBehaviour, IAnimDriver
    {
        public Transform pivot; public bool floaty; public float height = 1f;
        float t; LifeState life; float lifeT = 1; AnimAct act; float actT; Action onImpact; bool fired;
        public List<Renderer> renderers = new List<Renderer>();
        public bool Busy => act != AnimAct.None;
        public LifeState Life => life;
        public float Height => height;

        public static SimpleBodyAnimator BuildGrick(string name, Color c)
        {
            var go = new GameObject(name); var a = go.AddComponent<SimpleBodyAnimator>(); a.height = 1.1f;
            a.pivot = new GameObject("Pivot").transform; a.pivot.SetParent(go.transform, false);
            var mb = new MeshBuilder(3);
            var pts = new List<Vector3>(); var rr = new List<float>();
            for (int i = 0; i < 10; i++) { float k = i / 9f; pts.Add(new Vector3(0, 0.12f + Mathf.Sin(k * 2.2f) * 0.75f, -0.9f + k * 1.2f - Mathf.Pow(k, 3) * 0.6f)); rr.Add(Mathf.Lerp(0.14f, 0.22f, Mathf.Sin(k * Mathf.PI)) * (1 + 0.1f * Mathf.Sin(i * 2.5f))); }
            mb.AddTube(0, pts, rr, 10);
            Vector3 head = pts[pts.Count - 1];
            mb.AddCone(1, head + new Vector3(0, 0.05f, 0.04f), 0.09f, 0.18f, 6);
            for (int k = 0; k < 4; k++)
            {
                float ang = k * Mathf.PI * 0.5f + Mathf.PI / 4;
                var tp = new List<Vector3>(); var tr = new List<float>();
                for (int j = 0; j < 6; j++) { float q = j / 5f; tp.Add(head + new Vector3(Mathf.Cos(ang) * (0.12f + q * 0.35f), 0.08f + Mathf.Sin(ang) * (0.12f + q * 0.35f) + q * 0.2f, 0.05f + q * 0.1f)); tr.Add(Mathf.Lerp(0.05f, 0.012f, q)); }
                mb.AddTube(2, tp, tr, 6);
            }
            a.pivot.gameObject.AddComponent<MeshFilter>().sharedMesh = mb.Build("grick");
            var r = a.pivot.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterials = new[] { MatLib.Lit(c, TexId.Rock, .4f, 0, 1f, 1.5f), MatLib.Lit(new Color(.15f, .13f, .1f), null, .6f), MatLib.Lit(c * 0.8f, TexId.Leather, .5f, 0, 1f, 1f) };
            a.renderers.Add(r);
            return a;
        }

        public static SimpleBodyAnimator BuildBroom(string name)
        {
            var go = new GameObject(name); var a = go.AddComponent<SimpleBodyAnimator>(); a.floaty = true; a.height = 1.2f;
            a.pivot = new GameObject("Pivot").transform; a.pivot.SetParent(go.transform, false); a.pivot.localPosition = new Vector3(0, 0.5f, 0);
            var mb = new MeshBuilder(2);
            mb.AddCylinder(0, new Vector3(0, 0, 0), 0.018f, 0.018f, 1.2f, 8);
            mb.AddCylinder(1, new Vector3(0, -0.35f, 0), 0.14f, 0.04f, 0.38f, 12);
            mb.AddCylinder(0, new Vector3(0, -0.02f, 0), 0.05f, 0.05f, 0.05f, 8);
            a.pivot.gameObject.AddComponent<MeshFilter>().sharedMesh = mb.Build("broom");
            var r = a.pivot.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterials = new[] { MatLib.Lit(new Color(.4f, .28f, .16f), TexId.WoodPlank, .3f, 0, .5f, 1f), MatLib.Lit(new Color(.62f, .52f, .32f), TexId.Thatch, .2f, 0, 1f, 1f) };
            a.renderers.Add(r);
            return a;
        }

        public void Play(AnimAct act0, Action impact = null, float sp = 1) { if (life != LifeState.Alive) { impact?.Invoke(); return; } act = act0; actT = 0; onImpact = impact; fired = false; }
        public void SetCombat(bool on) { }
        public void SetLife(LifeState s, bool instant = false) { life = s; lifeT = instant ? 1 : 0; if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); } }
        public void LookAt(Vector3? p) { }
        public void SetTalking(bool on) { }

        void Update()
        {
            float dt = Time.deltaTime; t += dt; lifeT = Mathf.Min(1, lifeT + dt);
            if (act != AnimAct.None) { actT += dt; if (!fired && actT > 0.4f) { fired = true; var c = onImpact; onImpact = null; c?.Invoke(); } if (actT > 0.8f) act = AnimAct.None; }
            float u = act != AnimAct.None ? actT / 0.8f : 0;
            float wind = Anim.Env(u, 0, .4f, .45f, .5f), strike = Anim.Env(u, .45f, .5f, .6f, 1f);
            if (floaty)
            {
                pivot.localPosition = new Vector3(0, 0.55f + Mathf.Sin(t * 2.2f) * 0.1f, strike * 0.4f);
                pivot.localRotation = Quaternion.Euler(-60 * wind + 100 * strike + Mathf.Sin(t * 1.7f) * 8, t * 20 % 360 * 0, Mathf.Sin(t * 1.3f) * 10);
            }
            else
            {
                pivot.localRotation = Quaternion.Euler(-25 * wind + 30 * strike + Mathf.Sin(t * 1.4f) * 3, Mathf.Sin(t * 0.7f) * 10, Mathf.Sin(t * 1.1f) * 4);
                pivot.localPosition = new Vector3(0, 0, strike * 0.3f);
            }
            if (life == LifeState.Dead)
            {
                float k = Anim.Smooth(0, 1, lifeT);
                pivot.localRotation = Quaternion.Slerp(pivot.localRotation, Quaternion.Euler(floaty ? 90 : 0, 0, floaty ? 0 : 90), k);
                pivot.localPosition = Vector3.Lerp(pivot.localPosition, new Vector3(0, floaty ? 0.05f : 0, 0), k);
            }
        }
    }
}
