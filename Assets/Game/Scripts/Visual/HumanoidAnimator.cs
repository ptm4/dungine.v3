using System;
using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    public enum AnimAct
    {
        None, Slash, Overhead, Thrust, Punch, Bow, Crossbow, Throw, CastPoint, CastRaise, CastTouch, Heal, Drink,
        Hit, Dodge, Cheer, Interact, Shove, Breath, Roar, Claw, Bite, Bless, Draw, Stomp
    }

    public enum LifeState { Alive, Downed, Dead }

    public enum MotionStyle { Normal, Zombie, Ghoul, Skeleton, Ghost, Noble, Brute, Child }

    /// <summary>How a humanoid holds what it carries; picks stances and attack clips.</summary>
    public enum Grip { Unarmed, OneHand, Shield, TwoHand, Polearm, Bow, Crossbow }

    /// <summary>Common interface for every procedural animator (humanoid, quadruped, blob, swarm).</summary>
    public interface IAnimDriver
    {
        void Play(AnimAct act, Action onImpact = null, float speedMul = 1f);
        void SetCombat(bool on);
        void SetLife(LifeState s, bool instant = false);
        void LookAt(Vector3? worldPoint);
        void SetTalking(bool on);
        bool Busy { get; }
        LifeState Life { get; }
        float Height { get; }
    }

    public static class Anim
    {
        public static float Smooth(float a, float b, float t) { t = Mathf.Clamp01((t - a) / Mathf.Max(1e-4f, b - a)); return t * t * (3 - 2 * t); }
        /// <summary>0 before a, rises to 1 at b, holds to c, falls to 0 at d.</summary>
        public static float Env(float t, float a, float b, float c, float d) => Smooth(a, b, t) * (1 - Smooth(c, d, t));

        public static float Duration(AnimAct a)
        {
            switch (a)
            {
                case AnimAct.Slash: return 0.95f;
                case AnimAct.Overhead: return 1.15f;
                case AnimAct.Thrust: return 0.85f;
                case AnimAct.Punch: return 0.6f;
                case AnimAct.Bow: return 1.25f;
                case AnimAct.Crossbow: return 1.0f;
                case AnimAct.Throw: return 0.9f;
                case AnimAct.CastPoint: return 1.1f;
                case AnimAct.CastRaise: return 1.4f;
                case AnimAct.CastTouch: return 1.0f;
                case AnimAct.Heal: return 1.2f;
                case AnimAct.Drink: return 1.3f;
                case AnimAct.Hit: return 0.55f;
                case AnimAct.Dodge: return 0.6f;
                case AnimAct.Cheer: return 1.6f;
                case AnimAct.Interact: return 1.0f;
                case AnimAct.Shove: return 0.8f;
                case AnimAct.Breath: return 1.3f;
                case AnimAct.Roar: return 1.4f;
                case AnimAct.Claw: return 0.8f;
                case AnimAct.Bite: return 0.8f;
                case AnimAct.Bless: return 1.3f;
                case AnimAct.Draw: return 0.5f;
                case AnimAct.Stomp: return 1.0f;
                default: return 0.5f;
            }
        }

        public static float Impact(AnimAct a)
        {
            switch (a)
            {
                case AnimAct.Slash: return 0.45f;
                case AnimAct.Overhead: return 0.52f;
                case AnimAct.Thrust: return 0.45f;
                case AnimAct.Punch: return 0.42f;
                case AnimAct.Bow: return 0.66f;
                case AnimAct.Crossbow: return 0.5f;
                case AnimAct.Throw: return 0.5f;
                case AnimAct.CastPoint: return 0.5f;
                case AnimAct.CastRaise: return 0.55f;
                case AnimAct.CastTouch: return 0.5f;
                case AnimAct.Heal: return 0.55f;
                case AnimAct.Drink: return 0.6f;
                case AnimAct.Shove: return 0.45f;
                case AnimAct.Breath: return 0.5f;
                case AnimAct.Roar: return 0.45f;
                case AnimAct.Claw: return 0.45f;
                case AnimAct.Bite: return 0.45f;
                case AnimAct.Bless: return 0.55f;
                case AnimAct.Draw: return 0.5f;
                case AnimAct.Stomp: return 0.5f;
                default: return 0.5f;
            }
        }
    }

    // =====================================================================================================
    //  Keyed poses: an action is a handful of poses on a timeline, joined by smooth (Hermite) curves so the
    //  motion accelerates into a strike and eases out of it, with a wind-up before and follow-through after.
    // =====================================================================================================

    /// <summary>A pose as offsets from the stance: bone rotations (degrees), a pelvis offset and foot steps.</summary>
    public class Pose
    {
        public const int N = (int)B.COUNT;
        public readonly Vector3[] r = new Vector3[N];
        public Vector3 hip;              // pelvis offset (metres at human scale)
        public Vector3 stepR, stepL;     // how far each foot moves from where it stands (metres at human scale)
        public float lean;               // whole-body pitch (degrees, + forward)
        public Vector3 wgrip, waim;      // two-handed weapons: main-hand grip offset (chest space, metres) and aim offset (pitch, yaw, roll)

        public Pose R(B b, float x, float y = 0, float z = 0) { r[(int)b] += new Vector3(x, y, z); return this; }
        /// <summary>The same rotation on both sides, mirrored for the left.</summary>
        public Pose Both(B right, B left, float x, float y = 0, float z = 0) { r[(int)right] += new Vector3(x, y, z); r[(int)left] += new Vector3(x, -y, -z); return this; }
        public Pose Hip(float x, float y, float z) { hip = new Vector3(x, y, z); return this; }
        public Pose StepR(float x, float z) { stepR = new Vector3(x, 0, z); return this; }
        public Pose StepL(float x, float z) { stepL = new Vector3(x, 0, z); return this; }
        public Pose Lean(float a) { lean = a; return this; }
        public Pose WGrip(float x, float y, float z) { wgrip = new Vector3(x, y, z); return this; }
        public Pose WAim(float pitch, float yaw, float roll = 0) { waim = new Vector3(pitch, yaw, roll); return this; }
        public Pose Scaled(float k)
        {
            var p = new Pose();
            for (int i = 0; i < N; i++) p.r[i] = r[i] * k;
            p.hip = hip * k; p.stepR = stepR * k; p.stepL = stepL * k; p.lean = lean * k; p.wgrip = wgrip * k; p.waim = waim * k;
            return p;
        }
    }

    public class Clip
    {
        public readonly List<float> t = new List<float>();
        public readonly List<Pose> p = new List<Pose>();
        /// <summary>Adds a key at normalised time u (0..1). The stance (an empty pose) is implied at 0 and 1.</summary>
        public Clip K(float u, Pose pose) { t.Add(u); p.Add(pose); return this; }

        public void Eval(float u, Pose outp)
        {
            int n = t.Count + 2;
            float T(int i) => i == 0 ? 0f : i == n - 1 ? 1f : t[i - 1];
            Pose P(int i) => i == 0 || i == n - 1 ? null : p[i - 1];
            int k = 0;
            while (k < n - 2 && u > T(k + 1)) k++;
            float t0 = T(k), t1 = T(k + 1);
            float s = Mathf.Clamp01((u - t0) / Mathf.Max(1e-4f, t1 - t0));
            // cubic Hermite with Catmull-Rom style tangents (zero at the ends)
            float h00 = 2 * s * s * s - 3 * s * s + 1, h10 = s * s * s - 2 * s * s + s, h01 = -2 * s * s * s + 3 * s * s, h11 = s * s * s - s * s;
            float dt = t1 - t0;
            Pose a = P(k), b = P(k + 1), pa = k > 0 ? P(k - 1) : null, nb = k + 2 < n ? P(k + 2) : null;
            float ta0 = k > 0 ? T(k - 1) : t0, tb1 = k + 2 < n ? T(k + 2) : t1;
            bool endA = k == 0, endB = k + 1 == n - 1;
            Vector3 Herm(Vector3 va, Vector3 vb, Vector3 vpa, Vector3 vnb)
            {
                Vector3 ma = endA ? Vector3.zero : (vb - vpa) / Mathf.Max(1e-4f, t1 - ta0) * dt;
                Vector3 mb = endB ? Vector3.zero : (vnb - va) / Mathf.Max(1e-4f, tb1 - t0) * dt;
                return h00 * va + h10 * ma + h01 * vb + h11 * mb;
            }
            for (int i = 0; i < Pose.N; i++)
                outp.r[i] += Herm(a != null ? a.r[i] : Vector3.zero, b != null ? b.r[i] : Vector3.zero, pa != null ? pa.r[i] : Vector3.zero, nb != null ? nb.r[i] : Vector3.zero);
            Vector3 F(Func<Pose, Vector3> f) => Herm(a != null ? f(a) : Vector3.zero, b != null ? f(b) : Vector3.zero, pa != null ? f(pa) : Vector3.zero, nb != null ? f(nb) : Vector3.zero);
            outp.hip += F(q => q.hip);
            outp.stepR += F(q => q.stepR);
            outp.stepL += F(q => q.stepL);
            outp.lean += F(q => new Vector3(q.lean, 0, 0)).x;
            outp.wgrip += F(q => q.wgrip);
            outp.waim += F(q => q.waim);
        }
    }

    /// <summary>
    /// Procedural humanoid animation: a real gait (feet planted on the ground by leg IK, heel-to-toe roll, pelvis bob,
    /// sway and twist, counter-rotating shoulders), weight-shifting idles, grip-specific combat stances, keyed actions
    /// with wind-up and follow-through, an off hand that stays on two-handed weapons, reactions, falls and look-at.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class HumanoidAnimator : MonoBehaviour, IAnimDriver
    {
        public HumanoidRig rig;
        public MotionStyle style;
        public float walkSpeedRef = 1.6f;
        /// <summary>When set, nothing advances on its own: call Tick yourself (dev captures step frame by frame).</summary>
        public bool manual;
        /// <summary>Sit on something seatHeight metres high (the actor's origin should be over the seat).</summary>
        public bool seated; public float seatHeight = 0.46f;

        const int N = (int)B.COUNT;
        readonly Vector3[] rot = new Vector3[N];
        readonly Pose clipPose = new Pose();
        Transform model;
        Vector3 lastPos; float lastYaw; bool first = true;
        Vector3 vel; float speed, turnRate, accel, t, stanceW, targetStance, talkT;
        bool talking;
        AnimAct act; Clip clip; float actT, actDur, impactAt, actSpeed = 1; Action onImpact; bool impactFired; int variant;
        LifeState life = LifeState.Alive; float lifeT = 1f; float fallSide = 1f;
        Vector3? lookAt; float lookW, headYaw, headPitch;
        float capeSwing, capeVel;
        Vector3 hipOffset; float bodyPitch, bodyRoll, modelLift, pelvisYaw, pelvisRoll;
        float seed;
        public Grip grip;

        public bool Busy => act != AnimAct.None;
        public LifeState Life => life;
        public float Height => rig ? rig.height : 1.8f;
        public float Speed => speed;

        // ---------- legs ----------
        class Foot
        {
            public int side;                 // -1 left, +1 right
            public B thigh, shin, foot, toe;
            public bool stepping, gaitStep, placed;
            public Vector3 plant, from, to;  // world ground points (under the ankle)
            public float stepT, stepDur, roll, stanceS = -1;
            public Vector3 ground;            // where the foot touches this frame
            public float yawOff;
        }
        readonly Foot[] feet = new Foot[2];
        float ankleH, ballZ, heelZ, hipX, legLen, sizeK, gaitPhase, runW, moveW, ikW, hipDrop, idleStepCool;

        public void Init(HumanoidRig r, MotionStyle s)
        {
            rig = r; style = s;
            model = r.transform.Find("Model");
            lastPos = transform.position; lastYaw = transform.eulerAngles.y;
            seed = UnityEngine.Random.value * 100f;
            walkSpeedRef = 1.6f * Mathf.Lerp(0.7f, 1f, r.scale);
            for (int i = 0; i < 2; i++)
            {
                bool right = i == 1;
                feet[i] = new Foot
                {
                    side = right ? 1 : -1,
                    thigh = right ? B.ThighR : B.ThighL, shin = right ? B.ShinR : B.ShinL, foot = right ? B.FootR : B.FootL, toe = right ? B.ToeR : B.ToeL,
                    yawOff = right ? 7f : -7f
                };
            }
            var f0 = feet[1];
            float l1 = r.bindPos[(int)f0.shin].magnitude, l2 = r.bindPos[(int)f0.foot].magnitude;
            legLen = l1 + l2;
            // ankle height & foot landmarks from the bind pose (model space)
            Vector3 hipsBind = r.bindPos[(int)B.Hips], thighBind = r.bindPos[(int)f0.thigh], shinBind = r.bindPos[(int)f0.shin], footBind = r.bindPos[(int)f0.foot];
            ankleH = Mathf.Max(0.02f, (hipsBind + thighBind + shinBind + footBind).y);
            ballZ = Mathf.Max(0.03f, r.bindPos[(int)f0.toe].z);
            heelZ = 0.055f * r.scale;
            hipX = Mathf.Abs(thighBind.x);
            sizeK = Mathf.Sqrt(Mathf.Max(0.3f, legLen) / 0.84f);
            grip = GripOf(r.gear);
        }

        public static Grip GripOf(GearLook g)
        {
            if (g == null || g.main == WeaponVisual.None) return g != null && g.off == WeaponVisual.Shield ? Grip.Shield : Grip.Unarmed;
            var w = g.main;
            if (HumanoidBuilder.IsBow(w)) return Grip.Bow;
            if (w == WeaponVisual.LightCrossbow || w == WeaponVisual.HeavyCrossbow) return Grip.Crossbow;
            if (w == WeaponVisual.Glaive || w == WeaponVisual.Halberd || w == WeaponVisual.Spear || w == WeaponVisual.Trident || w == WeaponVisual.Quarterstaff || w == WeaponVisual.Staff) return Grip.Polearm;
            if (w == WeaponVisual.Greatsword || w == WeaponVisual.Greataxe || w == WeaponVisual.Maul || w == WeaponVisual.Greatclub) return Grip.TwoHand;
            if (g.off == WeaponVisual.Shield) return Grip.Shield;
            return Grip.OneHand;
        }

        public void Play(AnimAct a, Action impact = null, float speedMul = 1f)
        {
            if (life != LifeState.Alive && a != AnimAct.Hit) { impact?.Invoke(); return; }
            if (onImpact != null && !impactFired) { var old = onImpact; onImpact = null; old(); }
            variant = UnityEngine.Random.Range(0, 2);
            act = a; actT = 0; actSpeed = speedMul; actDur = Anim.Duration(a); impactAt = Anim.Impact(a) * actDur;
            clip = HumanoidClips.Get(a, grip, variant);
            onImpact = impact; impactFired = false;
        }

        /// <summary>Plays a specific variant (dev reviews).</summary>
        public void PlayVariant(AnimAct a, int v) { Play(a); variant = v; clip = HumanoidClips.Get(a, grip, v); }

        public void SetCombat(bool on)
        {
            if (on == (targetStance > 0.5f)) return;
            targetStance = on ? 1 : 0;
            if (rig && rig.gear != null && (rig.mainWeapon || rig.offWeapon))
            {
                if (act == AnimAct.None) Play(AnimAct.Draw, () => HumanoidBuilder.SetDrawn(rig, on));
                else HumanoidBuilder.SetDrawn(rig, on);
            }
        }

        public void SetLife(LifeState s, bool instant = false)
        {
            if (s == life) return;
            life = s;
            lifeT = instant ? 1f : 0f;
            fallSide = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            act = AnimAct.None; clip = null;
            if (onImpact != null && !impactFired) { var o = onImpact; onImpact = null; o(); }
        }

        public void LookAt(Vector3? p) => lookAt = p;
        public void SetTalking(bool on) => talking = on;

        void LateUpdate() { if (!manual) Tick(Time.deltaTime); }

        public void Tick(float dt)
        {
            if (!rig || !model) return;
            if (dt <= 0) return;
            dt = Mathf.Min(dt, 0.1f);
            t += dt;
            Vector3 p = transform.position;
            float yaw = transform.eulerAngles.y;
            if (first) { lastPos = p; lastYaw = yaw; first = false; }
            Vector3 v = (p - lastPos) / dt; v.y = 0;
            if (v.magnitude > 20f) v = Vector3.zero;   // teleport
            float prevSpeed = speed;
            vel = Vector3.Lerp(vel, v, 1 - Mathf.Exp(-dt * 12f));
            speed = vel.magnitude;
            accel = Mathf.Lerp(accel, (speed - prevSpeed) / dt, 1 - Mathf.Exp(-dt * 6f));
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(lastYaw, yaw) / dt, 1 - Mathf.Exp(-dt * 8f));
            lastPos = p; lastYaw = yaw;
            stanceW = Mathf.MoveTowards(stanceW, targetStance, dt * 3f);
            lifeT = Mathf.Min(1f, lifeT + dt / (life == LifeState.Alive ? 1.0f : 1.4f));
            if (rig.gear != null) grip = GripOf(rig.gear);
            if (act != AnimAct.None)
            {
                actT += dt * actSpeed;
                if (!impactFired && actT >= impactAt)
                {
                    impactFired = true;
                    var cb = onImpact; onImpact = null;
                    cb?.Invoke();
                }
                if (actT >= actDur) { act = AnimAct.None; clip = null; }
            }

            Evaluate(dt);
            Apply();
            SolveLegs(dt);
            DriveTwoHanded();
        }

        void Clear()
        {
            for (int i = 0; i < N; i++) rot[i] = Vector3.zero;
            hipOffset = Vector3.zero; bodyPitch = 0; bodyRoll = 0; modelLift = 0; pelvisYaw = 0; pelvisRoll = 0;
            for (int i = 0; i < N; i++) clipPose.r[i] = Vector3.zero;
            clipPose.hip = clipPose.stepR = clipPose.stepL = Vector3.zero; clipPose.lean = 0;
            clipPose.wgrip = clipPose.waim = Vector3.zero;
        }

        static float S(float x) => Mathf.Sin(x);
        static float C(float x) => Mathf.Cos(x);
        const float TAU = Mathf.PI * 2f;

        void Set(B b, Vector3 e) => rot[(int)b] += e;
        void Set(B b, float x, float y = 0, float z = 0) => rot[(int)b] += new Vector3(x, y, z);
        void Sym(B r, B l, Vector3 e) { rot[(int)r] += e; rot[(int)l] += new Vector3(e.x, -e.y, -e.z); }

        bool IkAllowed => style != MotionStyle.Ghost && !seated;

        void Evaluate(float dt)
        {
            Clear();
            float H = rig.height;
            float sc = rig.scale;
            bool alive = life == LifeState.Alive;

            // ---------------- base: relaxed idle ----------------
            float br = S(t * 1.25f + seed);                               // breathing
            Set(B.Chest, br * 1.4f, 0, 0);
            Set(B.Spine, -br * 0.4f, 0, 0);
            Sym(B.ClavR, B.ClavL, new Vector3(0, 0, br * 0.8f));
            Sym(B.UpperArmR, B.UpperArmL, new Vector3(3 + br * 1.2f, 0, -4.5f));
            Sym(B.LowerArmR, B.LowerArmL, new Vector3(-14, 0, 0));
            Sym(B.HandR, B.HandL, new Vector3(-4, 0, 6));
            // weight slowly shifting from foot to foot, and a wandering gaze
            float calm = 1f - Mathf.Max(moveW, stanceW * 0.7f);
            float shift = (S(t * 0.55f + seed) * 0.6f + S(t * 0.23f + seed * 2f) * 0.4f) * calm;
            hipOffset.x += shift * 0.022f * sc;
            pelvisRoll += -shift * 2.2f;
            Set(B.Spine, 0, 0, shift * 1.5f);
            float gazeY = (Mathf.PerlinNoise(t * 0.18f, seed) - 0.5f) * 30f * calm, gazeX = (Mathf.PerlinNoise(seed, t * 0.15f) - 0.5f) * 10f * calm;
            Set(B.Head, gazeX, gazeY * 0.65f, 0); Set(B.Neck, 0, gazeY * 0.35f, 0);

            switch (style)
            {
                case MotionStyle.Zombie:
                    Set(B.Spine, new Vector3(14, 0, 4)); Set(B.Chest, new Vector3(10, 0, 0)); Set(B.Neck, new Vector3(-10, 0, 8)); Set(B.Head, new Vector3(-6, 0, 10));
                    Sym(B.UpperArmR, B.UpperArmL, new Vector3(-55 + S(t * 1.1f) * 5, 0, 6)); Sym(B.LowerArmR, B.LowerArmL, new Vector3(-10, 0, 0));
                    hipOffset.y -= 0.03f * H;
                    break;
                case MotionStyle.Ghoul:
                    Set(B.Spine, new Vector3(28, 0, 0)); Set(B.Chest, new Vector3(12, 0, 0)); Set(B.Neck, new Vector3(-22, 0, 0)); Set(B.Head, new Vector3(-18, 0, 0));
                    Sym(B.UpperArmR, B.UpperArmL, new Vector3(-30, 0, 18)); Sym(B.LowerArmR, B.LowerArmL, new Vector3(-40, 0, 0));
                    hipOffset.y -= 0.16f * H;
                    break;
                case MotionStyle.Skeleton:
                    Set(B.Head, new Vector3(0, S(t * 2.3f) * 8, S(t * 1.7f) * 5));
                    break;
                case MotionStyle.Noble:
                    Sym(B.UpperArmR, B.UpperArmL, new Vector3(18, 0, 4)); Sym(B.LowerArmR, B.LowerArmL, new Vector3(-55, 25, 0));
                    Set(B.Chest, new Vector3(-4, 0, 0)); Set(B.Head, new Vector3(-3, 0, 0));
                    break;
                case MotionStyle.Brute:
                    Set(B.Spine, new Vector3(8, 0, 0)); Sym(B.UpperArmR, B.UpperArmL, new Vector3(-6, 0, 10));
                    hipOffset.y -= 0.02f * H;
                    break;
                case MotionStyle.Ghost:
                    hipOffset.y += 0.15f + S(t * 1.4f + seed) * 0.06f;
                    Sym(B.ThighR, B.ThighL, new Vector3(-8 + S(t) * 4, 0, 2)); Sym(B.ShinR, B.ShinL, new Vector3(18, 0, 0)); Sym(B.FootR, B.FootL, new Vector3(35, 0, 0));
                    Sym(B.UpperArmR, B.UpperArmL, new Vector3(-10 + S(t * 0.9f) * 6, 0, 8));
                    break;
            }

            // ---------------- seated ----------------
            bool sitting = seated && alive && stanceW < 0.01f && speed < 0.2f;
            if (sitting)
            {
                Sym(B.ThighR, B.ThighL, new Vector3(-86, 0, 5));
                Sym(B.ShinR, B.ShinL, new Vector3(84, 0, 0));
                Sym(B.FootR, B.FootL, new Vector3(2, 0, 0));
                Sym(B.UpperArmR, B.UpperArmL, new Vector3(-32, 0, -2));
                Sym(B.LowerArmR, B.LowerArmL, new Vector3(-48, 8, 0));
                Set(B.Spine, new Vector3(4, 0, 0));
                hipOffset.y -= H * 0.505f - seatHeight;
            }

            // ---------------- combat stance ----------------
            float w = Anim.Smooth(0, 1, stanceW);
            if (w > 0.001f && style != MotionStyle.Zombie && style != MotionStyle.Ghost)
            {
                float bounce = S(t * 2.4f + seed);
                switch (grip)
                {
                    case Grip.Bow:
                        Set(B.UpperArmL, new Vector3(-26, 0, -6) * w); Set(B.LowerArmL, new Vector3(-38, -10, 0) * w);
                        Set(B.UpperArmR, new Vector3(-12, 0, 6) * w); Set(B.LowerArmR, new Vector3(-40, 0, 0) * w);
                        Set(B.Chest, 0, 10 * w, 0);
                        break;
                    case Grip.Crossbow:
                        Set(B.UpperArmR, new Vector3(-20, 0, 8) * w); Set(B.LowerArmR, new Vector3(-70, 0, 0) * w);
                        Set(B.UpperArmL, new Vector3(-30, 0, -10) * w); Set(B.LowerArmL, new Vector3(-60, 20, 0) * w);
                        break;
                    case Grip.TwoHand:
                        // blade held up and across, point toward the foe; the off hand is placed by IK
                        Set(B.UpperArmR, new Vector3(-28, -24, 18) * w); Set(B.LowerArmR, new Vector3(-78, 0, 0) * w);
                        Set(B.HandR, new Vector3(-10, 0, 28) * w);
                        Set(B.UpperArmL, new Vector3(-40, 30, 4) * w); Set(B.LowerArmL, new Vector3(-60, 0, 0) * w);
                        Set(B.Chest, new Vector3(0, 20, 0) * w); Set(B.Spine, 0, 6 * w, 0);
                        break;
                    case Grip.Polearm:
                        // rear hand low by the hip, haft slanting forward and up, head leading
                        Set(B.UpperArmR, new Vector3(8, -10, 16) * w); Set(B.LowerArmR, new Vector3(-62, 18, 0) * w);
                        Set(B.HandR, new Vector3(-38, 0, 12) * w);
                        Set(B.UpperArmL, new Vector3(-45, 20, -6) * w); Set(B.LowerArmL, new Vector3(-50, 0, 0) * w);
                        Set(B.Chest, new Vector3(0, 26, 0) * w); Set(B.Spine, 0, 8 * w, 0); Set(B.Head, 0, -22 * w, 0);
                        break;
                    case Grip.Unarmed:
                        Sym(B.UpperArmR, B.UpperArmL, new Vector3(-38, 0, 14) * w); Sym(B.LowerArmR, B.LowerArmL, new Vector3(-105, 18, 0) * w);
                        Set(B.Chest, 0, 12 * w, 0);
                        break;
                    case Grip.Shield:
                        Set(B.UpperArmR, new Vector3(-26, 0, 14) * w); Set(B.LowerArmR, new Vector3(-72, 0, 0) * w); Set(B.HandR, new Vector3(-8, 0, 0) * w);
                        Set(B.UpperArmL, new Vector3(-34, 20, -14) * w); Set(B.LowerArmL, new Vector3(-80, 55, 0) * w);
                        Set(B.Chest, new Vector3(0, 12, 0) * w);
                        break;
                    default:
                        Set(B.UpperArmR, new Vector3(-26, 0, 14) * w); Set(B.LowerArmR, new Vector3(-72, 0, 0) * w); Set(B.HandR, new Vector3(-8, 0, 0) * w);
                        Set(B.UpperArmL, new Vector3(-18, 0, -10) * w); Set(B.LowerArmL, new Vector3(-58, 0, 0) * w);
                        Set(B.Chest, new Vector3(0, 12, 0) * w);
                        break;
                }
                Set(B.Spine, new Vector3(7 + bounce * 1.2f, 0, 0) * w);
                Set(B.Head, new Vector3(-6, 0, 0) * w);
                hipOffset.y -= (0.045f + bounce * 0.006f) * H * w * (1f - moveW * 0.6f);    // knees soft: the leg IK does the bending
                pelvisYaw += 14f * w * (1f - moveW * 0.7f);
            }

            // ---------------- locomotion ----------------
            float targetMove = alive && !sitting && style != MotionStyle.Ghost ? Mathf.Clamp01((speed - 0.08f) / 0.35f) : 0f;
            moveW = Mathf.MoveTowards(moveW, targetMove, dt * 4f);
            float targetRun = Anim.Smooth(1.9f * sizeK, 2.7f * sizeK, speed);
            if (style == MotionStyle.Zombie || style == MotionStyle.Skeleton) targetRun *= 0.3f;
            runW = Mathf.MoveTowards(runW, targetRun, dt * 2.5f);
            float cycle = CycleTime(speed);
            float duty = Mathf.Lerp(0.62f, 0.36f, runW);
            if (moveW > 0.001f)
            {
                gaitPhase = Mathf.Repeat(gaitPhase + dt / cycle, 1f);
                float mw = moveW;
                float pr = gaitPhase;                                  // right leg's phase; the left is half a cycle later
                float mid = duty * 0.5f;
                // pelvis: bob (high over a straight walking leg, low over a compressed running one), sway, twist, hip drop
                float bobWalk = -0.012f * sc - 0.018f * sc * C(TAU * 2f * (pr - mid));
                float bobRun = -0.045f * sc - 0.03f * sc * C(TAU * 2f * (pr - mid));
                hipOffset.y += Mathf.Lerp(bobWalk, bobRun, runW) * mw;
                hipOffset.x += 0.022f * sc * C(TAU * (pr - mid)) * (1 - runW) * mw;
                float twist = Mathf.Lerp(7f, 11f, runW) * mw;
                pelvisYaw += -twist * C(TAU * pr);
                pelvisRoll += Mathf.Lerp(4f, 2f, runW) * C(TAU * (pr - mid)) * mw;
                // trunk: lean into speed and acceleration, shoulders counter the hips, head steady
                float lean = Mathf.Lerp(3f, 11f, runW) * mw + Mathf.Clamp(accel * 1.6f, -8f, 8f) * mw;
                Set(B.Spine, lean * 0.6f, twist * C(TAU * pr) * 0.7f, 0);
                Set(B.Chest, lean * 0.4f + S(TAU * 2f * pr) * 1.5f * runW, twist * C(TAU * pr) * 0.9f, 0);
                Set(B.Neck, -lean * 0.4f, -twist * C(TAU * pr) * 0.4f, 0);
                Set(B.Head, -lean * 0.5f, -twist * C(TAU * pr) * 0.3f, 0);
                // bank into turns
                float bank = Mathf.Clamp(turnRate * speed * 0.012f, -10f, 10f) * mw;
                bodyRoll += -bank;
                // arms swing against the legs; runners pump with bent elbows
                float armFree = 1f - w * 0.75f;
                float swing = Mathf.Lerp(16f, 30f, runW) * mw * armFree;
                float cr = C(TAU * pr);
                // runners carry the arms a little back, hands low by the ribs
                float back = 12f * runW * mw * armFree;
                Set(B.UpperArmR, swing * cr + back, 0, 0); Set(B.UpperArmL, -swing * cr + back, 0, 0);
                float elbowBase = Mathf.Lerp(-8f, -62f, runW) * mw * armFree;
                Set(B.LowerArmR, elbowBase - Mathf.Lerp(14f, 22f, runW) * Mathf.Max(0, -cr) * mw * armFree, 0, 0);
                Set(B.LowerArmL, elbowBase - Mathf.Lerp(14f, 22f, runW) * Mathf.Max(0, cr) * mw * armFree, 0, 0);
                Sym(B.UpperArmR, B.UpperArmL, new Vector3(0, 0, Mathf.Lerp(0, 6f, runW) * mw));
                // shoulders ride the stride
                Set(B.ClavR, 0, 0, 2.5f * cr * mw); Set(B.ClavL, 0, 0, 2.5f * cr * mw);
            }

            // ---------------- talking gestures ----------------
            if (talking && act == AnimAct.None && alive)
            {
                talkT += dt;
                float g1 = Mathf.PerlinNoise(talkT * 0.7f, seed) - 0.4f, g2 = Mathf.PerlinNoise(seed, talkT * 0.6f) - 0.4f;
                Set(B.UpperArmR, new Vector3(-30 * Mathf.Max(0, g1), 0, 5 * g1)); Set(B.LowerArmR, new Vector3(-50 * Mathf.Max(0, g1), 0, 0));
                Set(B.UpperArmL, new Vector3(-20 * Mathf.Max(0, g2), 0, 0)); Set(B.LowerArmL, new Vector3(-40 * Mathf.Max(0, g2), 0, 0));
                Set(B.Head, new Vector3(S(talkT * 2.5f) * 3, S(talkT * 0.8f) * 4, S(talkT * 1.3f) * 2));
            }

            // ---------------- action ----------------
            if (act != AnimAct.None && alive && clip != null)
            {
                clip.Eval(Mathf.Clamp01(actT / actDur), clipPose);
                for (int i = 0; i < N; i++) rot[i] += clipPose.r[i];
                hipOffset += clipPose.hip * sc;
                bodyPitch += clipPose.lean;
            }

            // ---------------- life state ----------------
            if (life != LifeState.Alive || lifeT < 1f)
            {
                float k = life == LifeState.Alive ? 1 - lifeT : lifeT;   // 1 = fully down
                float buckle = Anim.Env(k, 0f, 0.28f, 0.5f, 0.85f);
                float fall = Anim.Smooth(0.2f, 0.78f, k);
                // settle: a small bounce as the body lands
                float bounceK = Mathf.Clamp01((k - 0.78f) / 0.22f);
                float settle = Mathf.Sin(bounceK * Mathf.PI) * (1 - bounceK) * 0.35f;
                Sym(B.ThighR, B.ThighL, new Vector3(-45 * buckle, 0, 0));
                Sym(B.ShinR, B.ShinL, new Vector3(85 * buckle, 0, 0));
                Set(B.Spine, new Vector3(26 * buckle, 12 * buckle * fallSide, 0));
                Set(B.Chest, new Vector3(10 * buckle, 0, 0));
                hipOffset.y -= 0.26f * H * buckle;
                bodyPitch = -84 * fall + settle * 10f;
                bodyRoll += 10f * fall * fallSide;
                modelLift = fall * 0.12f * sc;
                Sym(B.UpperArmR, B.UpperArmL, new Vector3(-20 * fall, 0, 58 * fall));
                Sym(B.LowerArmR, B.LowerArmL, new Vector3(-24 * fall, 0, 0));
                Set(B.UpperArmR, -25 * fall * fallSide, 0, 0);
                Set(B.Head, new Vector3(-12 * fall, 28 * fall * fallSide, 0));
                Sym(B.ThighR, B.ThighL, new Vector3(0, 0, 8 * fall));
                Set(B.ThighL, -18 * fall, 0, 0); Set(B.ShinL, 30 * fall, 0, 0);
                if (life == LifeState.Downed)
                {
                    float wr = S(t * 1.1f + seed);
                    Set(B.UpperArmR, new Vector3(-35 * Mathf.Max(0, wr) * fall, 0, 0));
                    Set(B.Head, new Vector3(-15 * Mathf.Max(0, S(t * 0.6f)) * fall, -20 * fall, 0));
                    Set(B.ThighL, new Vector3(-25 * fall * Mathf.Max(0, S(t * 0.5f)), 0, 0));
                    Set(B.ShinL, new Vector3(40 * fall * Mathf.Max(0, S(t * 0.5f)), 0, 0));
                }
            }

            // ---------------- look at ----------------
            float targetLook = lookAt.HasValue && alive ? 1 : 0;
            lookW = Mathf.MoveTowards(lookW, targetLook, dt * 3);
            if (lookAt.HasValue)
            {
                Vector3 local = transform.InverseTransformPoint(lookAt.Value);
                float lyaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(local.y - rig.height * 0.9f, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
                headYaw = Mathf.Lerp(headYaw, Mathf.Clamp(lyaw, -65, 65), 1 - Mathf.Exp(-dt * 6));
                headPitch = Mathf.Lerp(headPitch, Mathf.Clamp(pitch, -25, 25), 1 - Mathf.Exp(-dt * 6));
            }
            if (lookW > 0.001f)
            {
                Set(B.Head, new Vector3(headPitch * 0.6f, headYaw * 0.6f, 0) * lookW);
                Set(B.Neck, new Vector3(headPitch * 0.4f, headYaw * 0.4f, 0) * lookW);
                Set(B.Chest, new Vector3(0, headYaw * 0.15f, 0) * lookW);
            }

            // ---------------- cape & tail: springs driven by speed and turning ----------------
            float targetSwing = Mathf.Clamp(speed * 9f, 0, 48) + S(t * 1.3f + seed) * 3f + Mathf.Abs(turnRate) * 0.02f;
            float acc = (targetSwing - capeSwing) * 30f - capeVel * 7f;
            capeVel += acc * dt; capeSwing += capeVel * dt;
            float flutter = moveW * runW;
            Set(B.Cape0, new Vector3(capeSwing * 0.35f + 4, 0, 0));
            Set(B.Cape1, new Vector3(capeSwing * 0.35f + S(t * 2.1f + seed) * (2 + 4 * flutter), S(t * 1.7f) * 3 - turnRate * 0.01f, 0));
            Set(B.Cape2, new Vector3(capeSwing * 0.3f + S(t * 2.6f + seed) * (3 + 6 * flutter), S(t * 2.2f) * 4 - turnRate * 0.015f, 0));
            Set(B.Cape3, new Vector3(S(t * 3.1f + seed) * (2 + 5 * flutter), 0, 0));
            for (int i = 0; i < 4; i++)
                Set(B.Tail0 + i, new Vector3(S(t * 1.8f + i * 0.9f) * 6 + (i == 0 ? 10 : 0), S(t * 1.2f + i * 0.7f) * 14 * (i + 1) * 0.5f - turnRate * 0.02f * i, 0));

            // pelvis twist & hip drop live on the hips; the spine and chest turn back so the shoulders face forward
            Set(B.Hips, 0, pelvisYaw, pelvisRoll);
            Set(B.Spine, 0, -pelvisYaw * 0.35f, -pelvisRoll * 0.5f);
            Set(B.Chest, 0, -pelvisYaw * 0.45f, 0);

            // how much the legs follow the ground: none while seated, floating or lying down
            float lifeUp = life == LifeState.Alive ? Mathf.Clamp01(lifeT * 1.4f - 0.2f) : 1f - Anim.Smooth(0f, 0.25f, lifeT);
            float targetIk = IkAllowed && !sitting ? lifeUp : 0f;
            ikW = Mathf.MoveTowards(ikW, targetIk, dt * 5f);
        }

        float CycleTime(float v)
        {
            // human cadence: a relaxed walk takes ~1.1 s per stride, a run ~0.72 s; small folk take quicker steps
            float walkT = Mathf.Lerp(1.3f, 1.02f, Mathf.Clamp01((v - 0.8f) / 1.2f));
            float runT = Mathf.Lerp(0.8f, 0.68f, Mathf.Clamp01((v - 2.5f) / 2.5f));
            float k = style == MotionStyle.Zombie ? 1.5f : style == MotionStyle.Ghoul ? 0.85f : 1f;
            return Mathf.Lerp(walkT, runT, runW) * sizeK * k;
        }

        void Apply()
        {
            for (int i = 1; i < N; i++)
            {
                var b = rig.bones[i];
                if (!b) continue;
                b.localRotation = Quaternion.Euler(rot[i]);
            }
            rig.bones[(int)B.Hips].localPosition = rig.bindPos[(int)B.Hips] + hipOffset;
            model.localRotation = Quaternion.Euler(bodyPitch, 0, bodyRoll);
            model.localPosition = new Vector3(0, modelLift, 0);
        }

        // =============================================================================================
        //  Legs: plan where each foot goes (planted, or stepping), then bend the leg to put it there.
        // =============================================================================================

        Vector3 RestLocal(Foot f)
        {
            float w = Anim.Smooth(0, 1, stanceW) * (style == MotionStyle.Zombie || style == MotionStyle.Ghost ? 0 : 1);
            float x = f.side * hipX * Mathf.Lerp(0.95f, 1.45f, w);
            float z = f.side * Mathf.Lerp(0.015f, -0.14f, w) * rig.scale;     // combat: left foot leads, right foot back
            if (grip == Grip.Bow) z = -z;
            return new Vector3(x, 0, z);
        }

        Vector3 Ground(Vector3 p)
        {
            float top = transform.position.y + legLen * 1.2f;
            if (Physics.Raycast(new Vector3(p.x, top, p.z), Vector3.down, out var hit, legLen * 2.6f, Layers.GroundMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return new Vector3(p.x, transform.position.y, p.z);
        }

        void SolveLegs(float dt)
        {
            if (ikW <= 0.001f) { foreach (var f in feet) { f.stepping = false; f.stanceS = -1; f.placed = false; } return; }
            var root = transform;
            Quaternion yawRot = Quaternion.Euler(0, root.eulerAngles.y, 0);
            float cycle = CycleTime(speed);
            float duty = Mathf.Lerp(0.62f, 0.36f, runW);
            float stride = speed * cycle;
            float lift = Mathf.Lerp(0.075f, 0.2f, runW) * rig.scale * Mathf.Max(0.3f, moveW);
            bool gait = moveW > 0.05f;
            idleStepCool -= dt;

            for (int i = 0; i < 2; i++)
            {
                var f = feet[i];
                Vector3 restW = root.position + yawRot * (RestLocal(f) + (f.side > 0 ? clipPose.stepR : clipPose.stepL) * rig.scale);
                if (!f.placed) { f.plant = Ground(restW); f.ground = f.plant; f.placed = true; f.stepping = false; }

                if (gait)
                {
                    float ph = Mathf.Repeat(gaitPhase + (f.side > 0 ? 0f : 0.5f), 1f);
                    if (ph < duty)
                    {
                        // stance: the foot stays where it landed while the body passes over it
                        if (f.stepping) { f.stepping = false; f.plant = f.to; }
                        f.stanceS = ph / duty;
                        f.ground = f.plant;
                        f.roll = StanceRoll(f.stanceS);
                    }
                    else
                    {
                        float s = (ph - duty) / (1f - duty);
                        if (!f.stepping) { f.stepping = true; f.gaitStep = true; f.from = f.plant; }
                        // aim for where the body will be when this foot comes down, half a stance ahead of the hip
                        float timeLeft = (1f - s) * (1f - duty) * cycle;
                        Vector3 predRoot = root.position + vel * timeLeft;
                        Vector3 land = predRoot + yawRot * (RestLocal(f) + Vector3.forward * stride * duty * 0.5f);
                        f.to = Ground(land);
                        float e = s * s * (3f - 2f * s);
                        float arc = Mathf.Sin(Mathf.PI * Mathf.Pow(s, Mathf.Lerp(1f, 0.72f, runW)));
                        f.ground = Vector3.Lerp(f.from, f.to, e) + Vector3.up * lift * arc;
                        f.roll = SwingRoll(s);
                        f.stanceS = -1;
                        f.stepT = s; f.stepDur = (1f - duty) * cycle;
                    }
                }
                else
                {
                    // standing: finish any step in flight, then shuffle a foot back under the body when it drifts
                    if (f.stepping)
                    {
                        f.stepT += dt / Mathf.Max(0.12f, f.stepDur);
                        if (f.gaitStep) f.to = Ground(restW);
                        float s = Mathf.Clamp01(f.stepT);
                        float e = s * s * (3f - 2f * s);
                        f.ground = Vector3.Lerp(f.from, f.to, e) + Vector3.up * Mathf.Max(0.035f * rig.scale, lift) * Mathf.Sin(Mathf.PI * s);
                        f.roll = Mathf.Lerp(15f, -6f, s) * Mathf.Sin(Mathf.PI * s);
                        if (s >= 1f) { f.stepping = false; f.gaitStep = false; f.plant = f.to; idleStepCool = 0.1f; }
                    }
                    else
                    {
                        Vector3 d = restW - f.plant; d.y = 0;
                        var other = feet[1 - i];
                        bool clipStep = (f.side > 0 ? clipPose.stepR : clipPose.stepL).sqrMagnitude > 0.0001f || act != AnimAct.None;
                        float thresh = (clipStep ? 0.05f : 0.14f) * rig.scale;
                        if (d.magnitude > thresh && !other.stepping && idleStepCool <= 0)
                        {
                            f.stepping = true; f.gaitStep = false; f.from = f.plant; f.to = Ground(restW);
                            f.stepT = 0; f.stepDur = clipStep ? 0.13f : 0.26f;
                        }
                        f.ground = f.plant;
                        f.roll = Mathf.MoveTowards(f.roll, 0, dt * 120f);
                        f.stanceS = -1;
                    }
                }
            }

            // lower the pelvis when a foot can't be reached (long strides, stepping down)
            float need = 0;
            for (int i = 0; i < 2; i++)
            {
                var f = feet[i];
                float reach = Vector3.Distance(rig.bones[(int)f.thigh].position, AnkleFor(f, yawRot));
                need = Mathf.Max(need, reach - legLen * 0.985f);
            }
            hipDrop = Mathf.Lerp(hipDrop, Mathf.Clamp(need, 0, legLen * 0.35f), 1 - Mathf.Exp(-dt * 18f));
            if (hipDrop > 0.0005f)
                rig.bones[(int)B.Hips].position += Vector3.down * hipDrop * ikW;

            for (int i = 0; i < 2; i++)
            {
                var f = feet[i];
                Vector3 ankle = AnkleFor(f, yawRot);
                Vector3 pole = yawRot * new Vector3(f.side * 0.12f, 0, 1f);
                SolveTwoBone(rig.bones[(int)f.thigh], rig.bones[(int)f.shin], rig.bones[(int)f.foot], ankle, pole, ikW);
                // foot and toe: roll about the ankle's side axis; toes stay flat on the ground while the heel is up
                var footT = rig.bones[(int)f.foot];
                Quaternion footRot = yawRot * Quaternion.Euler(f.roll, f.yawOff * 0.5f, 0);
                footT.rotation = Quaternion.Slerp(footT.rotation, footRot, ikW);
                float toeBend = f.stanceS >= 0 || !f.stepping ? -Mathf.Max(0, f.roll) : -Mathf.Max(0, f.roll) * 0.4f;
                rig.bones[(int)f.toe].localRotation = Quaternion.Euler(toeBend * ikW, 0, 0);
            }
        }

        /// <summary>Where the ankle must be for the foot to touch ground point f.ground at roll f.roll (pivoting on ball or heel).</summary>
        Vector3 AnkleFor(Foot f, Quaternion yawRot)
        {
            Quaternion q = yawRot * Quaternion.Euler(f.roll, 0, 0);
            Vector3 fwd = yawRot * Vector3.forward;
            if (f.roll >= 0)
            {
                Vector3 ball = f.ground + fwd * ballZ;
                return ball + q * new Vector3(0, ankleH, -ballZ);
            }
            Vector3 heel = f.ground - fwd * heelZ;
            return heel + q * new Vector3(0, ankleH, heelZ);
        }

        float StanceRoll(float s)
        {
            // walk: heel strike (toes up) -> foot flat -> heel rises -> push off the toes. Runners land flatter.
            float strike = Mathf.Lerp(-11f, 2f, runW) * (1 - Anim.Smooth(0f, 0.16f, s));
            float push = Mathf.Lerp(34f, 28f, runW) * Anim.Smooth(Mathf.Lerp(0.55f, 0.35f, runW), 1f, s);
            return (strike + push) * moveW;
        }

        float SwingRoll(float s)
        {
            float a = Mathf.Lerp(34f, 30f, runW), b = Mathf.Lerp(-10f, 4f, runW);
            float mid = Mathf.Lerp(8f, 22f, runW);
            float r = s < 0.4f ? Mathf.Lerp(a, mid, s / 0.4f) : Mathf.Lerp(mid, b, Anim.Smooth(0.4f, 1f, s));
            return r * moveW;
        }

        /// <summary>Two-bone IK in world space: bends a-b-c so c lands on target, the middle joint toward pole.</summary>
        public static void SolveTwoBone(Transform a, Transform b, Transform c, Vector3 target, Vector3 pole, float weight)
        {
            if (weight <= 0.001f) return;
            Vector3 pa = a.position, pb = b.position, pc = c.position;
            float la = Vector3.Distance(pa, pb), lb = Vector3.Distance(pb, pc);
            Vector3 at = target - pa;
            float d = Mathf.Clamp(at.magnitude, Mathf.Abs(la - lb) + 1e-3f, la + lb - 1e-4f);
            Vector3 dir = at.sqrMagnitude > 1e-8f ? at.normalized : (pc - pa).normalized;
            float cosA = Mathf.Clamp((la * la + d * d - lb * lb) / (2f * la * d), -1f, 1f);
            float sinA = Mathf.Sqrt(1f - cosA * cosA);
            Vector3 perp = pole - dir * Vector3.Dot(pole, dir);
            if (perp.sqrMagnitude < 1e-6f) perp = Vector3.Cross(dir, Vector3.right);
            perp.Normalize();
            Vector3 knee = pa + (dir * cosA + perp * sinA) * la;
            Quaternion ra = Quaternion.FromToRotation(pb - pa, knee - pa) * a.rotation;
            a.rotation = Quaternion.Slerp(a.rotation, ra, weight);
            pb = b.position; pc = c.position;
            Vector3 endT = pa + dir * d;
            Quaternion rb = Quaternion.FromToRotation(pc - pb, endT - pb) * b.rotation;
            b.rotation = Quaternion.Slerp(b.rotation, rb, weight);
        }

        // =============================================================================================
        //  Two-handed weapons are animated from the weapon: the stance and each clip say where the main-hand grip
        //  is (relative to the chest, so twisting the body swings the weapon) and which way the haft points; both
        //  arms then reach for their grips by IK. Keeps a glaive or greatsword steady in thrusts and chops.
        // =============================================================================================
        float restChestYaw;
        void DriveTwoHanded()
        {
            if (life != LifeState.Alive || rig.mainWeapon == null || rig.gear == null || !rig.gear.drawn) return;
            if (grip != Grip.TwoHand && grip != Grip.Polearm) return;
            if (rig.mainWeapon.transform.parent != rig.socketHandR) return;
            float w = Anim.Smooth(0.25f, 1f, stanceW);
            if (w <= 0.001f) return;
            bool pole = grip == Grip.Polearm;
            float sc = rig.scale;
            var chest = rig[B.Chest];
            // stance: a polearm low at the right hip with the head leading forward and up; a greatsword raised before the chest
            Vector3 g0 = pole ? new Vector3(0.16f, -0.4f, 0.1f) : new Vector3(0.05f, -0.3f, 0.3f);
            Vector3 a0 = pole ? new Vector3(-22f, -4f, 0f) : new Vector3(-58f, -6f, 0f);
            Vector3 gl = (g0 + clipPose.wgrip) * sc;
            Vector3 al = a0 + clipPose.waim;
            // aim in the body's heading; only the twist a clip adds on top of the stance swings the weapon
            float chestYaw = Mathf.DeltaAngle(transform.eulerAngles.y, chest.eulerAngles.y);
            if (act == AnimAct.None) restChestYaw = Mathf.LerpAngle(restChestYaw, chestYaw, 0.2f);
            Quaternion frame = Quaternion.Euler(0, transform.eulerAngles.y + (chestYaw - restChestYaw), 0);
            Vector3 G = chest.position + frame * gl;
            Quaternion aim = frame * Quaternion.Euler(al.x, al.y, al.z);
            Vector3 D = aim * Vector3.forward;              // along the haft, toward the head
            Vector3 side = aim * Vector3.right;
            // the weapon's +Y runs along the haft; its +Z faces the blade's flat side
            Quaternion weaponRot = Quaternion.LookRotation(Vector3.Cross(side, D), D);

            PlaceHand(rig[B.UpperArmR], rig[B.LowerArmR], rig[B.HandR], rig.socketHandR, G, weaponRot, transform.rotation * new Vector3(0.7f, -0.7f, -0.3f), w);
            float along = pole ? 0.52f : -0.13f;
            if (rig.gear.main == WeaponVisual.Greataxe || rig.gear.main == WeaponVisual.Maul) along = -0.26f;
            Vector3 G2 = G + D * along * rig.mainWeapon.transform.lossyScale.y;
            PlaceHand(rig[B.UpperArmL], rig[B.LowerArmL], rig[B.HandL], rig.socketHandL, G2, weaponRot, transform.rotation * new Vector3(-0.7f, -0.7f, -0.3f), w);
        }

        /// <summary>Reach an arm so its hand socket sits at pos with the socket (and what it holds) turned to rot.</summary>
        static void PlaceHand(Transform upper, Transform lower, Transform hand, Transform socket, Vector3 pos, Quaternion rot, Vector3 pole, float w)
        {
            Quaternion handRot = rot * Quaternion.Inverse(socket.localRotation);
            Vector3 wrist = pos - handRot * socket.localPosition;
            SolveTwoBone(upper, lower, hand, wrist, pole, w);
            hand.rotation = Quaternion.Slerp(hand.rotation, handRot, w);
        }
    }
}
