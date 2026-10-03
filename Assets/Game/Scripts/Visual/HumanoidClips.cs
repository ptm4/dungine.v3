using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>
    /// The keyed actions for humanoids. Every pose is an offset from the combat stance for that grip; the clip starts
    /// and ends on the stance. Keys are placed so a strike has a wind-up (anticipation), a fast strike, an overshoot
    /// (follow-through) and a settle, and the feet step into the blow (the leg IK keeps them planted).
    /// </summary>
    public static class HumanoidClips
    {
        static readonly Dictionary<(AnimAct, Grip, int), Clip> cache = new Dictionary<(AnimAct, Grip, int), Clip>();

        static Pose P() => new Pose();

        /// <summary>v3 dev switch: 'A' plays the throw for CastPoint (default), 'B' the push. CastRaise is always the push.</summary>
        public static char Cast = 'A';

        public static Clip Get(AnimAct a, Grip g, int variant)
        {
            if (a == AnimAct.CastPoint) variant = Cast == 'B' ? 1 : 0;   // v3: the cast switch is part of the cache key
            if (cache.TryGetValue((a, g, variant), out var c)) return c;
            c = Build(a, g, variant) ?? new Clip();
            cache[(a, g, variant)] = c;
            return c;
        }

        // =============================================================================================================
        //  v3 (Peter's notes, 2026-10-03): the stances, and arm poses keyed as absolute angles.
        //  "hands should be out not up ... a fighter stance, left arm is extended and horizon at chest level as a guard
        //  ... the right hand is the striker". The unarmed and one-handed stance is now that guard; the others are v2's.
        // =============================================================================================================

        /// <summary>The combat stance for a grip, at full weight, as offsets added over the idle (HumanoidAnimator.Evaluate).</summary>
        public static Pose StancePose(Grip g)
        {
            var p = new Pose();
            switch (g)
            {
                case Grip.Bow:
                    p.R(B.UpperArmL, -26, 0, -6).R(B.LowerArmL, -38, -10, 0).R(B.UpperArmR, -12, 0, 6).R(B.LowerArmR, -40, 0, 0).R(B.Chest, 0, 10, 0);
                    break;
                case Grip.Crossbow:
                    p.R(B.UpperArmR, -20, 0, 8).R(B.LowerArmR, -70, 0, 0).R(B.UpperArmL, -30, 0, -10).R(B.LowerArmL, -60, 20, 0);
                    break;
                case Grip.TwoHand:
                    // blade held up and across, point toward the foe; the off hand is placed by IK
                    p.R(B.UpperArmR, -28, -24, 18).R(B.LowerArmR, -78, 0, 0).R(B.HandR, -10, 0, 28)
                     .R(B.UpperArmL, -40, 30, 4).R(B.LowerArmL, -60, 0, 0).R(B.Chest, 0, 20, 0).R(B.Spine, 0, 6, 0);
                    break;
                case Grip.Polearm:
                    // rear hand low by the hip, haft slanting forward and up, head leading
                    p.R(B.UpperArmR, 8, -10, 16).R(B.LowerArmR, -62, 18, 0).R(B.HandR, -38, 0, 12)
                     .R(B.UpperArmL, -45, 20, -6).R(B.LowerArmL, -50, 0, 0).R(B.Chest, 0, 26, 0).R(B.Spine, 0, 8, 0).R(B.Head, 0, -22, 0);
                    break;
                case Grip.Shield:
                    // v2's shield arm; the striking arm as in the guard, so the strike combo starts from the same place
                    p.R(B.UpperArmL, -34, 20, -14).R(B.LowerArmL, -80, 55, 0).R(B.Chest, 0, 12, 0);
                    Guard(p, true, true);
                    break;
                default:   // unarmed and one-handed: the guard
                    Guard(p, false, g != Grip.Unarmed);
                    p.R(B.Chest, 0, 12, 0);
                    break;
            }
            return p;
        }

        // The guard, as absolute angles (degrees: x forward is negative; y turns a raised arm toward the figure's right;
        // z lifts the right arm out and the left arm in). The striking hand is ready in front of the right ribs, the
        // forearm level and pointing at the foe; the left arm is straight out in front at shoulder height, a touch inward.
        static readonly Vector3 GuardR = new Vector3(-25, -10, 0), GuardRLow = new Vector3(-85, 0, 0), GuardRHand = new Vector3(-10, 0, 0);
        // v2 holds a weapon square to the hand (its blade points forward when the arm hangs), so a wrist angle aims it: the
        // blade's pitch is the sum of the arm's x angles. These turn the blade forward and a little up at the foe.
        static readonly Vector3 GuardRHandArmed = new Vector3(80, 0, 0);
        /// <summary>The striking hand's x angle that puts the blade (or the fist's line) along 'pitch' (0 = straight ahead).</summary>
        static Vector3 HandFor(Grip g, float upperX, float lowerX, float pitch = 0, float fist = -6)
            => new Vector3(g == Grip.Unarmed ? fist : pitch - upperX - lowerX, 0, 0);
        // the stab's chamber: a fist drawn back by the ribs; a blade drawn back by the hip, level, point at the foe
        static Vector3 ChamberU(Grip g) => g == Grip.Unarmed ? V(-20, -10, 4) : V(-10, -15, 0);
        static Vector3 ChamberL(Grip g) => g == Grip.Unarmed ? V(-120) : V(-80);
        static readonly Vector3 GuardL = new Vector3(-88, 12, -3), GuardLLow = new Vector3(-12, 0, 0), GuardLHand = new Vector3(-12, 0, -4);

        static void Guard(Pose p, bool rightOnly, bool armed)
        {
            p.r[(int)B.UpperArmR] = GuardR - Idle(B.UpperArmR);
            p.r[(int)B.LowerArmR] = GuardRLow - Idle(B.LowerArmR);
            p.r[(int)B.HandR] = (armed ? GuardRHandArmed : GuardRHand) - Idle(B.HandR);
            if (rightOnly) return;
            p.r[(int)B.UpperArmL] = GuardL - Idle(B.UpperArmL);
            p.r[(int)B.LowerArmL] = GuardLLow - Idle(B.LowerArmL);
            p.r[(int)B.HandL] = GuardLHand - Idle(B.HandL);
        }

        /// <summary>The idle's own arm angles (Evaluate's relaxed idle, breathing left out).</summary>
        static Vector3 Idle(B b)
        {
            switch (b)
            {
                case B.UpperArmR: return new Vector3(3, 0, -4.5f);
                case B.UpperArmL: return new Vector3(3, 0, 4.5f);
                case B.LowerArmR: case B.LowerArmL: return new Vector3(-14, 0, 0);
                case B.HandR: return new Vector3(-4, 0, 6);
                case B.HandL: return new Vector3(-4, 0, -6);
                default: return Vector3.zero;
            }
        }

        /// <summary>Sets arm bones of a key so they reach the given absolute angles from grip g's stance.</summary>
        static Pose With(this Pose p, Grip g, params (B bone, Vector3 abs)[] targets)
        {
            var st = StancePose(g);
            foreach (var (b, abs) in targets) p.r[(int)b] = abs - Idle(b) - st.r[(int)b];
            return p;
        }

        static Vector3 V(float x, float y = 0, float z = 0) => new Vector3(x, y, z);

        /// <summary>Peter's strike: from the guard the right hand swings left, swings right, then stabs straight forward
        /// as the third and final blow. The left arm keeps its guard throughout.</summary>
        static Clip Combo(Grip g)
        {
            // wind-up: blade cocked up behind the right shoulder; the swings and the stab carry it level, along the arm
            var windL = P().R(B.Chest, 0, 16, 0).R(B.Spine, 0, 8, 0).Hip(0, 0, -0.02f)
                .With(g, (B.UpperArmR, V(-95, 35, 30)), (B.LowerArmR, V(-55)), (B.HandR, HandFor(g, -95, -55, -120, -10)));
            var swingL = P().R(B.Chest, 0, -22, 0).R(B.Spine, 4, -10, 0).Hip(0, -0.02f, 0.04f)
                .With(g, (B.UpperArmR, V(-80, -50, 5)), (B.LowerArmR, V(-8)), (B.HandR, HandFor(g, -80, -8, 5)));
            var overL = P().R(B.Chest, 0, -26, 0).R(B.Spine, 4, -12, 0).Hip(0, -0.02f, 0.04f)
                .With(g, (B.UpperArmR, V(-78, -58, 5)), (B.LowerArmR, V(-10)), (B.HandR, HandFor(g, -78, -10, 5)));
            var swingR = P().R(B.Chest, 0, 12, 0).R(B.Spine, 4, 6, 0).Hip(0, -0.02f, 0.04f)
                .With(g, (B.UpperArmR, V(-80, 15, 5)), (B.LowerArmR, V(-8)), (B.HandR, HandFor(g, -80, -8, 5)));
            var overR = P().R(B.Chest, 0, 14, 0).R(B.Spine, 4, 7, 0).Hip(0, -0.02f, 0.04f)
                .With(g, (B.UpperArmR, V(-78, 22, 7)), (B.LowerArmR, V(-10)), (B.HandR, HandFor(g, -78, -10, 5)));
            var chamber = P().R(B.Chest, 0, 10, 0).R(B.Spine, -2, 4, 0).Hip(0, -0.01f, -0.05f)
                .With(g, (B.UpperArmR, ChamberU(g)), (B.LowerArmR, ChamberL(g)), (B.HandR, HandFor(g, ChamberU(g).x, ChamberL(g).x, -20, -10)));
            var stab = P().R(B.Chest, 0, -20, 0).R(B.Spine, 10, -10, 0).Hip(0, -0.06f, 0.16f).StepL(0, 0.28f)
                .With(g, (B.UpperArmR, V(-100, 5, 4)), (B.LowerArmR, V(-2)), (B.HandR, HandFor(g, -100, -2, -8)));
            return new Clip().K(0.1f, windL).K(0.22f, swingL).K(0.29f, overL).K(0.42f, swingR).K(0.5f, overR)
                .K(0.6f, chamber).K(0.72f, stab).K(0.8f, stab.Scaled(1.03f)).K(0.95f, stab.Scaled(0.3f));
        }

        /// <summary>The stab alone (a thrust, or a straight punch with a smaller lunge).</summary>
        static Clip Stab(Grip g, float lunge)
        {
            var chamber = P().R(B.Chest, 0, 10, 0).R(B.Spine, -2, 4, 0).Hip(0, -0.01f, -0.05f)
                .With(g, (B.UpperArmR, ChamberU(g)), (B.LowerArmR, ChamberL(g)), (B.HandR, HandFor(g, ChamberU(g).x, ChamberL(g).x, -20, -10)));
            var stab = P().R(B.Chest, 0, -20, 0).R(B.Spine, 10, -10, 0).Hip(0, -0.05f, 0.16f * lunge).StepL(0, 0.28f * lunge)
                .With(g, (B.UpperArmR, V(-100, 5, 4)), (B.LowerArmR, V(-2)), (B.HandR, HandFor(g, -100, -2, -8)));
            return new Clip().K(0.28f, chamber).K(0.45f, stab).K(0.55f, stab.Scaled(1.03f)).K(0.8f, stab.Scaled(0.3f));
        }

        /// <summary>Cast A, the throw: the casting hand tucked under the far armpit, then swept out to full reach at the
        /// target, like throwing; the other arm straight out at the target the whole time. Elbows stay in.</summary>
        static Clip CastThrow(Grip g)
        {
            // the steadying arm stays pointed at the target while the chest coils and uncoils under it, so its y
            // angle counters each key's chest turn (measured: tools probe, fighter rig)
            (B, Vector3) Aim(float y, float x = -85) => (B.UpperArmL, V(x, y, -2));
            var aimLow = (B.LowerArmL, V(-3));
            var aimHand = (B.HandL, V(-20, 0, -4));
            var settle = P().With(g, Aim(1), aimLow, aimHand);
            var tuck = P().R(B.Chest, 0, -18, 0).R(B.Spine, -2, -8, 0).Hip(0, -0.01f, -0.03f)
                .With(g, Aim(26), aimLow, aimHand, (B.UpperArmR, V(-35, -75, -12)), (B.LowerArmR, V(-120)), (B.HandR, V(0)));
            // release: the chest squares up to the target and the hand sweeps out to full reach at it
            var release = P().R(B.Chest, 0, -6, 0).R(B.Spine, 6, 0, 0).R(B.Head, 4).Hip(0, -0.02f, 0.08f).StepR(0, 0.14f)
                .With(g, Aim(-1, -97), aimLow, aimHand, (B.UpperArmR, V(-92, -32, 0)), (B.LowerArmR, V(-3)), (B.HandR, V(-15)));
            var follow = P().R(B.Chest, 0, 0, 0).R(B.Spine, 7, 2, 0).R(B.Head, 4).Hip(0, -0.02f, 0.09f).StepR(0, 0.15f)
                .With(g, Aim(-6, -98), aimLow, aimHand, (B.UpperArmR, V(-95, -24, 2)), (B.LowerArmR, V(-2)), (B.HandR, V(-15)));
            return new Clip().K(0.1f, settle).K(0.3f, tuck).K(0.4f, tuck.Scaled(1.04f)).K(0.52f, release).K(0.64f, follow).K(0.86f, follow.Scaled(0.35f));
        }

        /// <summary>Cast B, the push: both hands meet at the chest as if holding a ball, then push it out to full reach.</summary>
        static Clip CastPush(Grip g)
        {
            // the chest squares up to the target (the guard turns it 12 degrees), so the ball sits in the middle
            var gather = P().R(B.Chest, -6, -12, 0).Hip(0, 0, -0.03f)
                .With(g, (B.UpperArmR, V(-25, -38, -6)), (B.UpperArmL, V(-25, 38, 6)), (B.LowerArmR, V(-118)), (B.LowerArmL, V(-118)),
                         (B.HandR, V(-10)), (B.HandL, V(-10)));
            var push = P().R(B.Chest, 0, -12, 0).R(B.Spine, 6, 0, 0).Hip(0, -0.03f, 0.1f).StepL(0, 0.18f)
                .With(g, (B.UpperArmR, V(-104, -12, -4)), (B.UpperArmL, V(-104, 12, 4)), (B.LowerArmR, V(-4)), (B.LowerArmL, V(-4)),
                         (B.HandR, V(-35)), (B.HandL, V(-35)));
            return new Clip().K(0.3f, gather).K(0.42f, gather.Scaled(1.03f)).K(0.55f, push).K(0.68f, push.Scaled(1.03f)).K(0.88f, push.Scaled(0.3f));
        }

        static Clip Build(AnimAct a, Grip g, int v)
        {
            bool pole = g == Grip.Polearm, heavy = g == Grip.TwoHand;
            switch (a)
            {
                // ------------------------------------------------------------------ weapons
                case AnimAct.Slash:
                    if (Anim.Combo(a, g)) return Combo(g);   // v3: Peter's three-part strike
                    if (pole || heavy)
                    {
                        // the body winds up to the right and sweeps the blade across to the left
                        var wind = P().R(B.Chest, -2, -34, 0).R(B.Spine, 0, -20, 0).Hip(0, -0.03f, -0.05f).StepR(0.02f, -0.06f)
                            .WGrip(0.06f, 0.18f, -0.06f).WAim(pole ? -30 : -40, 55, pole ? -30 : 20);
                        var strike = P().R(B.Chest, 6, 40, 0).R(B.Spine, 6, 24, 0).Hip(0, -0.07f, 0.13f).StepL(0.04f, 0.3f)
                            .WGrip(-0.08f, 0.06f, 0.16f).WAim(pole ? 16 : 40, -60, pole ? 20 : -20);
                        return new Clip().K(0.3f, wind).K(0.38f, wind.Scaled(1.05f)).K(0.47f, strike).K(0.58f, strike.Scaled(1.08f)).K(0.8f, strike.Scaled(0.3f));
                    }
                    if (v == 0)
                    {
                        // forehand: high right to low left
                        var wind = P().R(B.UpperArmR, -98, 0, 48).R(B.LowerArmR, -28).R(B.HandR, -22).R(B.Spine, 0, -24, 0).R(B.Chest, -4, -16, 0)
                            .Hip(0, -0.012f, -0.035f);
                        var strike = P().R(B.UpperArmR, -22, 0, -46).R(B.LowerArmR, 52).R(B.HandR, 10).R(B.Spine, 9, 28, 0).R(B.Chest, 6, 18, 0)
                            .Hip(0, -0.05f, 0.11f).StepR(0.02f, 0.26f);
                        return new Clip().K(0.3f, wind).K(0.38f, wind.Scaled(1.05f)).K(0.47f, strike).K(0.57f, strike.Scaled(1.1f)).K(0.8f, strike.Scaled(0.35f));
                    }
                    else
                    {
                        // backhand: across from the left
                        var wind = P().R(B.UpperArmR, -62, 42, 58).R(B.LowerArmR, -44).R(B.Spine, 0, 30, 0).R(B.Chest, 0, 14, 0).Hip(0, -0.012f, -0.03f);
                        var strike = P().R(B.UpperArmR, -52, -32, -22).R(B.LowerArmR, 58).R(B.Spine, 5, -30, 0).R(B.Chest, 0, -16, 0)
                            .Hip(0, -0.05f, 0.1f).StepR(0.06f, 0.2f);
                        return new Clip().K(0.3f, wind).K(0.38f, wind.Scaled(1.05f)).K(0.47f, strike).K(0.57f, strike.Scaled(1.1f)).K(0.8f, strike.Scaled(0.35f));
                    }
                case AnimAct.Overhead:
                    {
                        if (pole || heavy)
                        {
                            // raise the weapon high over the right shoulder, then chop down in front
                            var up = P().R(B.Chest, -14, -10, 0).R(B.Spine, -6).Hip(0, 0.01f, -0.05f).R(B.Head, -6)
                                .WGrip(0.02f, 0.62f, -0.18f).WAim(pole ? -95 : -120, 16, 0);
                            var chop = P().R(B.Chest, 22, 6, 0).R(B.Spine, 16).Hip(0, -0.09f, 0.14f).StepL(0.02f, 0.32f).R(B.Head, 8)
                                .WGrip(-0.1f, 0.12f, 0.14f).WAim(pole ? 34 : 66, 10, 0);
                            return new Clip().K(0.32f, up).K(0.42f, up.Scaled(1.05f)).K(0.53f, chop).K(0.63f, chop.Scaled(1.06f)).K(0.84f, chop.Scaled(0.3f));
                        }
                        var wind = P().Both(B.UpperArmR, B.UpperArmL, -128, 0, 6).Both(B.LowerArmR, B.LowerArmL, -42).R(B.Chest, -16, 0, 0).R(B.Spine, -8, 0, 0)
                            .Hip(0, 0.01f, -0.04f).R(B.Head, -8);
                        var strike = P().Both(B.UpperArmR, B.UpperArmL, -32, 0, -10).Both(B.LowerArmR, B.LowerArmL, 42).R(B.Chest, 26, 0, 0).R(B.Spine, 18, 0, 0)
                            .Hip(0, -0.08f, 0.14f).StepR(0, 0.3f).R(B.Head, 10);
                        return new Clip().K(0.32f, wind).K(0.42f, wind.Scaled(1.06f)).K(0.53f, strike).K(0.63f, strike.Scaled(1.1f)).K(0.84f, strike.Scaled(0.3f));
                    }
                case AnimAct.Thrust:
                    {
                        if (pole || heavy)
                        {
                            // draw back along the haft, then drive it straight out with a lunge
                            var back = P().R(B.Chest, 0, -12, 0).R(B.Spine, -4).Hip(0, -0.02f, -0.1f).WGrip(0.04f, 0.04f, -0.2f).WAim(6, 10, 0);
                            var lunge = P().R(B.Chest, 4, 10, 0).R(B.Spine, 12).Hip(0, -0.08f, 0.24f).StepL(0, 0.38f).WGrip(-0.1f, 0.1f, 0.34f).WAim(20, 14, 0);
                            return new Clip().K(0.28f, back).K(0.36f, back.Scaled(1.04f)).K(0.45f, lunge).K(0.56f, lunge.Scaled(1.06f)).K(0.8f, lunge.Scaled(0.3f));
                        }
                        if (g == Grip.OneHand || g == Grip.Unarmed || g == Grip.Shield) return Stab(g, 1f);   // v3: from the guard
                        var w1 = P().R(B.UpperArmR, 24, 0, 6).R(B.LowerArmR, -52).R(B.Spine, 0, -18, 0).Hip(0, -0.01f, -0.05f);
                        var s1 = P().R(B.UpperArmR, -64, 0, -10).R(B.LowerArmR, 72).R(B.Spine, 9, 16, 0).Hip(0, -0.05f, 0.14f).StepR(0.02f, 0.3f);
                        return new Clip().K(0.28f, w1).K(0.36f, w1.Scaled(1.04f)).K(0.45f, s1).K(0.55f, s1.Scaled(1.08f)).K(0.78f, s1.Scaled(0.3f));
                    }
                case AnimAct.Punch:
                    {
                        if (g == Grip.Unarmed || g == Grip.OneHand || g == Grip.Shield) return Stab(g, 0.7f);   // v3: a straight punch from the guard
                        bool left = v == 1;
                        B ua = left ? B.UpperArmL : B.UpperArmR, la = left ? B.LowerArmL : B.LowerArmR;
                        float sg = left ? -1 : 1;
                        var wind = P().R(ua, 20, 0, 6 * sg).R(la, -40).R(B.Spine, 0, -16 * sg, 0).Hip(0, -0.02f, -0.03f);
                        var strike = P().R(ua, -62, 0, -12 * sg).R(la, 80).R(B.Spine, 8, 18 * sg, 0).R(B.Chest, 0, 10 * sg, 0).Hip(0, -0.04f, 0.1f);
                        if (left) strike.StepL(0, 0.2f); else strike.StepR(0, 0.2f);
                        return new Clip().K(0.26f, wind).K(0.42f, strike).K(0.52f, strike.Scaled(1.06f)).K(0.75f, strike.Scaled(0.25f));
                    }
                case AnimAct.Shove:
                    {
                        var wind = P().Both(B.UpperArmR, B.UpperArmL, -50, 0, -10).Both(B.LowerArmR, B.LowerArmL, -100).Hip(0, -0.03f, -0.04f);
                        var strike = P().Both(B.UpperArmR, B.UpperArmL, -82, 0, -12).Both(B.LowerArmR, B.LowerArmL, -5).R(B.Spine, 18).Hip(0, -0.06f, 0.2f).StepR(0, 0.34f);
                        return new Clip().K(0.28f, wind).K(0.45f, strike).K(0.58f, strike.Scaled(1.05f)).K(0.82f, strike.Scaled(0.3f));
                    }
                case AnimAct.Claw:
                case AnimAct.Bite:
                    {
                        var wind = P().Both(B.UpperArmR, B.UpperArmL, -100, 0, 35).Both(B.LowerArmR, B.LowerArmL, -20).R(B.Spine, -10).R(B.Head, -20).Hip(0, -0.02f, -0.05f);
                        var strike = P().Both(B.UpperArmR, B.UpperArmL, -40, 0, -30).Both(B.LowerArmR, B.LowerArmL, 10).R(B.Spine, 28).R(B.Head, 15).Hip(0, -0.06f, 0.2f).StepR(0, 0.3f);
                        return new Clip().K(0.3f, wind).K(0.38f, wind.Scaled(1.05f)).K(0.48f, strike).K(0.6f, strike.Scaled(1.06f)).K(0.82f, strike.Scaled(0.3f));
                    }
                case AnimAct.Bow:
                    {
                        var raise = P().R(B.UpperArmL, -62, 12, -4).R(B.LowerArmL, 34).R(B.UpperArmR, -66, -18, -20).R(B.LowerArmR, -55).R(B.Chest, 0, 14, 0).R(B.Head, 0, -12, 0);
                        var drawn = P().R(B.UpperArmL, -64, 12, -4).R(B.LowerArmL, 36).R(B.UpperArmR, -68, -26, -24).R(B.LowerArmR, -112).R(B.Chest, -2, 18, 0).R(B.Head, 0, -14, 0);
                        var loose = P().R(B.UpperArmL, -60, 12, -4).R(B.LowerArmL, 34).R(B.UpperArmR, -56, -40, -30).R(B.LowerArmR, -90).R(B.Chest, 0, 16, 0).R(B.Head, 0, -12, 0);
                        return new Clip().K(0.22f, raise).K(0.58f, drawn).K(0.65f, drawn).K(0.7f, loose).K(0.86f, loose.Scaled(0.6f));
                    }
                case AnimAct.Crossbow:
                    {
                        var aim = P().Both(B.UpperArmR, B.UpperArmL, -72, 0, -8).R(B.LowerArmR, -20).R(B.LowerArmL, -10, 30).R(B.Head, 6);
                        var kick = P().Both(B.UpperArmR, B.UpperArmL, -64, 0, -8).R(B.LowerArmR, -20).R(B.LowerArmL, -10, 30).R(B.Chest, -8).Hip(0, 0, -0.03f);
                        return new Clip().K(0.3f, aim).K(0.48f, aim).K(0.53f, kick).K(0.7f, aim.Scaled(0.8f));
                    }
                case AnimAct.Throw:
                    {
                        var wind = P().R(B.UpperArmR, -140, 0, 22).R(B.LowerArmR, -72).R(B.Spine, -8, -26, 0).Hip(0, -0.01f, -0.05f).StepL(0, 0.12f);
                        var rel = P().R(B.UpperArmR, -62, 0, -12).R(B.LowerArmR, 12).R(B.Spine, 12, 22, 0).Hip(0, -0.04f, 0.12f).StepL(0, 0.2f);
                        return new Clip().K(0.34f, wind).K(0.42f, wind.Scaled(1.05f)).K(0.52f, rel).K(0.62f, rel.Scaled(1.08f)).K(0.84f, rel.Scaled(0.3f));
                    }
                case AnimAct.Stomp:
                    {
                        var up = P().R(B.Spine, -6).Both(B.UpperArmR, B.UpperArmL, 0, 0, 25).StepR(0, 0.12f).Hip(0, 0.03f, 0);
                        var down = P().R(B.Spine, 10).Both(B.UpperArmR, B.UpperArmL, 0, 0, 34).StepR(0, 0.2f).Hip(0, -0.08f, 0.05f);
                        return new Clip().K(0.35f, up).K(0.48f, down).K(0.6f, down.Scaled(1.05f)).K(0.85f, down.Scaled(0.3f));
                    }

                // ------------------------------------------------------------------ magic
                case AnimAct.CastPoint:
                    {
                        if (Cast == 'B') return CastPush(g);
                        return CastThrow(g);   // v3: Peter's cast A
                        var gather = P().Both(B.UpperArmR, B.UpperArmL, -45, 0, -6).Both(B.LowerArmR, B.LowerArmL, -78, 30).R(B.Spine, -6, -12, 0).R(B.Chest, -4).Hip(0, -0.01f, -0.03f);
                        var rel = P().R(B.UpperArmR, -86, 0, -4).R(B.LowerArmR, 58).R(B.UpperArmL, -12, 0, -16).R(B.Spine, 10, 14, 0).R(B.Head, 4).Hip(0, -0.03f, 0.07f).StepR(0, 0.12f);
                        return new Clip().K(0.3f, gather).K(0.4f, gather.Scaled(1.08f)).K(0.5f, rel).K(0.64f, rel.Scaled(1.04f)).K(0.84f, rel.Scaled(0.35f));
                    }
                case AnimAct.CastRaise:
                    {
                        return CastPush(g);   // v3: Peter's cast B
                        var up = P().Both(B.UpperArmR, B.UpperArmL, -152, 0, 26).Both(B.LowerArmR, B.LowerArmL, 20).R(B.Chest, -16).R(B.Head, -20).Hip(0, 0.03f, 0);
                        var slam = P().Both(B.UpperArmR, B.UpperArmL, -60, 0, 40).Both(B.LowerArmR, B.LowerArmL, 10).R(B.Chest, 18).R(B.Spine, 8).R(B.Head, 8).Hip(0, -0.07f, 0.04f);
                        return new Clip().K(0.36f, up).K(0.5f, up.Scaled(1.04f)).K(0.58f, slam).K(0.7f, slam.Scaled(1.05f)).K(0.88f, slam.Scaled(0.3f));
                    }
                case AnimAct.CastTouch:
                case AnimAct.Heal:
                case AnimAct.Bless:
                    {
                        var reach = P().Both(B.UpperArmR, B.UpperArmL, -60, 0, -14).Both(B.LowerArmR, B.LowerArmL, -30, 40).R(B.Spine, 10).Hip(0, -0.02f, 0.05f);
                        if (a == AnimAct.Bless) reach.R(B.Head, 18);
                        if (a == AnimAct.Heal) reach.R(B.Head, 10);
                        return new Clip().K(0.35f, reach).K(0.7f, reach.Scaled(1.06f)).K(0.88f, reach.Scaled(0.4f));
                    }
                case AnimAct.Breath:
                case AnimAct.Roar:
                    {
                        var inh = P().R(B.Chest, -18).R(B.Head, -20).Both(B.UpperArmR, B.UpperArmL, 0, 0, 25).Hip(0, 0.01f, -0.04f);
                        var ex = P().R(B.Chest, 14).R(B.Head, 10).Both(B.UpperArmR, B.UpperArmL, -10, 0, 45).Hip(0, -0.03f, 0.05f);
                        return new Clip().K(0.36f, inh).K(0.5f, ex).K(0.72f, ex.Scaled(1.05f)).K(0.9f, ex.Scaled(0.3f));
                    }

                // ------------------------------------------------------------------ everything else
                case AnimAct.Drink:
                    {
                        var lift = P().R(B.UpperArmR, -48, 0, -20).R(B.LowerArmR, -125);
                        var tip = P().R(B.UpperArmR, -58, 0, -22).R(B.LowerArmR, -130).R(B.Head, -25).R(B.Neck, -10);
                        return new Clip().K(0.28f, lift).K(0.45f, tip).K(0.66f, tip).K(0.8f, lift.Scaled(0.6f));
                    }
                case AnimAct.Hit:
                    {
                        float sd = v == 0 ? 1 : -1;
                        var jolt = P().R(B.Spine, -14, 10 * sd, 4 * sd).R(B.Chest, -12).R(B.Head, -20, -12 * sd, 0).Both(B.UpperArmR, B.UpperArmL, -18, 0, 22).Hip(0, -0.01f, -0.08f).StepL(0, -0.08f);
                        return new Clip().K(0.14f, jolt).K(0.3f, jolt.Scaled(0.75f)).K(0.6f, jolt.Scaled(0.2f));
                    }
                case AnimAct.Dodge:
                    {
                        var lean = P().R(B.Spine, 0, 0, 18).R(B.Chest, -8, 0, 10).Hip(-0.14f, -0.05f, -0.02f).StepL(-0.26f, 0).StepR(-0.18f, 0);
                        return new Clip().K(0.22f, lean).K(0.55f, lean.Scaled(0.9f)).K(0.8f, lean.Scaled(0.3f));
                    }
                case AnimAct.Cheer:
                    {
                        var up = P().Both(B.UpperArmR, B.UpperArmL, -150, 0, 30).R(B.Head, -15).Hip(0, 0.03f, 0);
                        var hop = P().Both(B.UpperArmR, B.UpperArmL, -158, 0, 34).R(B.Head, -18).Hip(0, 0.06f, 0);
                        return new Clip().K(0.2f, up).K(0.35f, hop).K(0.5f, up).K(0.65f, hop).K(0.82f, up.Scaled(0.6f));
                    }
                case AnimAct.Interact:
                    {
                        var reach = P().R(B.Spine, 28).R(B.Chest, 12).R(B.UpperArmR, -55, 0, -5).R(B.LowerArmR, -10).Hip(0, -0.1f, -0.02f).StepR(0, 0.16f);
                        return new Clip().K(0.35f, reach).K(0.62f, reach.Scaled(1.03f)).K(0.85f, reach.Scaled(0.3f));
                    }
                case AnimAct.Draw:
                    {
                        bool back = g == Grip.TwoHand || g == Grip.Polearm || g == Grip.Bow;
                        var reach = back
                            ? P().R(B.UpperArmR, -150, 0, 12).R(B.LowerArmR, -70).R(B.Chest, 0, -8, 0).R(B.Head, 0, 10, 0)
                            : P().R(B.UpperArmR, -30, 0, -28).R(B.LowerArmR, -58).R(B.Chest, 0, 14, 0).R(B.Spine, 6);
                        return new Clip().K(0.42f, reach).K(0.52f, reach.Scaled(1.02f));
                    }
            }
            return null;
        }
    }
}
