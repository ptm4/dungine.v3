using System;
using System.Collections.Generic;
using UnityEngine;
using Q = Dungine.Visual.BeastRig.Q;

namespace Dungine.Visual
{
    /// <summary>A beast pose as offsets from standing: bone rotations, a body offset, the front paws' reach and the jaw.</summary>
    public class BeastPose
    {
        public const int N = (int)Q.COUNT;
        public readonly Vector3[] r = new Vector3[N];
        public Vector3 body;          // pelvis offset (metres at wolf scale)
        public float frontStep;       // how far the front paws reach forward (metres at wolf scale)
        public float frontLift;       // 0..1: front paws leave the ground (rearing, swiping)
        public BeastPose R(Q q, float x, float y = 0, float z = 0) { r[(int)q] += new Vector3(x, y, z); return this; }
        public BeastPose Body(float x, float y, float z) { body = new Vector3(x, y, z); return this; }
        public BeastPose Step(float z) { frontStep = z; return this; }
        public BeastPose Lift(float k) { frontLift = k; return this; }
        public BeastPose Scaled(float k)
        {
            var p = new BeastPose();
            for (int i = 0; i < N; i++) p.r[i] = r[i] * k;
            p.body = body * k; p.frontStep = frontStep * k; p.frontLift = frontLift * k;
            return p;
        }
    }

    public class BeastClip
    {
        readonly List<float> t = new List<float>();
        readonly List<BeastPose> p = new List<BeastPose>();
        public BeastClip K(float u, BeastPose pose) { t.Add(u); p.Add(pose); return this; }

        public void Eval(float u, BeastPose o)
        {
            int n = t.Count + 2;
            float T(int i) => i == 0 ? 0f : i == n - 1 ? 1f : t[i - 1];
            BeastPose P(int i) => i == 0 || i == n - 1 ? null : p[i - 1];
            int k = 0;
            while (k < n - 2 && u > T(k + 1)) k++;
            float t0 = T(k), t1 = T(k + 1);
            float s = Mathf.Clamp01((u - t0) / Mathf.Max(1e-4f, t1 - t0));
            float h00 = 2 * s * s * s - 3 * s * s + 1, h10 = s * s * s - 2 * s * s + s, h01 = -2 * s * s * s + 3 * s * s, h11 = s * s * s - s * s;
            float dt = t1 - t0;
            BeastPose a = P(k), b = P(k + 1), pa = k > 0 ? P(k - 1) : null, nb = k + 2 < n ? P(k + 2) : null;
            float ta0 = k > 0 ? T(k - 1) : t0, tb1 = k + 2 < n ? T(k + 2) : t1;
            bool endA = k == 0, endB = k + 1 == n - 1;
            Vector3 Herm(Func<BeastPose, Vector3> f)
            {
                Vector3 va = a != null ? f(a) : Vector3.zero, vb = b != null ? f(b) : Vector3.zero;
                Vector3 vpa = pa != null ? f(pa) : Vector3.zero, vnb = nb != null ? f(nb) : Vector3.zero;
                Vector3 ma = endA ? Vector3.zero : (vb - vpa) / Mathf.Max(1e-4f, t1 - ta0) * dt;
                Vector3 mb = endB ? Vector3.zero : (vnb - va) / Mathf.Max(1e-4f, tb1 - t0) * dt;
                return h00 * va + h10 * ma + h01 * vb + h11 * mb;
            }
            for (int i = 0; i < BeastPose.N; i++) { int ii = i; o.r[i] += Herm(q => q.r[ii]); }
            o.body += Herm(q => q.body);
            var st = Herm(q => new Vector3(q.frontStep, q.frontLift, 0));
            o.frontStep += st.x; o.frontLift += st.y;
        }
    }

