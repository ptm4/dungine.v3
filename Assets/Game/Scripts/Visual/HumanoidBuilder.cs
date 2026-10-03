using System;
using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    public enum B
    {
        Root, Hips, Spine, Chest, Neck, Head,
        ClavL, UpperArmL, LowerArmL, HandL,
        ClavR, UpperArmR, LowerArmR, HandR,
        ThighL, ShinL, FootL, ThighR, ShinR, FootR,
        Tail0, Tail1, Tail2, Tail3,
        Cape0, Cape1, Cape2, Cape3,
        ToeL, ToeR,
        COUNT
    }

    /// <summary>Skeleton + sockets of a built humanoid.</summary>
    public class HumanoidRig : MonoBehaviour
    {
        public Transform[] bones = new Transform[(int)B.COUNT];
        public Vector3[] bindPos = new Vector3[(int)B.COUNT];
        public Transform socketHandR, socketHandL, socketShield, socketBack, socketHipL, socketHipR, headTop, eyes;
        public float height, headSize, scale;
        public SkinnedMeshRenderer body;
        public Appearance look;
        public GearLook gear;
        public GameObject mainWeapon, offWeapon;
        public bool hasTail, hasCape;
        public Transform this[B b] => bones[(int)b];
        public List<Renderer> renderers = new List<Renderer>();
    }

    /// <summary>Builds a complete, skinned, fully-dressed humanoid from an Appearance and GearLook.</summary>
    public static partial class HumanoidBuilder
    {
        // submesh slots
        public const int SKIN = 0, HAIR = 1, EYEW = 2, IRIS = 3, CLOTH1 = 4, CLOTH2 = 5, LEATHER = 6, METAL = 7, TRIM = 8, HORN = 9, DARK = 10, CAPE = 11, FACE = 12, SUBS = 13;

        class Dims
        {
            public float H, headH, hs, s, ws, chinY, neckY, shoulderY, hipY, hipsY, T, kneeY, ankleY;
            public float hipW, waistW, chestW, shoulderW, pelvisD, waistD, chestD, shoulderD, neckR;
            public float thighR, kneeR, calfR, ankleR, deltR, upperArmR, elbowRad, forearmR, wristRad;
            public float upperArmLen, foreArmLen, handLen, armAngle;
            public float ballZ, heelZ, toeZ;     // foot landmarks, forward of the ankle
            public Vector3 shoulderL, shoulderR, elbowL, elbowR, wristL, wristR, hipL, hipR, kneeL, kneeR_, ankleL, ankleR_;
            public Vector3 headC, headRad;
            public bool fem;
        }

        static int I(B b) => (int)b;

        public static HumanoidRig Build(Appearance a, GearLook g, string name = "Humanoid", int faceRes = 256)
        {
            var look = RaceLooks.Looks[a.race];
            var d = ComputeDims(a, look);
            var go = new GameObject(name);
            var rig = go.AddComponent<HumanoidRig>();
            rig.look = a; rig.gear = g;
            rig.height = d.H; rig.headSize = d.headH; rig.scale = d.s;

            // ---------- skeleton ----------
            var pos = new Vector3[(int)B.COUNT];
            var parent = new int[(int)B.COUNT];
            void Def(B b, B p, Vector3 wp) { pos[I(b)] = wp; parent[I(b)] = I(p); }
            parent[0] = -1; pos[0] = Vector3.zero;
            Def(B.Hips, B.Root, new Vector3(0, d.hipsY, 0));
            Def(B.Spine, B.Hips, new Vector3(0, d.hipsY + d.T * 0.3f, 0));
            Def(B.Chest, B.Spine, new Vector3(0, d.hipsY + d.T * 0.62f, 0));
            Def(B.Neck, B.Chest, new Vector3(0, d.neckY, -0.005f * d.s));
            Def(B.Head, B.Neck, new Vector3(0, d.chinY + d.headH * 0.06f, 0));
            Def(B.ClavL, B.Chest, new Vector3(-0.03f * d.ws, d.shoulderY - 0.02f * d.s, 0));
            Def(B.ClavR, B.Chest, new Vector3(0.03f * d.ws, d.shoulderY - 0.02f * d.s, 0));
            Def(B.UpperArmL, B.ClavL, d.shoulderL); Def(B.LowerArmL, B.UpperArmL, d.elbowL); Def(B.HandL, B.LowerArmL, d.wristL);
            Def(B.UpperArmR, B.ClavR, d.shoulderR); Def(B.LowerArmR, B.UpperArmR, d.elbowR); Def(B.HandR, B.LowerArmR, d.wristR);
            Def(B.ThighL, B.Hips, d.hipL); Def(B.ShinL, B.ThighL, d.kneeL); Def(B.FootL, B.ShinL, d.ankleL);
            Def(B.ThighR, B.Hips, d.hipR); Def(B.ShinR, B.ThighR, d.kneeR_); Def(B.FootR, B.ShinR, d.ankleR_);
            // ball of the foot: the toes bend here as the heel lifts
            Def(B.ToeL, B.FootL, d.ankleL + new Vector3(0, -d.ankleY + 0.022f * d.s, d.ballZ));
            Def(B.ToeR, B.FootR, d.ankleR_ + new Vector3(0, -d.ankleY + 0.022f * d.s, d.ballZ));
            // tail
            Vector3 t0 = new Vector3(0, d.hipsY - 0.06f * d.s, -d.pelvisD * 0.95f);
            Def(B.Tail0, B.Hips, t0);
            Def(B.Tail1, B.Tail0, t0 + new Vector3(0, -0.12f, -0.12f) * d.s);
            Def(B.Tail2, B.Tail1, t0 + new Vector3(0, -0.28f, -0.2f) * d.s);
            Def(B.Tail3, B.Tail2, t0 + new Vector3(0, -0.45f, -0.22f) * d.s);
            // cape
            float capeLen = d.shoulderY - d.ankleY - 0.12f * d.s;
            Vector3 c0 = new Vector3(0, d.shoulderY + 0.01f * d.s, -d.chestD * 0.95f - 0.02f * d.s);
            Def(B.Cape0, B.Chest, c0);
            Def(B.Cape1, B.Cape0, c0 + new Vector3(0, -capeLen * 0.33f, -0.03f * d.s));
            Def(B.Cape2, B.Cape1, c0 + new Vector3(0, -capeLen * 0.66f, -0.05f * d.s));
            Def(B.Cape3, B.Cape2, c0 + new Vector3(0, -capeLen, -0.06f * d.s));

            var model = new GameObject("Model").transform;
            model.SetParent(go.transform, false);
            for (int i = 0; i < (int)B.COUNT; i++)
            {
                var t = new GameObject(((B)i).ToString()).transform;
                rig.bones[i] = t;
            }
            for (int i = 0; i < (int)B.COUNT; i++)
            {
                var t = rig.bones[i];
                if (parent[i] < 0) t.SetParent(model, false);
                else t.SetParent(rig.bones[parent[i]], false);
            }
            for (int i = 0; i < (int)B.COUNT; i++)
            {
                rig.bones[i].position = model.TransformPoint(pos[i]);
                rig.bindPos[i] = rig.bones[i].localPosition;
            }

            // ---------- mesh ----------
            var mb = new MeshBuilder(SUBS, true);
            BuildBody(mb, a, g, d, look);
            var mesh = mb.Build(name + "_mesh");
            var bind = new Matrix4x4[(int)B.COUNT];
            for (int i = 0; i < (int)B.COUNT; i++) bind[i] = rig.bones[i].worldToLocalMatrix * model.localToWorldMatrix;
            mesh.bindposes = bind;

            var smrGo = new GameObject("Body");
            smrGo.transform.SetParent(model, false);
            var smr = smrGo.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.bones = rig.bones;
            smr.rootBone = rig.bones[I(B.Hips)];
            smr.updateWhenOffscreen = false;
            smr.localBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(d.H * 2.2f, d.H * 2.2f, d.H * 2.2f));
            smr.sharedMaterials = BuildMaterials(a, g, PaintFace(a, faceRes));
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            rig.body = smr;
            rig.renderers.Add(smr);
            rig.hasTail = a.tail && a.race == RaceId.Tiefling;
            rig.hasCape = a.cape;

            // ---------- sockets ----------
            rig.socketHandR = Socket("SocketHandR", rig[B.HandR], new Vector3(0, -0.045f * d.s, 0.012f * d.s), Quaternion.Euler(90, 0, 0));
            rig.socketHandL = Socket("SocketHandL", rig[B.HandL], new Vector3(0, -0.045f * d.s, 0.012f * d.s), Quaternion.Euler(90, 0, 0));
            rig.socketShield = Socket("SocketShield", rig[B.LowerArmL], (d.wristL - d.elbowL) * 0.45f + new Vector3(-d.forearmR * 1.6f, 0, 0), Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 90, 0));
            rig.socketBack = Socket("SocketBack", rig[B.Chest], new Vector3(0, 0.02f * d.s, -d.chestD - 0.05f * d.s) - (pos[I(B.Chest)] - pos[I(B.Chest)]), Quaternion.Euler(0, 0, 35) * Quaternion.Euler(0, 180, 0));
            rig.socketHipL = Socket("SocketHipL", rig[B.Hips], new Vector3(-d.hipW - 0.04f * d.s, -0.02f * d.s, 0.02f * d.s), Quaternion.Euler(-160, 0, 8));
            rig.socketHipR = Socket("SocketHipR", rig[B.Hips], new Vector3(d.hipW + 0.04f * d.s, -0.02f * d.s, 0.02f * d.s), Quaternion.Euler(-160, 0, -8));
            rig.headTop = Socket("HeadTop", rig[B.Head], new Vector3(0, d.H - pos[I(B.Head)].y + 0.05f, 0), Quaternion.identity);
            rig.eyes = Socket("Eyes", rig[B.Head], d.headC + new Vector3(0, 0.01f * d.hs, d.headRad.z * 0.8f) - pos[I(B.Head)], Quaternion.identity);

            // ---------- weapons ----------
            ApplyGear(rig, g);
            return rig;
        }

        static Transform Socket(string n, Transform parent, Vector3 local, Quaternion rot)
        {
            var t = new GameObject(n).transform;
            t.SetParent(parent, false);
            t.localPosition = local; t.localRotation = rot;
            return t;
        }

        public static void ApplyGear(HumanoidRig rig, GearLook g)
        {
            if (rig.mainWeapon) UnityEngine.Object.Destroy(rig.mainWeapon);
            if (rig.offWeapon) UnityEngine.Object.Destroy(rig.offWeapon);
            rig.gear = g;
            float ws = Mathf.Lerp(0.72f, 1f, Mathf.InverseLerp(0.95f, 1.75f, rig.height));
            if (g.main != WeaponVisual.None)
            {
                rig.mainWeapon = WeaponBuilder.Build(g.main, ws);
                rig.renderers.AddRange(rig.mainWeapon.GetComponentsInChildren<Renderer>());
            }
            if (g.off != WeaponVisual.None)
            {
                rig.offWeapon = WeaponBuilder.Build(g.off, ws);
                rig.renderers.AddRange(rig.offWeapon.GetComponentsInChildren<Renderer>());
            }
            SetDrawn(rig, g.drawn);
        }

        public static bool IsBow(WeaponVisual w) => w == WeaponVisual.Shortbow || w == WeaponVisual.Longbow;
        public static bool IsCrossbow(WeaponVisual w) => w == WeaponVisual.LightCrossbow || w == WeaponVisual.HeavyCrossbow || w == WeaponVisual.HandCrossbow;
        public static bool IsLarge(WeaponVisual w) => w == WeaponVisual.Greatsword || w == WeaponVisual.Greataxe || w == WeaponVisual.Maul || w == WeaponVisual.Glaive || w == WeaponVisual.Halberd || w == WeaponVisual.Quarterstaff || w == WeaponVisual.Staff || w == WeaponVisual.Spear || w == WeaponVisual.Trident || w == WeaponVisual.Greatclub || IsBow(w) || w == WeaponVisual.HeavyCrossbow || w == WeaponVisual.Lute;

        /// <summary>Move weapons between hands (drawn) and back/hip (sheathed).</summary>
        public static void SetDrawn(HumanoidRig rig, bool drawn)
        {
            if (rig.gear != null) rig.gear.drawn = drawn;
            if (rig.mainWeapon)
            {
                var w = rig.gear.main;
                Transform p;
                if (drawn) p = IsBow(w) ? rig.socketHandL : rig.socketHandR;
                else p = IsLarge(w) ? rig.socketBack : rig.socketHipL;
                Attach(rig.mainWeapon.transform, p);
                if (!drawn && IsLarge(w)) rig.mainWeapon.transform.localPosition = new Vector3(0, -0.1f * rig.scale, 0);
                if (w == WeaponVisual.Torch) Attach(rig.mainWeapon.transform, rig.socketHandR);
            }
            if (rig.offWeapon)
            {
                var w = rig.gear.off;
                Transform p;
                if (w == WeaponVisual.Shield) p = drawn ? rig.socketShield : rig.socketBack;
                else if (w == WeaponVisual.Torch) p = rig.socketHandL;
                else p = drawn ? rig.socketHandL : rig.socketHipR;
                Attach(rig.offWeapon.transform, p);
                if (w == WeaponVisual.Shield && !drawn) { rig.offWeapon.transform.localRotation = Quaternion.Euler(0, 180, 0) * Quaternion.Euler(90, 0, 0); rig.offWeapon.transform.localPosition = new Vector3(0, -0.12f * rig.scale, -0.03f); }
            }
        }

        static void Attach(Transform t, Transform p)
        {
            t.SetParent(p, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }

        // ---------------------------------------------------------------------------------
        static Dims ComputeDims(Appearance a, RaceLook look)
        {
            var d = new Dims();
            d.fem = a.bodyType == 1;
            d.H = look.height * (d.fem ? look.femHeight : 1f) * (1f + a.heightAdj * 0.06f);
            if (a.hunched) d.H *= 0.94f;
            if (a.child) d.H *= 0.66f;
            d.headH = d.H / (a.child ? 5.7f : look.headRatio);
            d.hs = d.headH / 0.24f;
            d.s = d.H / 1.78f;
            float build = (a.strong ? 1.13f : 1f) * (d.fem ? 0.9f : 1f) * a.bulk * (a.gaunt ? 0.85f : 1f);
            d.ws = d.s * look.width * build;
            float limb = d.s * look.width * (a.strong ? 1.14f : 1f) * (d.fem ? 0.9f : 1f) * Mathf.Lerp(1f, a.bulk, 0.6f) * (a.gaunt ? 0.8f : 1f);
            if (a.skeletal) { limb *= 0.45f; d.ws *= 0.7f; }

            d.chinY = d.H - d.headH;
            d.neckY = d.chinY - d.headH * 0.22f;
            d.shoulderY = d.neckY - d.headH * 0.08f;
            d.hipY = d.H * (a.child ? look.legFrac - 0.035f : look.legFrac);
            d.hipsY = d.hipY + 0.035f * d.H;
            d.T = d.shoulderY - d.hipsY;
            d.kneeY = d.hipY * 0.53f;
            d.ankleY = 0.045f * d.H;
            d.heelZ = -0.055f * d.s; d.ballZ = 0.105f * d.s; d.toeZ = 0.175f * d.s;

            d.hipW = (d.fem ? 0.172f : 0.152f) * d.ws / build * Mathf.Lerp(1, build, 0.7f);
            d.waistW = (d.fem ? 0.118f : 0.135f) * d.ws * (a.bulk > 1.2f ? 1.25f : 1f);
            d.chestW = (d.fem ? 0.148f : 0.168f) * d.ws;
            d.shoulderW = (d.fem ? 0.172f : 0.2f) * d.ws * (a.strong ? 1.06f : 1f);
            d.pelvisD = 0.105f * d.ws;
            d.waistD = 0.095f * d.ws * (a.bulk > 1.2f ? 1.45f : 1f);
            d.chestD = (d.fem ? 0.108f : 0.118f) * d.ws;
            d.shoulderD = 0.088f * d.ws;
            d.neckR = (d.fem ? 0.052f : 0.062f) * Mathf.Lerp(d.s, d.ws, 0.5f) * (look.dragon ? 1.3f : 1f);
            if (a.skeletal) d.neckR *= 0.5f;

            d.thighR = 0.086f * limb; d.kneeR = 0.053f * limb; d.calfR = 0.058f * limb; d.ankleR = 0.036f * limb;
            d.deltR = 0.056f * limb; d.upperArmR = 0.047f * limb; d.elbowRad = 0.037f * limb; d.forearmR = 0.042f * limb; d.wristRad = 0.028f * limb;
            if (d.fem) { d.thighR *= 1.08f; }

            d.upperArmLen = 0.30f * d.s * (a.race == RaceId.Dwarf ? 0.98f : 1f);
            d.foreArmLen = 0.255f * d.s;
            d.handLen = 0.17f * d.s;
            d.armAngle = 11f;
            float sx = d.shoulderW - d.upperArmR * 0.55f;
            d.shoulderR = new Vector3(sx, d.shoulderY - 0.018f * d.s, -0.008f * d.s);
            d.shoulderL = new Vector3(-sx, d.shoulderR.y, d.shoulderR.z);
            Vector3 dirR = new Vector3(Mathf.Sin(d.armAngle * Mathf.Deg2Rad), -Mathf.Cos(d.armAngle * Mathf.Deg2Rad), 0);
            Vector3 dirL = new Vector3(-dirR.x, dirR.y, 0);
            d.elbowR = d.shoulderR + dirR * d.upperArmLen; d.elbowL = d.shoulderL + dirL * d.upperArmLen;
            d.wristR = d.elbowR + dirR * d.foreArmLen; d.wristL = d.elbowL + dirL * d.foreArmLen;

            float hx = d.hipW * 0.55f;
            d.hipR = new Vector3(hx, d.hipY, 0); d.hipL = new Vector3(-hx, d.hipY, 0);
            d.kneeR_ = new Vector3(hx * 0.92f, d.kneeY, 0.012f * d.s); d.kneeL = new Vector3(-hx * 0.92f, d.kneeY, 0.012f * d.s);
            d.ankleR_ = new Vector3(hx * 0.88f, d.ankleY, -0.005f * d.s); d.ankleL = new Vector3(-hx * 0.88f, d.ankleY, -0.005f * d.s);

            float faceW = a.race == RaceId.Dwarf || a.race == RaceId.HalfOrc ? 1.08f : (a.race == RaceId.Githyanki || a.gaunt ? 0.92f : 1f);
            if (a.faceShape == 1) faceW *= 1.04f;
            if (a.faceShape == 2) faceW *= 0.95f;
            d.headC = new Vector3(0, d.chinY + d.headH * 0.47f, 0.012f * d.hs);
            d.headRad = new Vector3(d.headH * 0.355f * faceW, d.headH * 0.5f * (a.faceShape == 2 ? 1.05f : 1f), d.headH * 0.44f);
            if (look.dragon) { d.headRad = new Vector3(d.headH * 0.36f, d.headH * 0.42f, d.headH * 0.42f); d.headC.y -= d.headH * 0.07f; }
            return d;
        }

        static float Sat(float x) => Mathf.Clamp01(x);

        static MeshBuilder.Ring R(Vector3 c, float rx, float rz, BoneWeight w, Quaternion? rot = null, float offZ = 0, Func<float, float> radial = null)
            => new MeshBuilder.Ring { center = c, rx = rx, rz = rz, weight = w, rot = rot ?? Quaternion.identity, offsetZ = offZ, radial = radial };

        // ---------------------------------------------------------------------------------
        static void BuildBody(MeshBuilder mb, Appearance a, GearLook g, Dims d, RaceLook look)
        {
            var armor = g.armor;
            bool ghost = a.ghostly;
            bool skel = a.skeletal;

            // material choices per region
            int torsoM = SKIN, sleeveM = SKIN, forearmM = SKIN, handM = SKIN, thighM = SKIN, shinM = SKIN, footM = SKIN;
            bool skirtLong = false, skirtShort = false, belt = false, pauldrons = false, tassets = false, tabard = false, bracers = false, mantle = false;
            switch (armor)
            {
                case ArmorVisual.None: torsoM = SKIN; sleeveM = SKIN; forearmM = SKIN; thighM = CLOTH2; shinM = CLOTH2; footM = SKIN; belt = true; break;
                case ArmorVisual.Cloth: torsoM = CLOTH1; sleeveM = CLOTH1; forearmM = CLOTH1; thighM = CLOTH2; shinM = LEATHER; footM = LEATHER; skirtShort = true; belt = true; break;
                case ArmorVisual.Robe: torsoM = CLOTH1; sleeveM = CLOTH1; forearmM = CLOTH1; thighM = CLOTH2; shinM = CLOTH2; footM = LEATHER; skirtLong = true; belt = true; break;
                case ArmorVisual.Leather: torsoM = LEATHER; sleeveM = CLOTH1; forearmM = LEATHER; handM = LEATHER; thighM = CLOTH2; shinM = LEATHER; footM = LEATHER; belt = true; tassets = true; bracers = true; break;
                case ArmorVisual.Hide: torsoM = LEATHER; sleeveM = SKIN; forearmM = LEATHER; thighM = CLOTH2; shinM = LEATHER; footM = LEATHER; belt = true; mantle = true; bracers = true; break;
                case ArmorVisual.Chain: case ArmorVisual.Scale: torsoM = METAL; sleeveM = METAL; forearmM = LEATHER; handM = LEATHER; thighM = CLOTH2; shinM = LEATHER; footM = LEATHER; belt = true; tabard = true; skirtShort = true; break;
                case ArmorVisual.Plate: torsoM = CLOTH2; sleeveM = CLOTH2; forearmM = CLOTH2; handM = METAL; thighM = CLOTH2; shinM = CLOTH2; footM = METAL; belt = true; break;
            }
            if (skel) { torsoM = sleeveM = forearmM = handM = thighM = shinM = footM = SKIN; skirtShort = skirtLong = belt = tassets = tabard = pauldrons = mantle = bracers = false; }

            float bi = d.fem ? 1f : 0f;
            // ---------------- torso ----------------
            Func<float, float> bust = null;
            if (d.fem && !skel)
                bust = ang =>
                {
                    // front is +Z => angle pi/2 in ring space (x=cos,z=sin)
                    float front = Mathf.Max(0, Mathf.Sin(ang));
                    float lr = Mathf.Abs(Mathf.Cos(ang));
                    return 1f + front * 0.28f * Mathf.Exp(-Mathf.Pow((lr - 0.45f) / 0.3f, 2));
                };
            Func<float, float> backFlat = ang => Mathf.Sin(ang) < 0 ? 1f - 0.12f * Mathf.Pow(-Mathf.Sin(ang), 3) : 1f;
            float armorInflate = armor == ArmorVisual.Plate ? 1.02f : (armor == ArmorVisual.Chain || armor == ArmorVisual.Scale ? 1.05f : (armor == ArmorVisual.Leather || armor == ArmorVisual.Hide ? 1.03f : 1f));
            if (torsoM == SKIN) armorInflate = 1f;
            float belly = a.bulk > 1.2f ? 1.3f : 1f;
            var torso = new List<MeshBuilder.Ring>
            {
                R(new Vector3(0, d.hipY - 0.04f * d.s, 0), d.hipW * 0.62f, d.pelvisD * 0.7f, MeshBuilder.W(I(B.Hips))),
                R(new Vector3(0, d.hipY + 0.02f * d.s, 0), d.hipW * 1.0f, d.pelvisD, MeshBuilder.W(I(B.Hips)), null, 0, backFlat),
                R(new Vector3(0, d.hipsY + d.T * 0.2f, 0.004f), Mathf.Lerp(d.hipW, d.waistW, 0.55f), Mathf.Lerp(d.pelvisD, d.waistD, 0.5f) * belly, MeshBuilder.W(I(B.Hips), I(B.Spine), 0.4f), null, 0.01f * (belly - 1) * 3, backFlat),
                R(new Vector3(0, d.hipsY + d.T * 0.4f, 0.006f), d.waistW, d.waistD * belly, MeshBuilder.W(I(B.Spine)), null, 0.015f * (belly - 1) * 3, backFlat),
                R(new Vector3(0, d.hipsY + d.T * 0.6f, 0.008f * d.s), d.chestW * 0.96f * armorInflate, d.chestD * 0.95f * armorInflate, MeshBuilder.W(I(B.Spine), I(B.Chest), 0.6f), null, 0, backFlat),
                R(new Vector3(0, d.hipsY + d.T * 0.78f, 0.01f * d.s), d.chestW * armorInflate, d.chestD * armorInflate, MeshBuilder.W(I(B.Chest)), null, 0, bust ?? backFlat),
                R(new Vector3(0, d.shoulderY - 0.035f * d.s, 0), d.shoulderW * 0.93f * armorInflate, d.chestD * 0.85f * armorInflate, MeshBuilder.W(I(B.Chest)), null, 0, backFlat),
                R(new Vector3(0, d.shoulderY + 0.012f * d.s, -0.006f * d.s), d.shoulderW * 0.62f, d.shoulderD * 0.9f, MeshBuilder.W(I(B.Chest))),
                R(new Vector3(0, d.shoulderY + 0.035f * d.s, -0.01f * d.s), d.neckR * 1.75f, d.neckR * 1.4f, MeshBuilder.W(I(B.Chest), I(B.Neck), 0.35f)),
                R(new Vector3(0, d.shoulderY + 0.06f * d.s, -0.012f * d.s), d.neckR * 1.2f, d.neckR * 1.1f, MeshBuilder.W(I(B.Chest), I(B.Neck), 0.6f)),
            };
            mb.AddLoft(torsoM, torso, 20, true, false);
            // neck (skin or armour gorget)
            var neck = new List<MeshBuilder.Ring>
            {
                R(new Vector3(0, d.shoulderY + 0.0f, -0.01f * d.s), d.neckR * 1.25f, d.neckR * 1.1f, MeshBuilder.W(I(B.Chest), I(B.Neck), 0.5f)),
                R(new Vector3(0, d.neckY + 0.02f * d.s, -0.006f * d.s), d.neckR, d.neckR * 0.95f, MeshBuilder.W(I(B.Neck))),
                R(new Vector3(0, d.chinY + 0.01f * d.s, 0.0f), d.neckR * 0.95f, d.neckR * 0.95f, MeshBuilder.W(I(B.Neck), I(B.Head), 0.7f)),
                R(new Vector3(0, d.chinY + d.headH * 0.25f, 0.0f), d.neckR * 0.8f, d.neckR * 0.8f, MeshBuilder.W(I(B.Head))),
            };
            if (!a.headless) mb.AddLoft(SKIN, neck, 12, false, true);

            // skeletal ribcage hint
            if (skel)
            {
                for (int r = 0; r < 5; r++)
                {
                    float y = d.hipsY + d.T * (0.45f + r * 0.1f);
                    var rib = new List<MeshBuilder.Ring>();
                    mb.bone = I(B.Chest);
                    for (int k = 0; k <= 10; k++)
                    {
                        float ang = Mathf.Lerp(-0.2f, Mathf.PI + 0.2f, k / 10f);
                        rib.Add(R(new Vector3(Mathf.Cos(ang) * d.chestW * 1.9f, y - Mathf.Sin(ang) * 0.01f, Mathf.Sin(ang) * d.chestD * 1.8f - 0.01f), 0.008f * d.s, 0.008f * d.s, MeshBuilder.W(I(B.Chest)), Quaternion.FromToRotation(Vector3.up, new Vector3(-Mathf.Sin(ang), 0, Mathf.Cos(ang)))));
                    }
                    mb.AddLoft(SKIN, rib, 5);
                }
            }

            // ---------------- legs ----------------
            for (int side = -1; side <= 1; side += 2)
            {
                B thigh = side < 0 ? B.ThighL : B.ThighR, shin = side < 0 ? B.ShinL : B.ShinR, foot = side < 0 ? B.FootL : B.FootR;
                Vector3 hip = side < 0 ? d.hipL : d.hipR, knee = side < 0 ? d.kneeL : d.kneeR_, ankle = side < 0 ? d.ankleL : d.ankleR_;
                Quaternion upR = Quaternion.FromToRotation(Vector3.down, (knee - hip).normalized) ;
                Quaternion loR = Quaternion.FromToRotation(Vector3.down, (ankle - knee).normalized);
                float thInf = thighM == METAL ? 1.08f : 1f;
                var upper = new List<MeshBuilder.Ring>
                {
                    R(hip + Vector3.up * 0.07f * d.s + new Vector3(-side * 0.01f, 0, 0), d.thighR * 0.95f * thInf, d.thighR * 0.9f * thInf, MeshBuilder.W(I(B.Hips), I(thigh), 0.5f), upR),
                    R(hip, d.thighR * thInf, d.thighR * 0.95f * thInf, MeshBuilder.W(I(thigh)), upR),
                    R(Vector3.Lerp(hip, knee, 0.35f), d.thighR * 0.93f * thInf, d.thighR * 0.9f * thInf, MeshBuilder.W(I(thigh)), upR, 0.004f),
                    R(Vector3.Lerp(hip, knee, 0.75f), d.thighR * 0.74f * thInf, d.thighR * 0.72f * thInf, MeshBuilder.W(I(thigh)), upR),
                    R(knee, d.kneeR * thInf, d.kneeR * 1.02f * thInf, MeshBuilder.W(I(thigh), I(shin), 0.5f), upR, 0.004f * d.s),
                    R(Vector3.Lerp(knee, ankle, 0.22f), d.calfR, d.calfR * 1.05f, MeshBuilder.W(I(shin)), loR, -0.008f * d.s),
                };
                mb.AddLoft(thighM, upper, 14, false, true);
                float bootInf = shinM == SKIN ? 1f : (shinM == METAL ? 1.18f : 1.12f);
                bool highBoot = shinM != thighM;
                var lower = new List<MeshBuilder.Ring>
                {
                    R(highBoot ? Vector3.Lerp(knee, ankle, 0.06f) : knee, d.kneeR * bootInf, d.kneeR * bootInf, MeshBuilder.W(I(thigh), I(shin), highBoot ? 0.85f : 0.5f), loR),
                    R(Vector3.Lerp(knee, ankle, 0.25f), d.calfR * bootInf, d.calfR * 1.05f * bootInf, MeshBuilder.W(I(shin)), loR, -0.008f * d.s),
                    R(Vector3.Lerp(knee, ankle, 0.62f), d.calfR * 0.74f * bootInf, d.calfR * 0.74f * bootInf, MeshBuilder.W(I(shin)), loR),
                    R(ankle + Vector3.up * 0.02f * d.s, d.ankleR * bootInf, d.ankleR * 1.1f * bootInf, MeshBuilder.W(I(shin), I(foot), 0.3f), loR),
                };
                mb.AddLoft(shinM, lower, 14, false, true);
                if (shinM == METAL) // knee cop
                {
                    mb.bone = I(shin); mb.bone2 = I(thigh); mb.bone2Weight = 0.5f;
                    mb.AddEllipsoid(METAL, knee + new Vector3(0, 0, d.kneeR * 0.7f), new Vector3(d.kneeR * 0.9f, d.kneeR * 1.0f, d.kneeR * 0.6f), 6, 10);
                    mb.bone2 = -1;
                }
                // foot: a loft from heel to toe with a flat sole; the toes ride the toe bone so the foot can roll
                B toe = side < 0 ? B.ToeL : B.ToeR;
                mb.bone = I(foot); mb.bone2 = -1;
                float fs = footM == SKIN ? 1f : (footM == METAL ? 1.14f : 1.1f);
                if (skel)
                    mb.AddEllipsoid(footM, ankle + new Vector3(0, -d.ankleY * 0.5f, 0.06f * d.s), new Vector3(0.03f, 0.018f, 0.1f) * d.s, 6, 10);
                else
                {
                    float sole = -d.ankleY;      // ground, relative to the ankle
                    // (z forward of the ankle, half width, height above the sole)
                    float[,] prof =
                    {
                        { d.heelZ - 0.012f * d.s, 0.022f, 0.030f },
                        { d.heelZ,                0.034f, 0.058f },
                        { d.heelZ * 0.2f,         0.038f, d.ankleY + 0.012f * d.s },
                        { d.ballZ * 0.45f,        0.043f, d.ankleY * 0.85f },
                        { d.ballZ,                0.049f, 0.046f },
                        { (d.ballZ + d.toeZ) * 0.5f, 0.046f, 0.032f },
                        { d.toeZ,                 0.030f, 0.022f },
                        { d.toeZ + 0.012f * d.s,  0.012f, 0.012f },
                    };
                    var fr = new List<MeshBuilder.Ring>();
                    for (int k = 0; k < prof.GetLength(0); k++)
                    {
                        float z = prof[k, 0], hw = prof[k, 1] * d.s * fs, ht = prof[k, 2] * (k == 2 || k == 3 ? 1f : d.s) * fs;
                        if (k == 2 || k == 3) ht = prof[k, 2] * fs;
                        float tw = Mathf.InverseLerp(d.ballZ - 0.02f * d.s, d.ballZ + 0.025f * d.s, z);
                        var w = MeshBuilder.W(I(foot), I(toe), tw);
                        // ring centred half-way up the foot; flatten the sole side
                        fr.Add(new MeshBuilder.Ring
                        {
                            center = ankle + new Vector3(side * 0.004f * d.s, sole + ht * 0.5f, z),
                            rot = Quaternion.Euler(90, 0, 0), rx = hw, rz = ht * 0.5f, weight = w,
                            radial = ang => { float sn = Mathf.Sin(ang); return sn > 0 ? Mathf.Lerp(1f, 0.82f, sn * sn) : 1f; }
                        });
                    }
                    mb.AddLoft(footM, fr, 12, true, true);
                    if (footM != SKIN) { mb.bone = I(foot); mb.AddEllipsoid(footM, ankle + new Vector3(0, 0.005f, -0.01f * d.s), new Vector3(d.ankleR * 1.25f, d.ankleY * 0.7f, d.ankleR * 1.4f), 6, 10); }
                }
                // scale foot size by s (ellipsoid used absolute radii) -> rebuild radii correctly for small races
            }

            // ---------------- skirts / tassets ----------------
            if (skirtLong || skirtShort)
            {
                float len = skirtLong ? (d.hipY - d.ankleY - 0.03f * d.s) : (d.hipY - d.kneeY) * 0.62f;
                int m = armor == ArmorVisual.Chain || armor == ArmorVisual.Scale ? METAL : CLOTH1;
                var sk = new List<MeshBuilder.Ring>();
                int rings = 6;
                for (int r = 0; r <= rings; r++)
                {
                    float t = r / (float)rings;
                    float y = d.hipsY + d.T * 0.35f - t * (len + (d.hipsY + d.T * 0.35f - d.hipY));
                    float flare = Mathf.Lerp(1.02f, skirtLong ? 1.55f : 1.25f, Mathf.Pow(t, 0.8f));
                    float rx = Mathf.Lerp(d.waistW * 1.12f, d.hipW * 1.18f, Sat(t * 3)) * flare;
                    float rz = Mathf.Lerp(d.waistD * 1.12f * belly, d.pelvisD * 1.25f, Sat(t * 3)) * flare;
                    float k = Sat((t - 0.15f) / 0.85f) * 0.85f;
                    var ring = R(new Vector3(0, y, 0.005f), rx, rz, MeshBuilder.W(I(B.Hips)));
                    float kk = k;
                    ring.weightFn = ang =>
                    {
                        float cx = Mathf.Cos(ang);
                        float wgt = kk * Mathf.Clamp01(Mathf.Abs(cx) * 1.6f);
                        return MeshBuilder.W(I(B.Hips), cx > 0 ? I(B.ThighR) : I(B.ThighL), wgt);
                    };
                    sk.Add(ring);
                }
                mb.AddLoft(m, sk, 22, false, false);
                // inner side so the skirt is visible from below
                var inner = new List<MeshBuilder.Ring>();
                for (int i = 0; i < sk.Count; i++) { var rr = sk[i]; rr.rx *= 0.985f; rr.rz *= 0.985f; inner.Add(rr); }
                mb.AddLoft(CLOTH2, inner, 22, false, false, 1f, true);
            }
            if (tassets)
            {
                int m = armor == ArmorVisual.Plate ? METAL : LEATHER;
                for (int i = 0; i < 6; i++)
                {
                    float ang = Mathf.Lerp(20, 160, i / 5f) * Mathf.Deg2Rad;
                    if (i == 2 || i == 3) continue;
                    Vector3 c = new Vector3(Mathf.Cos(ang) * d.hipW * 1.12f, d.hipY - 0.02f * d.s, Mathf.Sin(ang) * d.pelvisD * 1.2f);
                    B tb = c.x > 0 ? B.ThighR : B.ThighL;
                    mb.bone = I(B.Hips); mb.bone2 = I(tb); mb.bone2Weight = 0.5f;
                    mb.Push();
                    mb.Translate(c);
                    mb.Rotate(Quaternion.LookRotation(new Vector3(c.x, 0, c.z).normalized, Vector3.up) * Quaternion.Euler(-8, 0, 0));
                    mb.AddBox(m, new Vector3(0, -0.07f * d.s, 0), new Vector3(0.1f * d.ws, 0.15f * d.s, 0.012f * d.s));
                    mb.Pop();
                    mb.bone2 = -1;
                }
                // back tassets
                for (int i = 0; i < 2; i++)
                {
                    float x = (i == 0 ? -1 : 1) * d.hipW * 0.5f;
                    mb.bone = I(B.Hips); mb.bone2 = I(i == 0 ? B.ThighL : B.ThighR); mb.bone2Weight = 0.4f;
                    mb.AddBox(m, new Vector3(x, d.hipY - 0.06f * d.s, -d.pelvisD * 1.15f), new Vector3(0.1f * d.ws, 0.14f * d.s, 0.012f * d.s));
                    mb.bone2 = -1;
                }
            }
            if (tabard)
            {
                mb.bone = I(B.Chest);
                var tb = new List<MeshBuilder.Ring>();
                // front panel
                mb.AddBox(CLOTH1, new Vector3(0, d.hipsY + d.T * 0.55f, d.chestD * 1.08f), new Vector3(d.chestW * 1.1f, d.T * 0.75f, 0.01f * d.s));
                mb.bone = I(B.Hips);
                mb.AddBox(CLOTH1, new Vector3(0, d.hipY - 0.12f * d.s, d.pelvisD * 1.28f), new Vector3(d.chestW * 0.95f, 0.28f * d.s, 0.01f * d.s));
                mb.AddBox(CLOTH1, new Vector3(0, d.hipY - 0.12f * d.s, -d.pelvisD * 1.28f), new Vector3(d.chestW * 0.95f, 0.28f * d.s, 0.01f * d.s));
                mb.bone = I(B.Chest);
                mb.AddBox(CLOTH1, new Vector3(0, d.hipsY + d.T * 0.55f, -d.chestD * 1.02f), new Vector3(d.chestW * 1.1f, d.T * 0.75f, 0.01f * d.s));
                // emblem
                mb.AddBox(TRIM, new Vector3(0, d.hipsY + d.T * 0.62f, d.chestD * 1.1f), new Vector3(0.05f, 0.07f, 0.006f) * d.s);
            }
            if (belt)
            {
                var br = new List<MeshBuilder.Ring>
                {
                    R(new Vector3(0, d.hipsY + d.T * 0.18f, 0.004f), Mathf.Lerp(d.hipW, d.waistW, 0.5f) * 1.09f * (skirtLong || skirtShort ? 1.06f : 1f), Mathf.Lerp(d.pelvisD, d.waistD, 0.5f) * 1.12f * belly, MeshBuilder.W(I(B.Hips))),
                    R(new Vector3(0, d.hipsY + d.T * 0.28f, 0.004f), Mathf.Lerp(d.hipW, d.waistW, 0.7f) * 1.09f * (skirtLong || skirtShort ? 1.06f : 1f), Mathf.Lerp(d.pelvisD, d.waistD, 0.7f) * 1.12f * belly, MeshBuilder.W(I(B.Hips), I(B.Spine), 0.3f)),
                };
                mb.AddLoft(armor == ArmorVisual.Robe ? CLOTH2 : LEATHER, br, 18, false, false);
                mb.bone = I(B.Hips);
                mb.AddBox(TRIM, new Vector3(0, d.hipsY + d.T * 0.23f, Mathf.Lerp(d.pelvisD, d.waistD, 0.6f) * 1.14f * belly), new Vector3(0.04f, 0.035f, 0.01f) * d.s);
                // pouch
                mb.AddBox(LEATHER, new Vector3(d.hipW * 0.9f, d.hipsY + d.T * 0.12f, d.pelvisD * 0.5f), new Vector3(0.035f, 0.06f, 0.05f) * d.s);
            }

            // ---------------- arms ----------------
            for (int side = -1; side <= 1; side += 2)
            {
                B clav = side < 0 ? B.ClavL : B.ClavR, up = side < 0 ? B.UpperArmL : B.UpperArmR, lo = side < 0 ? B.LowerArmL : B.LowerArmR, hand = side < 0 ? B.HandL : B.HandR;
                Vector3 sh = side < 0 ? d.shoulderL : d.shoulderR, el = side < 0 ? d.elbowL : d.elbowR, wr = side < 0 ? d.wristL : d.wristR;
                Vector3 dir = (el - sh).normalized;
                Quaternion rot = Quaternion.FromToRotation(Vector3.down, dir);
                float slInf = sleeveM == METAL ? 1.12f : (sleeveM == SKIN ? 1f : 1.06f);
                var upper = new List<MeshBuilder.Ring>
                {
                    R(sh - dir * 0.04f * d.s + new Vector3(-side * 0.02f * d.s, 0, 0), d.deltR * 0.9f * slInf, d.deltR * 0.9f * slInf, MeshBuilder.W(I(B.Chest), I(up), 0.35f), rot),
                    R(sh + dir * 0.03f * d.s, d.deltR * slInf, d.deltR * 0.98f * slInf, MeshBuilder.W(I(up)), rot),
                    R(sh + dir * d.upperArmLen * 0.4f, d.upperArmR * slInf, d.upperArmR * 1.06f * slInf, MeshBuilder.W(I(up)), rot, 0.003f * d.s),
                    R(sh + dir * d.upperArmLen * 0.82f, d.upperArmR * 0.84f * slInf, d.upperArmR * 0.86f * slInf, MeshBuilder.W(I(up)), rot),
                    R(el, d.elbowRad * slInf, d.elbowRad * slInf, MeshBuilder.W(I(up), I(lo), 0.5f), rot),
                    R(el + dir * d.foreArmLen * 0.12f, d.forearmR * slInf * 0.97f, d.forearmR * slInf * 0.95f, MeshBuilder.W(I(lo)), rot),
                };
                mb.AddLoft(sleeveM, upper, 12, false, true);
                bool flared = armor == ArmorVisual.Robe;
                float faInf = forearmM == METAL ? 1.2f : (forearmM == SKIN ? 1f : 1.08f);
                var fore = new List<MeshBuilder.Ring>
                {
                    R(el - dir * 0.01f * d.s, d.elbowRad * faInf, d.elbowRad * faInf, MeshBuilder.W(I(up), I(lo), 0.6f), rot),
                    R(el + dir * d.foreArmLen * 0.3f, d.forearmR * faInf, d.forearmR * 0.92f * faInf, MeshBuilder.W(I(lo)), rot),
                    R(el + dir * d.foreArmLen * 0.75f, d.forearmR * 0.78f * faInf * (flared ? 1.5f : 1f), d.forearmR * 0.72f * faInf * (flared ? 1.5f : 1f), MeshBuilder.W(I(lo)), rot),
                    R(wr - dir * 0.012f * d.s, d.wristRad * faInf * (flared ? 2.1f : 1f), d.wristRad * 0.85f * faInf * (flared ? 2.1f : 1f), MeshBuilder.W(I(lo), I(hand), 0.3f), rot),
                };
                mb.AddLoft(forearmM, fore, 12, false, !flared);
                if (flared)
                {
                    // wrist skin under a flared sleeve
                    var w = new List<MeshBuilder.Ring> { R(wr - dir * 0.06f * d.s, d.wristRad * 0.95f, d.wristRad * 0.85f, MeshBuilder.W(I(lo)), rot), R(wr + dir * 0.01f * d.s, d.wristRad * 0.9f, d.wristRad * 0.8f, MeshBuilder.W(I(lo), I(hand), 0.5f), rot) };
                    mb.AddLoft(SKIN, w, 10, false, false);
                }
                if (bracers && forearmM != METAL)
                {
                    var br = new List<MeshBuilder.Ring> { R(el + dir * d.foreArmLen * 0.45f, d.forearmR * 1.2f, d.forearmR * 1.12f, MeshBuilder.W(I(lo)), rot), R(wr - dir * 0.015f * d.s, d.wristRad * 1.35f, d.wristRad * 1.2f, MeshBuilder.W(I(lo)), rot) };
                    mb.AddLoft(LEATHER, br, 12, true, true);
                }
                // hand: palm (faces the thigh) + curled fingers + thumb
                mb.bone = I(hand); mb.bone2 = -1;
                float hsz = (handM == SKIN ? 1f : 1.1f) * d.s * (skel ? 0.7f : 1f) * Mathf.Lerp(1f, a.bulk, 0.25f) * (a.strong ? 1.06f : 1f) * (d.fem ? 0.9f : 1f);
                if (look.dragon && handM == SKIN) hsz *= 1.1f;
                BuildHand(mb, handM, I(hand), wr, rot, side, hsz, handM == METAL, look.dragon && handM == SKIN, HORN);

                // pauldrons
                if (pauldrons || (armor == ArmorVisual.Leather) || mantle)
                {
                    mb.bone = I(up); mb.bone2 = I(clav); mb.bone2Weight = 0.35f;
                    int pm = pauldrons ? METAL : (mantle ? CLOTH2 : LEATHER);
                    float pr = pauldrons ? 1.38f : (mantle ? 1.45f : 1.2f);
                    Vector3 pc = sh + new Vector3(side * 0.01f * d.s, 0.02f * d.s, 0);
                    mb.AddEllipsoid(pm, pc, new Vector3(d.deltR * pr, d.deltR * pr * 0.75f, d.deltR * pr), 7, 12,
                        p => new Vector3(p.x, p.y < -d.deltR * 0.2f ? -d.deltR * 0.2f + (p.y + d.deltR * 0.2f) * 0.15f : p.y, p.z));
                    if (pauldrons)
                    {
                        mb.AddEllipsoid(TRIM, pc + new Vector3(0, -d.deltR * 0.25f, 0), new Vector3(d.deltR * pr * 1.02f, d.deltR * 0.12f, d.deltR * pr * 1.02f), 3, 12);
                        mb.AddEllipsoid(METAL, pc + new Vector3(side * d.deltR * 0.4f, -d.deltR * 0.6f, 0), new Vector3(d.deltR * pr * 0.9f, d.deltR * pr * 0.45f, d.deltR * pr * 0.95f), 6, 10);
                    }
                    mb.bone2 = -1;
                }
            }

            if (armor == ArmorVisual.Plate && !skel) BuildPlate(mb, a, d);

            // mantle (fur) collar
            if (mantle)
            {
                mb.bone = I(B.Chest);
                var mr = new List<MeshBuilder.Ring>
                {
                    R(new Vector3(0, d.shoulderY - 0.07f * d.s, -0.005f), d.shoulderW * 1.08f, d.chestD * 1.12f, MeshBuilder.W(I(B.Chest)), null, 0, ang => 1f + 0.06f * Mathf.Sin(ang * 9)),
                    R(new Vector3(0, d.shoulderY + 0.03f * d.s, -0.01f), d.neckR * 2.4f, d.neckR * 2.0f, MeshBuilder.W(I(B.Chest)), null, 0, ang => 1f + 0.08f * Mathf.Sin(ang * 7)),
                };
                mb.AddLoft(CLOTH2, mr, 24, false, false);
            }

            // ---------------- cape ----------------
            if (a.cape)
            {
                // a draped cloth sheet: wraps the shoulders from the collarbones round the back, flares toward the hem
                // and falls in folds that deepen as it hangs; each row rides the cape chain so it swings as one
                float capeTop = d.shoulderY + 0.025f * d.s;
                float bottom = d.ankleY + 0.12f * d.s;
                B[] cb = { B.Cape0, B.Cape1, B.Cape2, B.Cape3 };
                bool armoured = g.armor == ArmorVisual.Plate;
                float wrapTop = d.shoulderW * (armoured ? 1.28f : 1.08f), wrapBot = d.shoulderW * 1.55f;
                float depthTop = d.chestD * (armoured ? 1.45f : 1.2f), depthBot = d.chestD * 1.9f + 0.05f * d.s;
                Func<float, float, Vector3> P = (u, v) =>
                {
                    float th = Mathf.Lerp(-1.95f, 1.95f, u);                   // radians round the back (0 = straight behind)
                    float y = Mathf.Lerp(capeTop, bottom, v);
                    float rx = Mathf.Lerp(wrapTop, wrapBot, Mathf.Pow(v, 0.8f));
                    float rz = Mathf.Lerp(depthTop, depthBot, Mathf.Sqrt(v));
                    float fold = 1f + (0.02f + 0.07f * v) * Mathf.Sin(th * 7f + 0.6f) + 0.03f * v * Mathf.Sin(th * 13f);
                    float cz = -d.chestD * 0.15f - 0.02f * d.s * v;
                    return new Vector3(Mathf.Sin(th) * rx * fold, y, cz - Mathf.Cos(th) * rz * fold);
                };
                BoneWeight CW(float u, float v)
                {
                    float seg = Mathf.Clamp(v * 3f, 0, 2.999f);
                    int i0 = Mathf.FloorToInt(seg);
                    return v < 0.04f ? MeshBuilder.W(I(B.Chest)) : MeshBuilder.W(I(cb[i0]), I(cb[i0 + 1]), seg - i0);
                }
                Shell(mb, CAPE, 28, 10, P, 0.006f * d.s, CW, new Vector3(0, (capeTop + bottom) * 0.5f, 0.05f));
                // collar
                mb.bone = I(B.Chest);
                var col = new List<MeshBuilder.Ring>
                {
                    R(new Vector3(0, d.shoulderY - 0.005f * d.s, -0.01f), d.neckR * 2.2f, d.neckR * 1.9f, MeshBuilder.W(I(B.Chest))),
                    R(new Vector3(0, d.shoulderY + 0.07f * d.s, -0.035f), d.neckR * 2.3f, d.neckR * 2.0f, MeshBuilder.W(I(B.Chest)), null, 0, ang => Mathf.Sin(ang) > 0.2f ? 0.75f : 1f),
                };
                mb.AddLoft(CAPE, col, 16, false, false);
            }

            // ---------------- tail ----------------
            if (a.tail && a.race == RaceId.Tiefling)
            {
                var tr = new List<MeshBuilder.Ring>();
                B[] tb = { B.Tail0, B.Tail1, B.Tail2, B.Tail3 };
                Vector3 t0 = new Vector3(0, d.hipsY - 0.06f * d.s, -d.pelvisD * 0.95f);
                Vector3[] tp = { t0, t0 + new Vector3(0, -0.12f, -0.12f) * d.s, t0 + new Vector3(0, -0.28f, -0.2f) * d.s, t0 + new Vector3(0, -0.45f, -0.22f) * d.s, t0 + new Vector3(0, -0.6f, -0.18f) * d.s };
                for (int i = 0; i < tp.Length; i++)
                {
                    float rr = Mathf.Lerp(0.03f, 0.008f, i / (float)(tp.Length - 1)) * d.s;
                    Vector3 dir = i < tp.Length - 1 ? tp[i + 1] - tp[i] : tp[i] - tp[i - 1];
                    tr.Add(R(tp[i], rr, rr, MeshBuilder.W(I(tb[Mathf.Min(i, 3)])), Quaternion.FromToRotation(Vector3.up, dir.normalized)));
                }
                mb.AddLoft(SKIN, tr, 8, true, true);
                mb.bone = I(B.Tail3);
                mb.Push(); mb.Translate(tp[4]);
                mb.AddEllipsoid(SKIN, Vector3.zero, new Vector3(0.025f, 0.035f, 0.006f) * d.s, 5, 8);
                mb.Pop();
            }

            // ---------------- head ----------------
            if (!a.headless) BuildHeadSculpt(mb, a, g, d, look);
            else
            {
                // hollow collar with glowing eyes (animated armour)
                mb.bone = I(B.Chest);
                mb.AddEllipsoid(DARK, new Vector3(0, d.shoulderY + 0.02f, 0), new Vector3(d.neckR * 1.8f, 0.02f, d.neckR * 1.6f), 4, 12);
            }
        }




        /// <summary>Skull-cap of hair between a hairline curve and the crown.</summary>
        static void AddCap(MeshBuilder mb, int sub, Vector3 c, Vector3 r, float frontDeg, float sideDeg, float backDeg, Func<Vector3, Vector3> deform = null, int lat = 9, int lon = 20)
        {
            int start = mb.verts.Count;
            for (int y = 0; y <= lat; y++)
            {
                float v = y / (float)lat;
                for (int x = 0; x <= lon; x++)
                {
                    float u = x / (float)lon;
                    float th = u * Mathf.PI * 2;
                    // th: 0 = +x, pi/2 = +z (front), pi = -x, 3pi/2 = back
                    float fz = Mathf.Sin(th);
                    float limit = fz >= 0 ? Mathf.Lerp(sideDeg, frontDeg, fz) : Mathf.Lerp(sideDeg, backDeg, -fz);
                    float phi = v * limit * Mathf.Deg2Rad;
                    Vector3 dd = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    Vector3 p = Vector3.Scale(dd, r);
                    if (deform != null) p = deform(p);
                    mb.AddVertexRaw(mb.M.MultiplyPoint3x4(c + p), mb.M.MultiplyVector(dd).normalized, new Vector2(u * 2, v), MeshBuilder.W(mb.bone));
                }
            }
            for (int y = 0; y < lat; y++)
                for (int x = 0; x < lon; x++)
                {
                    int a0 = start + y * (lon + 1) + x, b0 = a0 + 1, c0 = a0 + lon + 1, d0 = c0 + 1;
                    mb.Tri(sub, a0, b0, d0); mb.Tri(sub, a0, d0, c0);
                }
            mb.RecalcNormalsRange(start, mb.verts.Count, sub);
            // thickness rim: a second slightly smaller cap facing inward is unnecessary; add rim ring
        }




        static void BuildHood(MeshBuilder mb, Dims d)
        {
            mb.bone = I(B.Head);
            AddCap(mb, CLOTH1, d.headC + new Vector3(0, 0.005f, -0.01f), d.headRad * 1.18f, 40, 115, 150);
            var rings = new List<MeshBuilder.Ring>
            {
                R(d.headC + new Vector3(0, -d.headRad.y * 0.9f, -d.headRad.z * 0.2f), d.headRad.x * 1.3f, d.headRad.z * 1.1f, MeshBuilder.W(I(B.Head), I(B.Neck), 0.5f)),
                R(new Vector3(0, d.shoulderY + 0.01f * d.s, -0.01f), d.shoulderW * 0.8f, d.chestD * 1.15f, MeshBuilder.W(I(B.Chest))),
            };
            mb.AddLoft(CLOTH1, rings, 16, false, false);
        }

        static void BuildHelmet(MeshBuilder mb, Dims d, GearLook g)
        {
            mb.bone = I(B.Head);
            AddCap(mb, METAL, d.headC, d.headRad * 1.12f, 70, 108, 118);
            var r = new List<MeshBuilder.Ring> { R(d.headC + Vector3.up * d.headRad.y * 0.18f, d.headRad.x * 1.14f, d.headRad.z * 1.14f, MeshBuilder.W(I(B.Head))), R(d.headC + Vector3.up * d.headRad.y * 0.28f, d.headRad.x * 1.14f, d.headRad.z * 1.14f, MeshBuilder.W(I(B.Head))) };
            mb.AddLoft(TRIM, r, 20, false, false);
            mb.AddBox(METAL, d.headC + new Vector3(0, -0.01f * d.hs, d.headRad.z * 1.1f), new Vector3(0.012f, 0.07f, 0.01f) * d.hs);
        }

        // ---------------------------------------------------------------------------------
        public static Material[] BuildMaterials(Appearance a, GearLook g, Texture2D face)
        {
            var mats = new Material[SUBS];
            var look = RaceLooks.Looks[a.race];
            bool scaled = look.dragon;
            Color skin = a.skin;
            if (a.rotting) skin = Color.Lerp(skin, new Color(.42f, .48f, .36f), 0.6f);
            if (a.ghostly)
            {
                var gm = MatLib.Ghost(new Color(.45f, .75f, .9f, .22f), new Color(.6f, .95f, 1f, 1f));
                for (int i = 0; i < SUBS; i++) mats[i] = gm;
                return mats;
            }
            mats[SKIN] = a.skeletal ? MatLib.Lit(new Color(.82f, .78f, .66f), TexId.Bone, .3f) : MatLib.Lit(skin, scaled ? TexId.Scales : TexId.Skin, scaled ? .42f : .36f, 0, scaled ? 1.5f : 1f, scaled ? 0.55f : 0.35f);
            mats[HAIR] = MatLib.Lit(a.hair, TexId.Hair, .35f, 0, 1f, 1f);
            mats[EYEW] = MatLib.Lit(new Color(.88f, .86f, .82f), null, .85f);
            Color eyeC = a.glowEyes.a > 0 ? a.glowEyes : a.eyes;
            // glowEyes' alpha is the strength: 1 for the undead's burning stare, less for a faint gleam
            mats[IRIS] = a.glowEyes.a > 0 ? MatLib.Emissive(new Color(eyeC.r, eyeC.g, eyeC.b), new Color(eyeC.r, eyeC.g, eyeC.b) * 3f * a.glowEyes.a) : MatLib.Emissive(eyeC, eyeC * 0.15f);
            mats[CLOTH1] = MatLib.Lit(a.cloth1, TexId.Fabric, .12f, 0, 3f, .6f);
            mats[CLOTH2] = MatLib.Lit(a.cloth2, g.armor == ArmorVisual.Hide ? TexId.Hair : TexId.Fabric, .1f, 0, 3f, .6f);
            mats[LEATHER] = MatLib.Lit(g.armor == ArmorVisual.Hide ? new Color(.36f, .28f, .2f) : new Color(.3f, .2f, .13f), TexId.Leather, .32f, 0, 2f, .8f);
            TexId metalTex = g.armor == ArmorVisual.Chain ? TexId.Fabric : (g.armor == ArmorVisual.Scale ? TexId.Scales : TexId.Metal);
            Color metalC = a.headless ? new Color(.3f, .3f, .32f) : a.metalTint.a > 0 ? a.metalTint : new Color(.58f, .58f, .6f);
            // plate is smooth, lightly worked steel; mail and scale keep their pattern
            bool plate = g.armor == ArmorVisual.Plate;
            mats[METAL] = MatLib.Lit(metalC, metalTex, g.armor == ArmorVisual.Chain ? .45f : plate ? .66f : .62f, .85f, g.armor == ArmorVisual.Chain ? 6f : plate ? 5f : 2f, plate ? 0.3f : 1f);
            mats[TRIM] = MatLib.Lit(new Color(.72f, .55f, .27f), TexId.Metal, .7f, 1f, 1f, .5f);
            mats[HORN] = MatLib.Lit(a.horn, null, .5f);
            mats[DARK] = MatLib.Lit(new Color(.07f, .04f, .04f), null, .3f);
            mats[FACE] = MatLib.Face(face, scaled);
            var cape = new Material(MatLib.Lit(a.capeColor, TexId.Fabric, .15f, 0, 3f, .5f));
            cape.SetFloat("_Cull", 0);
            cape.doubleSidedGI = true;
            mats[CAPE] = cape;
            return mats;
        }
    }
}
