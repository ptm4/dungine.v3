using System;
using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>
    /// Skeleton of a four-legged beast built on real anatomy: a flexible back (pelvis, loin, chest), a two-piece neck,
    /// head, jaw and ears, a five-bone tail, front legs from the shoulder blade down (scapula, humerus, forearm,
    /// pastern, paw) and digitigrade hind legs (femur, tibia, metatarsus with the backward-bending hock, paw).
    /// </summary>
    public class BeastRig : MonoBehaviour
    {
        public enum Q
        {
            Root, Pelvis, Loin, Chest, Neck1, Neck2, Head, Jaw, EarL, EarR, Tail0, Tail1, Tail2, Tail3, Tail4,
            ScapL, HumL, ForeL, CarpL, PawFL, ScapR, HumR, ForeR, CarpR, PawFR,
            FemL, TibL, MetL, PawHL, FemR, TibR, MetR, PawHR, COUNT
        }
        public Transform[] bones = new Transform[(int)Q.COUNT];
        public Vector3[] bindPos = new Vector3[(int)Q.COUNT];   // local position of each bone under its parent
        public Vector3[] bindModel = new Vector3[(int)Q.COUNT]; // model-space rest position (unscaled)
        public Transform model, mouth;
        public float height, length, scale;
        public BodyKind kind;
        public List<Renderer> renderers = new List<Renderer>();
        public Transform this[Q q] => bones[(int)q];
    }

    /// <summary>Procedural beasts (wolves, dire wolves, bears, giant rats), sculpted from anatomical landmarks.</summary>
    public static class BeastBuilder
    {
        public const int FUR = 0, BELLY = 1, DARK = 2, EYE = 3, TEETH = 4, MOUTH = 5, SADDLE = 6, LEG = 7, SUBS = 8;
        static int I(BeastRig.Q q) => (int)q;
        static BoneWeight W(BeastRig.Q a) => MeshBuilder.W(I(a));
        static BoneWeight W(BeastRig.Q a, BeastRig.Q b, float t) => MeshBuilder.W(I(a), I(b), Mathf.Clamp01(t));

        /// <summary>Landmarks in metres for an animal at scale 1 (a grey wolf stands 0.8 m at the withers).</summary>
        public class Dims
        {
            public float[] bz, btop, bbot, bw;           // body profile, rump(-z) .. brisket(+z)
            public Vector3 chest, loin, pelvis, neck1, neck2, head, skull, jaw, earBase;
            public Vector3 scap, shoulder, elbow, carpus, pawF;
            public Vector3 hip, stifle, hock, pawH;
            public Vector3[] tail;
            public Vector3 skullR; public float muzzleLen, muzzleW, muzzleH, earH, earW, pawR, legT;
            public float[] tailR;
            public bool roundEars, heavyRuff, bare;
        }

        public static Dims DimsFor(BodyKind k)
        {
            var d = new Dims
            {
                bz = new[] { -0.56f, -0.48f, -0.38f, -0.25f, -0.12f, 0.01f, 0.13f, 0.23f, 0.31f },
                btop = new[] { 0.63f, 0.7f, 0.72f, 0.735f, 0.75f, 0.78f, 0.8f, 0.78f, 0.7f },
                bbot = new[] { 0.55f, 0.51f, 0.52f, 0.52f, 0.46f, 0.38f, 0.35f, 0.38f, 0.47f },
                bw = new[] { 0.062f, 0.1f, 0.102f, 0.09f, 0.1f, 0.12f, 0.126f, 0.113f, 0.086f },
                pelvis = new Vector3(0, 0.63f, -0.4f), loin = new Vector3(0, 0.64f, -0.18f), chest = new Vector3(0, 0.6f, 0.08f),
                neck1 = new Vector3(0, 0.7f, 0.26f), neck2 = new Vector3(0, 0.79f, 0.36f), head = new Vector3(0, 0.85f, 0.44f),
                skull = new Vector3(0, 0.87f, 0.5f), skullR = new Vector3(0.08f, 0.074f, 0.09f),
                jaw = new Vector3(0, 0.825f, 0.49f), earBase = new Vector3(0.05f, 0.935f, 0.47f),
                scap = new Vector3(0.068f, 0.73f, 0.12f), shoulder = new Vector3(0.09f, 0.56f, 0.24f), elbow = new Vector3(0.086f, 0.38f, 0.17f),
                carpus = new Vector3(0.078f, 0.1f, 0.195f), pawF = new Vector3(0.078f, 0.03f, 0.24f),
                hip = new Vector3(0.074f, 0.6f, -0.39f), stifle = new Vector3(0.094f, 0.41f, -0.26f), hock = new Vector3(0.084f, 0.19f, -0.45f),
                pawH = new Vector3(0.078f, 0.03f, -0.4f),
                tail = new[] { new Vector3(0, 0.665f, -0.55f), new Vector3(0, 0.62f, -0.65f), new Vector3(0, 0.55f, -0.73f), new Vector3(0, 0.47f, -0.79f), new Vector3(0, 0.39f, -0.82f), new Vector3(0, 0.31f, -0.84f) },
                tailR = new[] { 0.03f, 0.05f, 0.056f, 0.05f, 0.036f, 0.012f },
                muzzleLen = 0.12f, muzzleW = 0.05f, muzzleH = 0.046f, earH = 0.09f, earW = 0.038f, pawR = 0.033f, legT = 1f,
            };
            if (k == BodyKind.DireWolf) { d.heavyRuff = true; d.legT = 1.08f; for (int i = 0; i < d.bw.Length; i++) d.bw[i] *= 1.08f; }
            if (k == BodyKind.Bear)
            {
                // heavy, deep and low-slung: shorter legs, a shoulder hump, short muzzle, round ears, stub tail
                for (int i = 0; i < d.bz.Length; i++) { d.bw[i] *= 1.7f; d.btop[i] = Mathf.Lerp(d.btop[i], d.btop[i] + 0.1f, i >= 5 && i <= 6 ? 1 : 0.5f); d.bbot[i] = Mathf.Lerp(d.bbot[i], 0.38f, 0.5f); }
                d.skullR = new Vector3(0.1f, 0.09f, 0.1f); d.muzzleLen = 0.11f; d.muzzleW = 0.06f; d.muzzleH = 0.055f; d.earH = 0.045f; d.earW = 0.035f; d.roundEars = true;
                d.tail = new[] { new Vector3(0, 0.66f, -0.56f), new Vector3(0, 0.63f, -0.6f), new Vector3(0, 0.6f, -0.62f), new Vector3(0, 0.57f, -0.63f), new Vector3(0, 0.55f, -0.635f), new Vector3(0, 0.54f, -0.64f) };
                d.tailR = new[] { 0.04f, 0.05f, 0.045f, 0.035f, 0.02f, 0.01f };
                d.hock = new Vector3(0.1f, 0.1f, -0.47f); d.pawH = new Vector3(0.1f, 0.03f, -0.38f); d.pawR = 0.05f; d.legT = 1.7f;
                d.shoulder.x = d.elbow.x = d.carpus.x = d.pawF.x = 0.13f; d.hip.x = d.stifle.x = 0.12f;
            }
            if (k == BodyKind.Rat)
            {
                // a long low body, pointed snout, round ears and a long bare tail
                for (int i = 0; i < d.bz.Length; i++) { d.btop[i] -= 0.05f; d.bbot[i] = Mathf.Lerp(d.bbot[i], 0.5f, 0.6f); d.bw[i] *= 0.95f; }
                d.muzzleLen = 0.18f; d.muzzleW = 0.04f; d.muzzleH = 0.035f; d.earH = 0.05f; d.earW = 0.04f; d.roundEars = true; d.bare = true;
                d.tail = new[] { new Vector3(0, 0.62f, -0.55f), new Vector3(0, 0.55f, -0.73f), new Vector3(0, 0.45f, -0.91f), new Vector3(0, 0.35f, -1.08f), new Vector3(0, 0.26f, -1.23f), new Vector3(0, 0.2f, -1.35f) };
                d.tailR = new[] { 0.028f, 0.02f, 0.015f, 0.011f, 0.007f, 0.004f };
            }
            if (k == BodyKind.Rat) Squash(d, 0.62f);      // low and hunched
            if (k == BodyKind.Bear) Squash(d, 0.84f);     // heavy, short-legged
            return d;
        }

        /// <summary>Lowers everything above the paws toward the ground: shorter legs, a lower body.</summary>
        static void Squash(Dims d, float k)
        {
            float Y(float y) => 0.03f + (y - 0.03f) * k;
            Vector3 V(Vector3 v) => new Vector3(v.x, Y(v.y), v.z);
            for (int i = 0; i < d.btop.Length; i++) { d.btop[i] = Y(d.btop[i]); d.bbot[i] = Y(d.bbot[i]); }
            d.pelvis = V(d.pelvis); d.loin = V(d.loin); d.chest = V(d.chest); d.neck1 = V(d.neck1); d.neck2 = V(d.neck2);
            d.head = V(d.head); d.skull = V(d.skull); d.jaw = V(d.jaw); d.earBase = V(d.earBase);
            d.scap = V(d.scap); d.shoulder = V(d.shoulder); d.elbow = V(d.elbow); d.carpus = V(d.carpus);
            d.hip = V(d.hip); d.stifle = V(d.stifle); d.hock = V(d.hock);
            for (int i = 0; i < d.tail.Length; i++) d.tail[i] = V(d.tail[i]);
        }

        public static BeastRig Build(BodyKind kind, Color fur, float scale, string name)
        {
            var d = DimsFor(kind);
            var go = new GameObject(name);
            var rig = go.AddComponent<BeastRig>();
            rig.kind = kind; rig.scale = scale;
            rig.height = d.btop[6] * scale; rig.length = (d.skull.z + d.skullR.z + d.muzzleLen - d.tail[0].z) * scale;
            var model = new GameObject("Model").transform; model.SetParent(go.transform, false); rig.model = model;
            model.localScale = Vector3.one * scale;

            // ---------------- skeleton ----------------
            var pos = new Vector3[(int)BeastRig.Q.COUNT];
            var par = new int[(int)BeastRig.Q.COUNT];
            void D(BeastRig.Q q, BeastRig.Q p, Vector3 v) { pos[I(q)] = v; par[I(q)] = I(p); }
            par[0] = -1;
            D(BeastRig.Q.Pelvis, BeastRig.Q.Root, d.pelvis);
            D(BeastRig.Q.Loin, BeastRig.Q.Pelvis, d.loin);
            D(BeastRig.Q.Chest, BeastRig.Q.Loin, d.chest);
            D(BeastRig.Q.Neck1, BeastRig.Q.Chest, d.neck1);
            D(BeastRig.Q.Neck2, BeastRig.Q.Neck1, d.neck2);
            D(BeastRig.Q.Head, BeastRig.Q.Neck2, d.head);
            D(BeastRig.Q.Jaw, BeastRig.Q.Head, d.jaw);
            D(BeastRig.Q.EarL, BeastRig.Q.Head, Mirror(d.earBase));
            D(BeastRig.Q.EarR, BeastRig.Q.Head, d.earBase);
            BeastRig.Q[] tb = { BeastRig.Q.Tail0, BeastRig.Q.Tail1, BeastRig.Q.Tail2, BeastRig.Q.Tail3, BeastRig.Q.Tail4 };
            for (int i = 0; i < tb.Length; i++) D(tb[i], i == 0 ? BeastRig.Q.Pelvis : tb[i - 1], d.tail[i]);
            void FrontLeg(bool right, BeastRig.Q s, BeastRig.Q h, BeastRig.Q f, BeastRig.Q c, BeastRig.Q p)
            {
                Func<Vector3, Vector3> m = v => right ? v : Mirror(v);
                D(s, BeastRig.Q.Chest, m(d.scap)); D(h, s, m(d.shoulder)); D(f, h, m(d.elbow)); D(c, f, m(d.carpus)); D(p, c, m(d.pawF));
            }
            void HindLeg(bool right, BeastRig.Q fe, BeastRig.Q ti, BeastRig.Q me, BeastRig.Q p)
            {
                Func<Vector3, Vector3> m = v => right ? v : Mirror(v);
                D(fe, BeastRig.Q.Pelvis, m(d.hip)); D(ti, fe, m(d.stifle)); D(me, ti, m(d.hock)); D(p, me, m(d.pawH));
            }
            FrontLeg(false, BeastRig.Q.ScapL, BeastRig.Q.HumL, BeastRig.Q.ForeL, BeastRig.Q.CarpL, BeastRig.Q.PawFL);
            FrontLeg(true, BeastRig.Q.ScapR, BeastRig.Q.HumR, BeastRig.Q.ForeR, BeastRig.Q.CarpR, BeastRig.Q.PawFR);
            HindLeg(false, BeastRig.Q.FemL, BeastRig.Q.TibL, BeastRig.Q.MetL, BeastRig.Q.PawHL);
            HindLeg(true, BeastRig.Q.FemR, BeastRig.Q.TibR, BeastRig.Q.MetR, BeastRig.Q.PawHR);

            for (int i = 0; i < pos.Length; i++) rig.bones[i] = new GameObject(((BeastRig.Q)i).ToString()).transform;
            for (int i = 0; i < pos.Length; i++) rig.bones[i].SetParent(par[i] < 0 ? model : rig.bones[par[i]], false);
            for (int i = 0; i < pos.Length; i++) { rig.bones[i].position = model.TransformPoint(pos[i]); rig.bindPos[i] = rig.bones[i].localPosition; rig.bindModel[i] = pos[i]; }

            // ---------------- body ----------------
            var mb = new MeshBuilder(SUBS, true);
            int bodyStart = mb.TriCount(FUR);
            var body = new List<MeshBuilder.Ring>();
            for (int i = 0; i < d.bz.Length; i++)
            {
                float z = d.bz[i];
                float top = d.btop[i], bot = d.bbot[i];
                var c = new Vector3(0, (top + bot) * 0.5f, z);
                float t = Mathf.InverseLerp(d.pelvis.z, d.chest.z, z);
                BoneWeight wgt = z < d.pelvis.z + 0.05f ? W(BeastRig.Q.Pelvis)
                    : z < d.loin.z ? W(BeastRig.Q.Pelvis, BeastRig.Q.Loin, Mathf.InverseLerp(d.pelvis.z + 0.05f, d.loin.z, z))
                    : z < d.chest.z - 0.02f ? W(BeastRig.Q.Loin, BeastRig.Q.Chest, Mathf.InverseLerp(d.loin.z, d.chest.z - 0.02f, z))
                    : W(BeastRig.Q.Chest);
                float keel = i >= 5 ? 0.12f : 0f;    // the ribcage narrows to a keel at the brisket
                body.Add(new MeshBuilder.Ring
                {
                    center = c, rot = Frame(Vector3.forward, Vector3.right), rx = d.bw[i], rz = (top - bot) * 0.5f, weight = wgt,
                    radial = a =>
                    {
                        float sn = Mathf.Sin(a), cs = Mathf.Cos(a);
                        // sides a little flat, the spine a gentle ridge, the underline narrowing
                        float m = 1f - 0.08f * cs * cs;
                        if (sn > 0) m *= 1f - keel * sn * sn;
                        else m *= 1f + 0.04f * sn * sn;
                        return m;
                    }
                });
            }
            mb.AddLoft(FUR, body, 22, true, true);

            // underside lighter, back darker (a wolf's saddle)
            mb.Reassign(FUR, BELLY, bodyStart, (c, n) => n.y < -0.35f && c.y < d.chest.y);
            mb.Reassign(FUR, SADDLE, bodyStart, (c, n) => n.y > 0.55f && c.z < d.chest.z + 0.1f && c.z > d.pelvis.z - 0.12f);

            // ---------------- neck ----------------
            var neckPts = new[] { d.chest + new Vector3(0, 0.06f, 0.1f), d.neck1 + new Vector3(0, 0.03f, 0.02f), d.neck2 + new Vector3(0, 0.02f, 0), d.head + new Vector3(0, 0.0f, 0.03f) };
            float[] nW = { 0.1f, 0.085f, 0.07f, 0.058f }, nH = { 0.15f, 0.115f, 0.09f, 0.072f };
            BoneWeight[] nWt = { W(BeastRig.Q.Chest), W(BeastRig.Q.Neck1, BeastRig.Q.Chest, 0.2f), W(BeastRig.Q.Neck2, BeastRig.Q.Neck1, 0.3f), W(BeastRig.Q.Head, BeastRig.Q.Neck2, 0.3f) };
            float neckK = kind == BodyKind.Bear ? 1.5f : 1f;
            var neck = new List<MeshBuilder.Ring>();
            for (int i = 0; i < neckPts.Length; i++)
            {
                Vector3 dir = (i < neckPts.Length - 1 ? neckPts[i + 1] - neckPts[i] : neckPts[i] - neckPts[i - 1]).normalized;
                neck.Add(new MeshBuilder.Ring { center = neckPts[i], rot = Frame(dir, Vector3.right), rx = nW[i] * neckK, rz = nH[i] * neckK, weight = nWt[i], offsetZ = 0.012f * (i == 1 || i == 2 ? 1 : 0) });
            }
            int neckStart = mb.TriCount(FUR);
            mb.AddLoft(FUR, neck, 16, false, false);
            mb.Reassign(FUR, BELLY, neckStart, (c, n) => n.y < -0.4f);

            // ---------------- head ----------------
            BuildHead(mb, d, kind);

            // ---------------- legs ----------------
            for (int side = -1; side <= 1; side += 2)
            {
                bool r = side > 0;
                Func<Vector3, Vector3> m = v => r ? v : Mirror(v);
                var S = r ? BeastRig.Q.ScapR : BeastRig.Q.ScapL; var H = r ? BeastRig.Q.HumR : BeastRig.Q.HumL; var F = r ? BeastRig.Q.ForeR : BeastRig.Q.ForeL;
                var C = r ? BeastRig.Q.CarpR : BeastRig.Q.CarpL; var P = r ? BeastRig.Q.PawFR : BeastRig.Q.PawFL;
                float k = d.legT;
                // shoulder and upper arm: from high on the chest wall (the shoulder blade's muscle) down to the elbow
                Vector3 scapIn = new Vector3(m(d.scap).x * 0.45f, d.scap.y - 0.02f, d.scap.z - 0.01f);
                Limb(mb, FUR, new[] { scapIn, Vector3.Lerp(scapIn, m(d.shoulder), 0.45f) + new Vector3(side * 0.012f, 0, 0), m(d.shoulder) + new Vector3(0, 0.02f, 0), Vector3.Lerp(m(d.shoulder), m(d.elbow), 0.55f), m(d.elbow) + new Vector3(0, 0.01f, 0) },
                    new[] { 0.02f * k, 0.042f * k, 0.052f * k, 0.044f * k, 0.036f * k }, new[] { 0.05f * k, 0.09f * k, 0.078f * k, 0.06f * k, 0.042f * k },
                    new[] { W(S, BeastRig.Q.Chest, 0.5f), W(S, BeastRig.Q.Chest, 0.25f), W(H, S, 0.3f), W(H), W(H, F, 0.4f) }, true);
                // forearm: muscle high, slender low; the elbow's point at the back
                Limb(mb, LEG, new[] { m(d.elbow) + new Vector3(0, 0.02f, 0), Vector3.Lerp(m(d.elbow), m(d.carpus), 0.3f), Vector3.Lerp(m(d.elbow), m(d.carpus), 0.75f), m(d.carpus) + new Vector3(0, 0.005f, 0) },
                    new[] { 0.034f * k, 0.03f * k, 0.022f * k, 0.02f * k }, new[] { 0.04f * k, 0.036f * k, 0.024f * k, 0.022f * k },
                    new[] { W(F, H, 0.3f), W(F), W(F), W(F, C, 0.5f) });
                mb.AddEllipsoid(LEG, m(d.elbow) + new Vector3(0, 0.005f, -0.03f), new Vector3(0.022f, 0.028f, 0.022f) * k, 5, 8, null, _ => W(F));
                // pastern and paw
                Limb(mb, LEG, new[] { m(d.carpus) + new Vector3(0, 0.012f, 0), m(d.carpus), Vector3.Lerp(m(d.carpus), m(d.pawF), 0.6f) },
                    new[] { 0.021f * k, 0.02f * k, 0.019f * k }, new[] { 0.023f * k, 0.022f * k, 0.02f * k }, new[] { W(C, F, 0.3f), W(C), W(C, P, 0.5f) });
                Paw(mb, m(d.pawF), d.pawR * (kind == BodyKind.Bear ? 1.2f : 1f), P, kind);

                var Fe = r ? BeastRig.Q.FemR : BeastRig.Q.FemL; var Ti = r ? BeastRig.Q.TibR : BeastRig.Q.TibL; var Me = r ? BeastRig.Q.MetR : BeastRig.Q.MetL;
                var PH = r ? BeastRig.Q.PawHR : BeastRig.Q.PawHL;
                // haunch and thigh: from the croup down to the stifle, broad and deep like a real wolf's ham
                Vector3 hipIn = new Vector3(m(d.hip).x * 0.35f, d.hip.y + 0.08f, d.hip.z - 0.03f);
                Limb(mb, FUR, new[] { hipIn, Vector3.Lerp(hipIn, m(d.hip), 0.5f) + new Vector3(side * 0.01f, 0, -0.01f), m(d.hip) + new Vector3(0, -0.01f, -0.012f), Vector3.Lerp(m(d.hip), m(d.stifle), 0.55f) + new Vector3(0, 0, -0.014f), m(d.stifle) + new Vector3(0, 0.012f, 0) },
                    new[] { 0.025f * k, 0.05f * k, 0.064f * k, 0.056f * k, 0.04f * k }, new[] { 0.06f * k, 0.11f * k, 0.118f * k, 0.09f * k, 0.05f * k },
                    new[] { W(BeastRig.Q.Pelvis), W(Fe, BeastRig.Q.Pelvis, 0.5f), W(Fe, BeastRig.Q.Pelvis, 0.2f), W(Fe), W(Fe, Ti, 0.45f) }, true);
                // gaskin: the second thigh, muscle at the back, down to the hock
                Limb(mb, LEG, new[] { m(d.stifle) + new Vector3(0, 0.02f, -0.01f), Vector3.Lerp(m(d.stifle), m(d.hock), 0.35f) + new Vector3(0, 0, -0.012f), Vector3.Lerp(m(d.stifle), m(d.hock), 0.75f), m(d.hock) },
                    new[] { 0.042f * k, 0.036f * k, 0.022f * k, 0.019f * k }, new[] { 0.058f * k, 0.05f * k, 0.028f * k, 0.022f * k },
                    new[] { W(Ti, Fe, 0.3f), W(Ti), W(Ti), W(Ti, Me, 0.5f) });
                mb.AddEllipsoid(LEG, m(d.hock) + new Vector3(0, 0.01f, -0.02f), new Vector3(0.016f, 0.024f, 0.02f) * k, 5, 8, null, _ => W(Ti, Me, 0.5f));
                Limb(mb, LEG, new[] { m(d.hock), Vector3.Lerp(m(d.hock), m(d.pawH), 0.5f), Vector3.Lerp(m(d.hock), m(d.pawH), 0.85f) },
                    new[] { 0.019f * k, 0.018f * k, 0.019f * k }, new[] { 0.022f * k, 0.02f * k, 0.02f * k }, new[] { W(Me, Ti, 0.2f), W(Me), W(Me, PH, 0.5f) });
                Paw(mb, m(d.pawH), d.pawR * (kind == BodyKind.Bear ? 1.2f : 0.95f), PH, kind);
            }

            // ---------------- tail ----------------
            var tail = new List<MeshBuilder.Ring>();
            for (int i = 0; i < d.tail.Length; i++)
            {
                Vector3 dir = (i < d.tail.Length - 1 ? d.tail[i + 1] - d.tail[i] : d.tail[i] - d.tail[i - 1]).normalized;
                var tq = tb[Mathf.Min(i, tb.Length - 1)];
                var wq = i == 0 ? W(BeastRig.Q.Tail0, BeastRig.Q.Pelvis, 0.3f) : i < tb.Length ? W(tb[i], tb[i - 1], 0.25f) : W(tq);
                tail.Add(new MeshBuilder.Ring { center = d.tail[i], rot = Frame(dir, Vector3.right), rx = d.tailR[i], rz = d.tailR[i] * 1.1f, weight = wq, radial = a => 1f + (d.bare ? 0 : 0.07f * Mathf.Sin(a * 7f + i)) });
            }
            mb.AddLoft(d.bare ? BELLY : FUR, tail, 12, true, true);

            // ---------------- fur ----------------
            if (!d.bare) Fur(mb, d, kind);

            var mesh = mb.Build(name);
            var bind = new Matrix4x4[(int)BeastRig.Q.COUNT];
            for (int i = 0; i < bind.Length; i++) bind[i] = rig.bones[i].worldToLocalMatrix * model.localToWorldMatrix;
            mesh.bindposes = bind;
            var smrGo = new GameObject("Body"); smrGo.transform.SetParent(model, false);
            var smr = smrGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh; smr.bones = rig.bones; smr.rootBone = rig.bones[I(BeastRig.Q.Pelvis)];
            smr.localBounds = new Bounds(Vector3.zero, Vector3.one * 2.4f);
            smr.sharedMaterials = Materials(kind, fur);
            rig.renderers.Add(smr);
            rig.mouth = rig.bones[I(BeastRig.Q.Jaw)];
            return rig;
        }

        static Vector3 Mirror(Vector3 v) => new Vector3(-v.x, v.y, v.z);

        /// <summary>A ring frame whose axis runs along dir, with ring-X toward side (ring-Z then points "down/back").</summary>
        static Quaternion Frame(Vector3 dir, Vector3 side)
        {
            Vector3 y = dir.normalized;
            Vector3 x = side - Vector3.Dot(side, y) * y;
            if (x.sqrMagnitude < 1e-5f) x = Vector3.Cross(Vector3.up, y);
            x.Normalize();
            Vector3 z = Vector3.Cross(x, y);
            return Quaternion.LookRotation(z, y);
        }

        static void Limb(MeshBuilder mb, int sub, Vector3[] pts, float[] wide, float[] deep, BoneWeight[] w, bool capTop = false)
        {
            var rings = new List<MeshBuilder.Ring>();
            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 dir = (i < pts.Length - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                rings.Add(new MeshBuilder.Ring { center = pts[i], rot = Frame(dir, Vector3.right), rx = wide[i], rz = deep[i], weight = w[i] });
            }
            mb.AddLoft(sub, rings, 12, capTop, false);
        }

        static void Paw(MeshBuilder mb, Vector3 at, float r, BeastRig.Q bone, BodyKind kind)
        {
            // pad-shaped foot, four toes in front, dark pads and claws beneath
            mb.AddEllipsoid(LEG, at + new Vector3(0, -0.004f, 0.012f), new Vector3(r, r * 0.62f, r * 1.25f), 7, 10, p => new Vector3(p.x, Mathf.Max(p.y, -r * 0.45f), p.z), _ => W(bone));
            for (int t = 0; t < 4; t++)
            {
                float x = (t - 1.5f) * r * 0.48f;
                float z = r * (0.95f - Mathf.Abs(t - 1.5f) * 0.18f);
                Vector3 tc = at + new Vector3(x, -0.01f, z + 0.012f);
                mb.AddEllipsoid(LEG, tc, new Vector3(r * 0.27f, r * 0.3f, r * 0.34f), 5, 7, null, _ => W(bone));
                mb.bone = I(bone); mb.bone2 = -1;
                mb.Push(); mb.Translate(tc + new Vector3(0, -r * 0.1f, r * 0.3f)); mb.Rotate(Quaternion.Euler(70, 0, 0));
                mb.AddCone(DARK, Vector3.zero, r * 0.07f, r * (kind == BodyKind.Bear ? 0.55f : 0.3f), 5);
                mb.Pop();
            }
            mb.AddEllipsoid(DARK, at + new Vector3(0, -r * 0.44f, 0.004f), new Vector3(r * 0.55f, r * 0.12f, r * 0.55f), 4, 8, null, _ => W(bone));
        }

        static void BuildHead(MeshBuilder mb, Dims d, BodyKind kind)
        {
            var HQ = BeastRig.Q.Head;
            Vector3 sk = d.skull;
            Vector3 R = d.skullR;
            // skull: broad behind the eyes, flat on top, cheeks swelling low at the back
            int headStart = mb.TriCount(FUR);
            mb.AddEllipsoid(FUR, sk, R, 12, 16, p =>
            {
                float f = Mathf.Clamp01((p.z / R.z + 1f) * 0.5f);   // 0 back .. 1 front
                float x = p.x * Mathf.Lerp(1.12f, 0.8f, f * f);
                float y = p.y > 0 ? p.y * Mathf.Lerp(0.95f, 0.72f, f) : p.y * Mathf.Lerp(1.05f, 0.85f, f);
                if (p.y < 0 && p.z < 0) x *= 1.08f;                   // jowls / cheek ruff base
                return new Vector3(x, y, p.z);
            }, _ => W(HQ));
            // muzzle: from the stop to the nose, flat-topped, narrowing
            float mzBackZ = sk.z + R.z * 0.45f, mzFrontZ = sk.z + R.z * 0.55f + d.muzzleLen;
            var mz = new List<MeshBuilder.Ring>();
            for (int i = 0; i <= 5; i++)
            {
                float t = i / 5f;
                float z = Mathf.Lerp(mzBackZ, mzFrontZ, t);
                float y = sk.y - R.y * 0.18f - t * 0.02f;
                float w = Mathf.Lerp(d.muzzleW, d.muzzleW * 0.62f, t * t) * (i == 0 ? 1.15f : 1f);
                float h = Mathf.Lerp(d.muzzleH, d.muzzleH * 0.72f, t);
                mz.Add(new MeshBuilder.Ring
                {
                    center = new Vector3(0, y, z), rot = Frame(Vector3.forward, Vector3.right), rx = w, rz = h, weight = W(HQ),
                    radial = a => { float sn = Mathf.Sin(a); return sn < 0 ? Mathf.Lerp(1f, 0.82f, sn * sn) : 1f; }   // flat bridge
                });
            }
            mb.AddLoft(FUR, mz, 14, false, true);
            mb.Reassign(FUR, BELLY, headStart, (c, n) => n.y < -0.3f || (Mathf.Abs(n.x) > 0.6f && c.y < sk.y - R.y * 0.25f));
            // nose leather
            mb.AddEllipsoid(DARK, new Vector3(0, sk.y - R.y * 0.1f - 0.018f, mzFrontZ + 0.004f), new Vector3(d.muzzleW * 0.55f, d.muzzleH * 0.5f, 0.018f), 6, 8, null, _ => W(HQ));
            // lips along the muzzle's lower edge
            for (int s = -1; s <= 1; s += 2)
                mb.AddEllipsoid(DARK, new Vector3(s * d.muzzleW * 0.72f, sk.y - R.y * 0.18f - d.muzzleH * 0.72f, (mzBackZ + mzFrontZ) * 0.5f), new Vector3(0.006f, 0.006f, d.muzzleLen * 0.5f), 4, 6, null, _ => W(HQ));
            // eyes: almond, set forward and to the sides above the muzzle base
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 ec = new Vector3(s * R.x * 0.62f, sk.y + R.y * 0.18f, sk.z + R.z * 0.62f);
                mb.AddEllipsoid(DARK, ec + new Vector3(s * 0.002f, 0, -0.002f), new Vector3(0.016f, 0.011f, 0.016f), 5, 8, null, _ => W(HQ));
                mb.AddEllipsoid(EYE, ec + new Vector3(s * 0.004f, 0, 0.002f), new Vector3(0.011f, 0.008f, 0.011f), 5, 8, null, _ => W(HQ));
            }
            // lower jaw and the inside of the mouth
            var JQ = BeastRig.Q.Jaw;
            var jaw = new List<MeshBuilder.Ring>();
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                float z = Mathf.Lerp(d.jaw.z, mzFrontZ - 0.012f, t);
                float y = d.jaw.y - 0.012f - t * 0.012f;
                jaw.Add(new MeshBuilder.Ring { center = new Vector3(0, y, z), rot = Frame(Vector3.forward, Vector3.right), rx = Mathf.Lerp(d.muzzleW * 0.9f, d.muzzleW * 0.45f, t), rz = Mathf.Lerp(0.02f, 0.012f, t), weight = W(JQ) });
            }
            mb.AddLoft(BELLY, jaw, 10, false, true);
            mb.AddEllipsoid(MOUTH, new Vector3(0, d.jaw.y - 0.002f, (d.jaw.z + mzFrontZ) * 0.5f), new Vector3(d.muzzleW * 0.6f, 0.012f, d.muzzleLen * 0.45f), 5, 8, null, _ => W(JQ));
            // teeth: canines up and down, a row between
            for (int s = -1; s <= 1; s += 2)
            {
                mb.bone = I(HQ); mb.bone2 = -1;
                mb.Push(); mb.Translate(new Vector3(s * d.muzzleW * 0.5f, sk.y - R.y * 0.18f - d.muzzleH * 0.72f, mzFrontZ - 0.035f)); mb.Rotate(Quaternion.Euler(180, 0, 0));
                mb.AddCone(TEETH, Vector3.zero, 0.005f, 0.022f, 5); mb.Pop();
                mb.bone = I(JQ);
                mb.Push(); mb.Translate(new Vector3(s * d.muzzleW * 0.42f, d.jaw.y - 0.004f, mzFrontZ - 0.05f));
                mb.AddCone(TEETH, Vector3.zero, 0.004f, 0.016f, 5); mb.Pop();
            }
            // ears: tall triangles cupped forward (or small rounds for bears and rats)
            for (int s = -1; s <= 1; s += 2)
            {
                var EQ = s < 0 ? BeastRig.Q.EarL : BeastRig.Q.EarR;
                Vector3 eb = new Vector3(s * d.earBase.x, d.earBase.y, d.earBase.z);
                Vector3 up = Quaternion.Euler(-12, 0, -s * 16) * Vector3.up;
                var er = new List<MeshBuilder.Ring>();
                int n = d.roundEars ? 3 : 4;
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n;
                    float w = d.roundEars ? d.earW * Mathf.Sqrt(Mathf.Max(0.05f, 1f - t * t)) : d.earW * (1f - t) + 0.002f;
                    er.Add(new MeshBuilder.Ring
                    {
                        center = eb + up * d.earH * t, rot = Frame(up, Vector3.right), rx = w, rz = w * 0.42f, weight = W(EQ), offsetZ = -w * 0.25f,
                        radial = a => Mathf.Sin(a) < 0 ? 1f : 0.55f            // cupped: thin at the front, thick at the back
                    });
                }
                mb.AddLoft(FUR, er, 10, true, true);
                mb.AddEllipsoid(BELLY, eb + up * d.earH * 0.38f + new Vector3(0, 0, 0.006f), new Vector3(d.earW * 0.5f, d.earH * 0.28f, 0.004f), 4, 6, null, _ => W(EQ));
            }
        }

        /// <summary>Clumps of fur: the ruff at the neck and cheeks, hackles on the back, feathering on the legs, a bushy tail.</summary>
        static void Fur(MeshBuilder mb, Dims d, BodyKind kind)
        {
            var rnd = new System.Random(kind == BodyKind.DireWolf ? 7 : 3);
            float R() => (float)rnd.NextDouble();
            float ruff = d.heavyRuff ? 1.3f : 1f;
            if (kind == BodyKind.Bear) ruff = 0.8f;
            // neck ruff: a mane from behind the ears down over the shoulders and throat
            for (int i = 0; i < 34; i++)
            {
                float a = Mathf.Lerp(-Mathf.PI * 0.95f, Mathf.PI * 0.95f, R());
                float along = R();
                Vector3 c = Vector3.Lerp(d.neck1 + new Vector3(0, 0.02f, 0), d.neck2, along * 0.9f);
                float rw = Mathf.Lerp(0.1f, 0.075f, along), rh = Mathf.Lerp(0.14f, 0.1f, along);
                Vector3 outDir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0);
                Vector3 root = c + new Vector3(outDir.x * rw, outDir.y * rh, 0) * 0.92f;
                Vector3 dir = (outDir * 0.5f + new Vector3(0, -0.35f, -1f)).normalized;
                var w = along < 0.5f ? W(BeastRig.Q.Neck1, BeastRig.Q.Chest, 0.3f) : W(BeastRig.Q.Neck2, BeastRig.Q.Neck1, 0.3f);
                Tuft(mb, root, dir, outDir, (0.055f + R() * 0.04f) * ruff, 0.04f * ruff, w, outDir.y < -0.3f ? BELLY : FUR);
            }
            // cheek ruff
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 7; i++)
                {
                    Vector3 root = d.skull + new Vector3(s * d.skullR.x * 0.95f, -d.skullR.y * (0.2f + R() * 0.5f), -d.skullR.z * (0.1f + R() * 0.5f));
                    Vector3 dir = new Vector3(s * 0.7f, -0.35f, -0.75f).normalized;
                    Tuft(mb, root, dir, new Vector3(s, 0, 0), (0.04f + R() * 0.025f) * ruff, 0.018f, W(BeastRig.Q.Head), BELLY);
                }
            // hackles along the spine
            for (int i = 0; i < 4; i++)
            {
                float z = Mathf.Lerp(d.chest.z + 0.12f, d.chest.z - 0.04f, i / 3f);
                float top = Mathf.Lerp(d.btop[6], d.btop[5], i / 3f);
                Vector3 root = new Vector3((R() - 0.5f) * 0.05f, top - 0.012f, z);
                Vector3 dir = new Vector3((R() - 0.5f) * 0.4f, 0.35f, -1f).normalized;
                var w = z > d.chest.z - 0.05f ? W(BeastRig.Q.Chest) : W(BeastRig.Q.Loin, BeastRig.Q.Chest, 0.5f);
                Tuft(mb, root, dir, Vector3.up, 0.04f + R() * 0.02f, 0.034f, w, SADDLE);
            }
            // feathering behind the forearms and the thighs' "trousers"
            for (int side = -1; side <= 1; side += 2)
            {
                bool r = side > 0;
                var F = r ? BeastRig.Q.ForeR : BeastRig.Q.ForeL; var Fe = r ? BeastRig.Q.FemR : BeastRig.Q.FemL;
                Vector3 el = new Vector3(side * d.elbow.x, d.elbow.y, d.elbow.z);
                for (int i = 0; i < 4; i++)
                    Tuft(mb, el + new Vector3(0, -0.02f - i * 0.03f, -0.028f), new Vector3(0, -0.4f, -1f).normalized, new Vector3(side, 0, 0), 0.035f, 0.018f, W(F), LEG);
                Vector3 th = new Vector3(side * (d.hip.x + 0.03f), (d.hip.y + d.stifle.y) * 0.5f - 0.03f, (d.hip.z + d.stifle.z) * 0.5f - 0.1f);
                for (int i = 0; i < 6; i++)
                    Tuft(mb, th + new Vector3(0, (R() - 0.5f) * 0.12f, -R() * 0.02f), new Vector3(side * 0.3f, -0.5f, -1f).normalized, new Vector3(side, 0, 0), 0.05f + R() * 0.02f, 0.024f, W(Fe, BeastRig.Q.Pelvis, 0.2f), FUR);
            }
            // bushy tail
            BeastRig.Q[] tb = { BeastRig.Q.Tail0, BeastRig.Q.Tail1, BeastRig.Q.Tail2, BeastRig.Q.Tail3, BeastRig.Q.Tail4 };
            for (int i = 0; i < 30; i++)
            {
                float t = 0.15f + R() * 0.8f;
                float seg = t * (d.tail.Length - 1);
                int k = Mathf.Min(d.tail.Length - 2, Mathf.FloorToInt(seg));
                Vector3 c = Vector3.Lerp(d.tail[k], d.tail[k + 1], seg - k);
                Vector3 along = (d.tail[k + 1] - d.tail[k]).normalized;
                float a = R() * Mathf.PI * 2f;
                Vector3 side = Vector3.Cross(along, Vector3.right).normalized;
                Vector3 outDir = (Vector3.right * Mathf.Cos(a) + side * Mathf.Sin(a)).normalized;
                float rad = Mathf.Lerp(d.tailR[k], d.tailR[k + 1], seg - k);
                Tuft(mb, c + outDir * rad * 0.8f, (along * 1f + outDir * 0.35f).normalized, outDir, 0.045f + R() * 0.025f, 0.034f, W(tb[Mathf.Min(k, tb.Length - 1)]), i % 3 == 0 ? SADDLE : FUR);
            }
        }

        /// <summary>A tapered clump of fur: a flat strip leaving the body along dir and curling a little with gravity.</summary>
        static void Tuft(MeshBuilder mb, Vector3 root, Vector3 dir, Vector3 normal, float len, float width, BoneWeight w, int sub)
        {
            var rings = new List<MeshBuilder.Ring>();
            const int n = 4;
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                Vector3 p = root + dir * len * t + Vector3.down * len * 0.18f * t * t;
                Vector3 dd = (dir + Vector3.down * 0.36f * t).normalized;
                Vector3 side = Vector3.Cross(dd, normal);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(dd, Vector3.up);
                rings.Add(new MeshBuilder.Ring { center = p, rot = Frame(dd, side), rx = width * (1f - t * 0.92f), rz = width * 0.3f * (1f - t * 0.7f), weight = w });
            }
            mb.AddLoft(sub, rings, 5, false, true);
        }

        static Material[] Materials(BodyKind kind, Color fur)
        {
            Color cream = kind == BodyKind.Rat ? new Color(.78f, .6f, .58f) : new Color(.78f, .72f, .62f);
            Color belly = Color.Lerp(fur, cream, kind == BodyKind.DireWolf ? 0.18f : 0.42f);
            Color saddle = Color.Lerp(fur, Color.black, kind == BodyKind.Bear ? 0.15f : 0.38f);
            Color leg = Color.Lerp(fur, new Color(.62f, .54f, .44f), kind == BodyKind.DireWolf ? 0.1f : 0.3f);
            if (kind == BodyKind.Bear) { belly = Color.Lerp(fur, Color.black, 0.1f); leg = Color.Lerp(fur, Color.black, 0.2f); }
            Color eye = kind == BodyKind.Rat ? new Color(.25f, .02f, .02f) : new Color(.95f, .72f, .2f);
            return new[]
            {
                MatLib.Lit(fur, TexId.Hair, .22f, 0, 2.5f, 1.2f),
                MatLib.Lit(belly, TexId.Hair, .2f, 0, 2.5f, 1.0f),
                MatLib.Lit(new Color(.05f, .04f, .04f), null, .55f),
                MatLib.Emissive(eye, eye * (kind == BodyKind.DireWolf ? 2.2f : 0.5f)),
                MatLib.Lit(new Color(.9f, .88f, .8f), null, .5f),
                MatLib.Lit(new Color(.35f, .1f, .1f), null, .6f),
                MatLib.Lit(saddle, TexId.Hair, .2f, 0, 2.5f, 1.2f),
                MatLib.Lit(leg, TexId.Hair, .22f, 0, 3f, 1.0f),
            };
        }
    }
}