    /// <summary>
    /// Animates a BeastRig: real four-legged gaits (a four-beat walk, the diagonal trot, a rotary gallop) with every
    /// paw planted by IK, pasterns and hocks folding in the swing, shoulder blades sliding with the stride, a spine
    /// that flexes in the gallop, a steady head, a tail and ears on springs, keyed bites, hits and howls, a crouched
    /// snarling combat stance and a proper fall onto the side.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class BeastAnimator : MonoBehaviour, IAnimDriver
    {
        BeastRig rig;
        public bool manual;
        readonly Vector3[] rot = new Vector3[BeastPose.N];
        readonly BeastPose clipPose = new BeastPose();
        Vector3 lastPos; float lastYaw; bool first = true;
        Vector3 vel; float speed, turnRate, t, seed, sizeK;
        AnimAct act; BeastClip clip; float actT, actDur, impactAt; Action onImpact; bool fired;
        LifeState life = LifeState.Alive; float lifeT = 1, fallSide = 1;
        float combatW, targetCombat, moveW, ikW, bodyDrop;
        float gaitPhase, tailSwing, tailVel, tailLift, tailLiftVel;
        Vector3 bodyOff; float bodyRoll, bodyPitch, modelLift;
        Vector3? lookAt;

        public bool Busy => act != AnimAct.None;
        public LifeState Life => life;
        public float Height => rig ? rig.height : 0.8f;

        class Leg
        {
            public bool front; public int side;
            public Q scap, a, b, c, paw;
            public float offWalk, offTrot, offGallop;
            public Vector3 restLocal;   // paw rest position (character space, metres)
            public Vector3 cToPaw;      // pastern/metatarsus vector (character space), from the joint above the paw to the paw
            public bool stepping, placed;
            public Vector3 plant, from, to, ground;
            public float swingS = -1, stepT, stepDur;
        }
        readonly Leg[] legs = new Leg[4];

        public void Init(BeastRig r)
        {
            rig = r; seed = UnityEngine.Random.value * 10;
            lastPos = transform.position; lastYaw = transform.eulerAngles.y;
            sizeK = Mathf.Sqrt(Mathf.Max(0.2f, r.height) / 0.8f);
            float s = r.scale;
            Leg Make(bool front, int side, Q scap, Q a, Q b, Q c, Q paw, float ow, float ot, float og)
            {
                return new Leg
                {
                    front = front, side = side, scap = scap, a = a, b = b, c = c, paw = paw, offWalk = ow, offTrot = ot, offGallop = og,
                    restLocal = new Vector3(r.bindModel[(int)paw].x, 0, r.bindModel[(int)paw].z) * s,
                    cToPaw = (r.bindModel[(int)paw] - r.bindModel[(int)c]) * s,
                };
            }
            // phase offsets: walk = lateral sequence (LH, LF, RH, RF); trot = diagonal pairs; gallop = rotary (LH, RH, RF, LF)
            legs[0] = Make(true, -1, Q.ScapL, Q.HumL, Q.ForeL, Q.CarpL, Q.PawFL, 0.25f, 0.0f, 0.62f);
            legs[1] = Make(true, 1, Q.ScapR, Q.HumR, Q.ForeR, Q.CarpR, Q.PawFR, 0.75f, 0.5f, 0.52f);
            legs[2] = Make(false, -1, Q.FemL, Q.FemL, Q.TibL, Q.MetL, Q.PawHL, 0.0f, 0.5f, 0.0f);
            legs[3] = Make(false, 1, Q.FemR, Q.FemR, Q.TibR, Q.MetR, Q.PawHR, 0.5f, 0.0f, 0.1f);
        }

        public void Play(AnimAct a, Action impact = null, float sp = 1)
        {
            if (life != LifeState.Alive) { impact?.Invoke(); return; }
            if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); }
            act = a; actT = 0; actDur = Duration(a); impactAt = actDur * (a == AnimAct.Hit ? 0.1f : a == AnimAct.Roar ? 0.4f : 0.45f);
            clip = ClipFor(a); onImpact = impact; fired = false;
        }
        public void SetCombat(bool on) => targetCombat = on ? 1 : 0;
        public void SetLife(LifeState s, bool instant = false)
        {
            if (s == life) return;
            life = s; lifeT = instant ? 1 : 0; fallSide = UnityEngine.Random.value < 0.5f ? -1 : 1;
            act = AnimAct.None; clip = null;
            if (onImpact != null && !fired) { var o = onImpact; onImpact = null; o(); }
        }
        public void LookAt(Vector3? p) => lookAt = p;
        public void SetTalking(bool on) { }

        static float Duration(AnimAct a) => a == AnimAct.Hit ? 0.5f : a == AnimAct.Roar ? 1.8f : a == AnimAct.Claw ? 1.0f : 0.8f;

        BeastClip ClipFor(AnimAct a)
        {
            switch (a)
            {
                case AnimAct.Hit:
                    {
                        var j = new BeastPose().Body(0, -0.01f, -0.06f).R(Q.Chest, -4, 10 * fallSide, 0).R(Q.Neck1, -18, -8 * fallSide).R(Q.Head, -8).R(Q.Jaw, 16).R(Q.EarL, -25).R(Q.EarR, -25);
                        return new BeastClip().K(0.15f, j).K(0.35f, j.Scaled(0.65f)).K(0.7f, j.Scaled(0.15f));
                    }
                case AnimAct.Roar:
                    {
                        // throw the head back and howl
                        var up = new BeastPose().R(Q.Neck1, -42).R(Q.Neck2, -18).R(Q.Head, -20).R(Q.Jaw, 26).R(Q.Chest, -4).Body(0, -0.02f, -0.02f).R(Q.EarL, -15).R(Q.EarR, -15);
                        return new BeastClip().K(0.3f, up).K(0.78f, up.Scaled(1.05f)).K(0.92f, up.Scaled(0.3f));
                    }
                case AnimAct.Claw:
                    {
                        // rear up and rake with a forepaw
                        var rear = new BeastPose().Body(0, 0.18f, -0.12f).R(Q.Pelvis, -28).R(Q.Chest, -18).R(Q.Neck1, 20).R(Q.Jaw, 20).Lift(1f).R(Q.HumR, -60).R(Q.HumL, -40);
                        var rake = new BeastPose().Body(0, 0.02f, 0.2f).R(Q.Pelvis, -6).R(Q.Chest, 8).R(Q.Neck1, 12).R(Q.Jaw, 10).Lift(0.6f).R(Q.HumR, 40).R(Q.ForeR, -30).Step(0.2f);
                        return new BeastClip().K(0.3f, rear).K(0.42f, rear).K(0.52f, rake).K(0.7f, rake.Scaled(0.6f));
                    }
                default:
                    {
                        // a bite: gather back on the haunches, launch, snap the jaws shut on the lunge, recover
                        var gather = new BeastPose().Body(0, -0.05f, -0.07f).R(Q.Pelvis, 4).R(Q.Neck1, -10).R(Q.Head, -6).R(Q.Jaw, 12).R(Q.EarL, -20).R(Q.EarR, -20);
                        var open = new BeastPose().Body(0, -0.02f, 0.16f).R(Q.Neck1, 16).R(Q.Neck2, 8).R(Q.Head, -10).R(Q.Jaw, 40).Step(0.16f);
                        var snap = new BeastPose().Body(0, -0.03f, 0.24f).R(Q.Neck1, 22).R(Q.Neck2, 10).R(Q.Head, 4, 6).R(Q.Jaw, 2).Step(0.22f);
                        return new BeastClip().K(0.28f, gather).K(0.4f, open).K(0.47f, snap).K(0.6f, snap.Scaled(0.95f)).K(0.82f, snap.Scaled(0.3f));
                    }
            }
        }

        void LateUpdate() { if (!manual) Tick(Time.deltaTime); }

        static float S(float x) => Mathf.Sin(x);
        const float TAU = Mathf.PI * 2f;
        void Set(Q q, float x, float y = 0, float z = 0) => rot[(int)q] += new Vector3(x, y, z);

        public void Tick(float dt)
        {
            if (!rig || dt <= 0) return;
            dt = Mathf.Min(dt, 0.1f);
            t += dt;
            Vector3 p = transform.position; float yaw = transform.eulerAngles.y;
            if (first) { lastPos = p; lastYaw = yaw; first = false; }
            Vector3 v = (p - lastPos) / dt; v.y = 0; if (v.magnitude > 30f) v = Vector3.zero;
            vel = Vector3.Lerp(vel, v, 1 - Mathf.Exp(-dt * 10f)); speed = vel.magnitude;
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(lastYaw, yaw) / dt, 1 - Mathf.Exp(-dt * 8f));
            lastPos = p; lastYaw = yaw;
            lifeT = Mathf.Min(1, lifeT + dt / 1.1f);
            combatW = Mathf.MoveTowards(combatW, targetCombat, dt * 2.5f);
            if (act != AnimAct.None)
            {
                actT += dt;
                if (!fired && actT >= impactAt) { fired = true; var c = onImpact; onImpact = null; c?.Invoke(); }
                if (actT >= actDur) { act = AnimAct.None; clip = null; }
            }
            Evaluate(dt);
            Apply();
            SolveLegs(dt);
        }

        // ------------------------------------------------------------------ gait parameters, blended by speed
        float WalkToTrot => Anim.Smooth(1.5f * sizeK, 2.2f * sizeK, speed);
        float TrotToGallop => Anim.Smooth(5.0f * sizeK, 6.2f * sizeK, speed);
        float CycleTime()
        {
            float walk = Mathf.Lerp(1.05f, 0.85f, Mathf.Clamp01(speed / 1.5f)), trot = Mathf.Lerp(0.6f, 0.48f, Mathf.Clamp01((speed - 2f) / 3f)), gallop = 0.42f;
            return Mathf.Lerp(Mathf.Lerp(walk, trot, WalkToTrot), gallop, TrotToGallop) * sizeK;
        }
        float Duty() => Mathf.Lerp(Mathf.Lerp(0.66f, 0.46f, WalkToTrot), 0.3f, TrotToGallop);
        float Offset(Leg l)
        {
            float a = Mathf.LerpAngle(l.offWalk * 360f, l.offTrot * 360f, WalkToTrot);
            a = Mathf.LerpAngle(a, l.offGallop * 360f, TrotToGallop);
            return Mathf.Repeat(a / 360f, 1f);
        }

        void Evaluate(float dt)
        {
            for (int i = 0; i < rot.Length; i++) rot[i] = Vector3.zero;
            for (int i = 0; i < BeastPose.N; i++) clipPose.r[i] = Vector3.zero;
            clipPose.body = Vector3.zero; clipPose.frontStep = 0; clipPose.frontLift = 0;
            bodyOff = Vector3.zero; bodyRoll = 0; bodyPitch = 0; modelLift = 0;
            float sc = rig.scale;
            bool alive = life == LifeState.Alive;
            float calm = 1f - Mathf.Max(moveW, combatW * 0.6f);

            // idle: breathing, the head scanning and sniffing, ears flicking, a lazy tail
            float br = S(t * 1.6f + seed);
            Set(Q.Chest, br * 1.2f); Set(Q.Loin, -br * 0.5f);
            float look = (Mathf.PerlinNoise(t * 0.2f, seed) - 0.5f) * 50f * calm;
            float sniff = Mathf.Max(0, Mathf.PerlinNoise(seed, t * 0.12f) - 0.62f) * 60f * calm;
            Set(Q.Neck1, sniff * 0.6f, look * 0.35f); Set(Q.Neck2, sniff * 0.3f, look * 0.35f); Set(Q.Head, -sniff * 0.2f, look * 0.3f);
            float twL = Mathf.Max(0, Mathf.PerlinNoise(t * 1.3f, seed + 3) - 0.7f) * 90f, twR = Mathf.Max(0, Mathf.PerlinNoise(seed + 5, t * 1.2f) - 0.7f) * 90f;
            Set(Q.EarL, -twL * 0.4f, twL); Set(Q.EarR, -twR * 0.4f, -twR);

            // combat: crouched, head low and forward, lips back in a snarl, ears pricked
            float cw = Anim.Smooth(0, 1, combatW) * (alive ? 1 : 0);
            if (cw > 0.001f)
            {
                bodyOff.y -= 0.05f * sc * cw;
                Set(Q.Pelvis, -3 * cw); Set(Q.Chest, 6 * cw);
                Set(Q.Neck1, 14 * cw); Set(Q.Head, -16 * cw);
                Set(Q.Jaw, (7 + Mathf.Max(0, S(t * 2.1f + seed)) * 6) * cw);
                Set(Q.EarL, 10 * cw); Set(Q.EarR, 10 * cw);
                bodyOff.x += S(t * 1.1f + seed) * 0.015f * sc * cw * (1 - moveW);
            }

            // locomotion: whole-body rhythm of the current gait
            float targetMove = alive ? Mathf.Clamp01((speed - 0.08f) / 0.3f) : 0f;
            moveW = Mathf.MoveTowards(moveW, targetMove, dt * 4f);
            if (moveW > 0.001f)
            {
                float T = CycleTime();
                gaitPhase = Mathf.Repeat(gaitPhase + dt / T, 1f);
                float ph = gaitPhase * TAU;
                float trot = WalkToTrot * (1 - TrotToGallop), gallop = TrotToGallop, walk = 1 - WalkToTrot;
                float mw = moveW;
                // bob: twice per cycle at walk and trot; the gallop pitches the body fore and aft instead
                bodyOff.y += (-0.006f * walk - 0.012f * trot) * sc * Mathf.Cos(2 * ph) * mw;
                bodyOff.y -= 0.02f * sc * gallop * mw;
                float pitch = 7f * gallop * Mathf.Sin(ph + 0.6f) * mw;
                Set(Q.Pelvis, pitch); Set(Q.Loin, -pitch * 0.8f); Set(Q.Chest, -pitch * 0.9f);
                // spine flex: gathered as the hind legs reach under, stretched as they drive
                Set(Q.Loin, 8f * gallop * Mathf.Sin(ph) * mw);
                // a little roll and sway at the walk and trot
                bodyRoll += 2.5f * Mathf.Sin(ph) * (walk + trot) * mw;
                Set(Q.Chest, 0, 3f * Mathf.Sin(ph) * walk * mw);
                // head carried lower and forward at speed, held steady against the bob
                Set(Q.Neck1, (10f * trot + 16f * gallop) * mw); Set(Q.Head, (-6f * trot - 10f * gallop) * mw + pitch * 0.9f);
                Set(Q.Neck2, 2f * Mathf.Cos(2 * ph) * (walk + trot) * mw);
                // lean the body into turns, the head leading
                float bend = Mathf.Clamp(turnRate * 0.08f, -18f, 18f) * mw;
                Set(Q.Chest, 0, bend * 0.5f); Set(Q.Neck1, 0, bend * 0.6f); Set(Q.Loin, 0, bend * 0.3f);
                bodyRoll += Mathf.Clamp(-turnRate * speed * 0.01f, -12f, 12f) * mw;
                // shoulder blades glide with the forelegs
                foreach (var l in legs)
                    if (l.front)
                    {
                        float lp = Mathf.Repeat(gaitPhase + Offset(l), 1f);
                        Set(l.scap, -12f * Mathf.Cos(lp * TAU) * mw * (1 + gallop * 0.5f));
                    }
            }

            // action
            if (act != AnimAct.None && alive && clip != null)
            {
                clip.Eval(Mathf.Clamp01(actT / actDur), clipPose);
                for (int i = 0; i < rot.Length; i++) rot[i] += clipPose.r[i];
                bodyOff += clipPose.body * sc;
            }

            // tail: a spring that swings with turns and lifts with speed and excitement
            float swingTarget = S(t * 1.2f + seed) * 10f * calm - turnRate * 0.06f + S(gaitPhase * TAU) * 6f * moveW;
            float acc = (swingTarget - tailSwing) * 40f - tailVel * 8f; tailVel += acc * dt; tailSwing += tailVel * dt;
            float liftTarget = 14f * moveW * (1 + TrotToGallop) + 10f * cw - (life != LifeState.Alive ? 20f : 0f);
            float acc2 = (liftTarget - tailLift) * 30f - tailLiftVel * 7f; tailLiftVel += acc2 * dt; tailLift += tailLiftVel * dt;
            Q[] tq = { Q.Tail0, Q.Tail1, Q.Tail2, Q.Tail3, Q.Tail4 };
            for (int i = 0; i < tq.Length; i++)
                Set(tq[i], -tailLift * (i == 0 ? 0.8f : 0.25f), tailSwing * (0.3f + i * 0.25f) + S(t * 2.4f - i * 0.8f) * 3f * moveW);

            // death: legs fold, the body drops and rolls onto its side
            if (life != LifeState.Alive || lifeT < 1)
            {
                float k = life == LifeState.Alive ? 1 - lifeT : lifeT;
                float fold = Anim.Smooth(0f, 0.4f, k), fall = Anim.Smooth(0.25f, 0.85f, k);
                bodyOff.y -= 0.18f * sc * fold * (1 - fall);
                bodyRoll += 88f * fall * fallSide;
                modelLift = fall * rig.height * 0.18f;
                foreach (var l in legs)
                {
                    Set(l.a, (l.front ? -20 : 25) * fall); Set(l.b, (l.front ? 30 : -30) * fall);
                    if (l.front) Set(l.c, -20 * fall);
                }
                Set(Q.Neck1, 20 * fall, 0, 0); Set(Q.Head, 10 * fall); Set(Q.Jaw, 14 * fall);
                // a last few kicks
                if (life == LifeState.Dead && k > 0.85f && k < 1f) foreach (var l in legs) Set(l.b, S(t * 18f) * 10f * (1 - k) * 6f);
            }

            // how much the legs follow the ground
            float targetIk = alive ? 1f : 1f - Anim.Smooth(0f, 0.3f, lifeT);
            ikW = Mathf.MoveTowards(ikW, targetIk, dt * 5f);
        }

        void Apply()
        {
            for (int i = 1; i < rot.Length; i++) rig.bones[i].localRotation = Quaternion.Euler(rot[i]);
            rig.bones[(int)Q.Pelvis].localPosition = rig.bindPos[(int)Q.Pelvis] + bodyOff / Mathf.Max(0.01f, rig.scale);
            rig.model.localRotation = Quaternion.Euler(bodyPitch, 0, bodyRoll);
            rig.model.localPosition = new Vector3(0, modelLift, 0);
        }

        Vector3 Ground(Vector3 p)
        {
            float top = transform.position.y + rig.height * 1.5f;
            if (Physics.Raycast(new Vector3(p.x, top, p.z), Vector3.down, out var hit, rig.height * 3f, Layers.GroundMask, QueryTriggerInteraction.Ignore))
                return hit.point;
            return new Vector3(p.x, transform.position.y, p.z);
        }

        void SolveLegs(float dt)
        {
            if (ikW <= 0.001f) { foreach (var l in legs) l.placed = false; return; }
            Quaternion yawRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            float T = CycleTime(), duty = Duty(), stride = speed * T;
            float gallop = TrotToGallop;
            bool gait = moveW > 0.05f;
            float liftH = Mathf.Lerp(0.07f, 0.13f, WalkToTrot) * rig.scale * Mathf.Max(0.35f, moveW);

            foreach (var l in legs)
            {
                float step = l.front ? clipPose.frontStep * rig.scale : 0f;
                Vector3 restW = transform.position + yawRot * (l.restLocal + Vector3.forward * step);
                if (!l.placed) { l.plant = Ground(restW); l.ground = l.plant; l.placed = true; l.stepping = false; }
                l.swingS = -1;
                if (gait)
                {
                    float lp = Mathf.Repeat(gaitPhase + Offset(l), 1f);
                    if (lp < duty)
                    {
                        if (l.stepping) { l.stepping = false; l.plant = l.to; }
                        l.ground = l.plant;
                    }
                    else
                    {
                        float s = (lp - duty) / (1f - duty);
                        if (!l.stepping) { l.stepping = true; l.from = l.plant; }
                        float timeLeft = (1f - s) * (1f - duty) * T;
                        Vector3 land = transform.position + vel * timeLeft + yawRot * (l.restLocal + Vector3.forward * stride * duty * 0.5f);
                        l.to = Ground(land);
                        float e = s * s * (3f - 2f * s);
                        float arc = Mathf.Sin(Mathf.PI * Mathf.Pow(s, l.front ? 0.8f : 1.1f));
                        l.ground = Vector3.Lerp(l.from, l.to, e) + Vector3.up * liftH * arc * (l.front ? 1.15f : 1f);
                        l.swingS = s;
                        l.stepT = s; l.stepDur = (1f - duty) * T;
                    }
                }
                else
                {
                    if (l.stepping)
                    {
                        l.stepT += dt / Mathf.Max(0.12f, l.stepDur);
                        float s = Mathf.Clamp01(l.stepT);
                        l.to = Ground(restW);
                        l.ground = Vector3.Lerp(l.from, l.to, s * s * (3f - 2f * s)) + Vector3.up * 0.05f * rig.scale * Mathf.Sin(Mathf.PI * s);
                        l.swingS = s;
                        if (s >= 1f) { l.stepping = false; l.plant = l.to; }
                    }
                    else
                    {
                        Vector3 d = restW - l.plant; d.y = 0;
                        bool diagonalBusy = false;
                        foreach (var o in legs) if (o != l && o.stepping && (o.front == l.front || o.side == l.side)) diagonalBusy = true;
                        if (d.magnitude > (step > 0 ? 0.05f : 0.1f) * rig.scale && !diagonalBusy)
                        { l.stepping = true; l.from = l.plant; l.to = Ground(restW); l.stepT = 0; l.stepDur = 0.22f * sizeK; }
                        l.ground = l.plant;
                    }
                }
                // rearing: the front paws come off the ground
                if (l.front && clipPose.frontLift > 0.001f)
                    l.ground += Vector3.up * clipPose.frontLift * rig.height * 0.55f + yawRot * Vector3.forward * clipPose.frontLift * 0.15f * rig.scale;
            }

            // lower the body if a paw can't be reached
            float need = 0;
            foreach (var l in legs)
            {
                Vector3 target = JointTarget(l, yawRot);
                float len = Vector3.Distance(rig[l.a].position, rig[l.b].position) + Vector3.Distance(rig[l.b].position, rig[l.c].position);
                need = Mathf.Max(need, Vector3.Distance(rig[l.a].position, target) - len * 0.985f);
            }
            bodyDrop = Mathf.Lerp(bodyDrop, Mathf.Clamp(need, 0, rig.height * 0.3f), 1 - Mathf.Exp(-dt * 16f));
            if (bodyDrop > 0.0005f) rig[Q.Pelvis].position += Vector3.down * bodyDrop * ikW;

            foreach (var l in legs)
            {
                Vector3 target = JointTarget(l, yawRot);
                // elbows point back, stifles point forward
                Vector3 pole = yawRot * (l.front ? new Vector3(0, -0.2f, -1f) : new Vector3(0, -0.2f, 1f));
                HumanoidAnimator.SolveTwoBone(rig[l.a], rig[l.b], rig[l.c], target, pole, ikW);
                // the pastern/metatarsus reaches the paw's ground point
                var c = rig[l.c];
                Vector3 pawNow = rig[l.paw].position;
                Vector3 want = l.ground + Vector3.up * rig.bindModel[(int)l.paw].y * rig.scale;
                Quaternion fix = Quaternion.FromToRotation(pawNow - c.position, want - c.position);
                c.rotation = Quaternion.Slerp(c.rotation, fix * c.rotation, ikW);
                // paw flat on the ground in stance, toes curled back in the swing
                float curl = l.swingS >= 0 ? Mathf.Sin(Mathf.PI * Mathf.Min(1f, l.swingS * 1.3f)) * (l.front ? 70f : 45f) : 0f;
                rig[l.paw].rotation = Quaternion.Slerp(rig[l.paw].rotation, yawRot * Quaternion.Euler(curl, 0, 0), ikW);
            }
        }

        /// <summary>Where the joint above the paw (carpus or hock) must be for the paw to touch l.ground.</summary>
        Vector3 JointTarget(Leg l, Quaternion yawRot)
        {
            Vector3 v = l.cToPaw;
            // in the swing the pastern/metatarsus folds back (forelegs flip the paw up behind)
            if (l.swingS >= 0)
            {
                float fold = Mathf.Sin(Mathf.PI * Mathf.Min(1f, l.swingS * 1.25f)) * (l.front ? 70f : 35f);
                v = Quaternion.Euler(l.front ? fold : -fold, 0, 0) * v;
            }
            Vector3 pawCentre = l.ground + Vector3.up * rig.bindModel[(int)l.paw].y * rig.scale;
            return pawCentre - yawRot * v;
        }
    }
}
