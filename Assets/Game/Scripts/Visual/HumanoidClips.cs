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

        public static Clip Get(AnimAct a, Grip g, int variant)
        {
            if (cache.TryGetValue((a, g, variant), out var c)) return c;
            c = Build(a, g, variant) ?? new Clip();
            cache[(a, g, variant)] = c;
            return c;
        }

        static Clip Build(AnimAct a, Grip g, int v)
        {
            bool pole = g == Grip.Polearm, heavy = g == Grip.TwoHand;
            switch (a)
            {
                // ------------------------------------------------------------------ weapons
                case AnimAct.Slash:
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
                        var w1 = P().R(B.UpperArmR, 24, 0, 6).R(B.LowerArmR, -52).R(B.Spine, 0, -18, 0).Hip(0, -0.01f, -0.05f);
                        var s1 = P().R(B.UpperArmR, -64, 0, -10).R(B.LowerArmR, 72).R(B.Spine, 9, 16, 0).Hip(0, -0.05f, 0.14f).StepR(0.02f, 0.3f);
                        return new Clip().K(0.28f, w1).K(0.36f, w1.Scaled(1.04f)).K(0.45f, s1).K(0.55f, s1.Scaled(1.08f)).K(0.78f, s1.Scaled(0.3f));
                    }
                case AnimAct.Punch:
                    {
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
                        var gather = P().Both(B.UpperArmR, B.UpperArmL, -45, 0, -6).Both(B.LowerArmR, B.LowerArmL, -78, 30).R(B.Spine, -6, -12, 0).R(B.Chest, -4).Hip(0, -0.01f, -0.03f);
                        var rel = P().R(B.UpperArmR, -86, 0, -4).R(B.LowerArmR, 58).R(B.UpperArmL, -12, 0, -16).R(B.Spine, 10, 14, 0).R(B.Head, 4).Hip(0, -0.03f, 0.07f).StepR(0, 0.12f);
                        return new Clip().K(0.3f, gather).K(0.4f, gather.Scaled(1.08f)).K(0.5f, rel).K(0.64f, rel.Scaled(1.04f)).K(0.84f, rel.Scaled(0.35f));
                    }
                case AnimAct.CastRaise:
                    {
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
