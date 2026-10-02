using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dungine
{
    /// <summary>Seedable RNG shared by rules code.</summary>
    public static class Rng
    {
        static System.Random _r = new System.Random(Environment.TickCount);
        public static void Seed(int s) => _r = new System.Random(s);
        public static int Range(int minInclusive, int maxExclusive) => _r.Next(minInclusive, maxExclusive);
        public static float Value => (float)_r.NextDouble();
        public static int D(int sides) => _r.Next(1, sides + 1);
        public static T Pick<T>(IList<T> list) => list[_r.Next(list.Count)];
        public static bool Chance(float p) => _r.NextDouble() < p;
    }

    public enum RollMode { Normal, Advantage, Disadvantage }

    /// <summary>A parsed dice expression like "2d6+3" or "1d8+1d6".</summary>
    [Serializable]
    public struct DiceExpr
    {
        public int count, sides, bonus;
        public int count2, sides2;

        public static DiceExpr Parse(string s)
        {
            var d = new DiceExpr();
            if (string.IsNullOrEmpty(s)) return d;
            s = s.Replace(" ", "").ToLowerInvariant();
            int i = 0; int sign = 1; bool first = true;
            while (i < s.Length)
            {
                if (s[i] == '+') { sign = 1; i++; continue; }
                if (s[i] == '-') { sign = -1; i++; continue; }
                int start = i;
                while (i < s.Length && char.IsDigit(s[i])) i++;
                int n = start == i ? 1 : int.Parse(s.Substring(start, i - start));
                if (i < s.Length && s[i] == 'd')
                {
                    i++;
                    int st = i;
                    while (i < s.Length && char.IsDigit(s[i])) i++;
                    int sides = int.Parse(s.Substring(st, i - st));
                    if (first) { d.count = n; d.sides = sides; first = false; }
                    else { d.count2 = n; d.sides2 = sides; }
                }
                else d.bonus += sign * n;
            }
            return d;
        }

        public bool IsEmpty => count == 0 && bonus == 0 && count2 == 0;
        public int Min => count + count2 + bonus;
        public int Max => count * sides + count2 * sides2 + bonus;
        public float Average => count * (sides + 1) / 2f + count2 * (sides2 + 1) / 2f + bonus;

        public DiceExpr WithExtraDice(int extra) { var d = this; d.count += extra; return d; }
        public DiceExpr WithBonus(int b) { var d = this; d.bonus += b; return d; }

        public int Roll(bool crit = false, bool rerollLow = false)
        {
            int total = bonus;
            int mult = crit ? 2 : 1;
            for (int i = 0; i < count * mult; i++) total += RollDie(sides, rerollLow);
            for (int i = 0; i < count2 * mult; i++) total += RollDie(sides2, rerollLow);
            return Mathf.Max(0, total);
        }

        static int RollDie(int sides, bool rerollLow)
        {
            int r = Rng.D(sides);
            if (rerollLow && r <= 2) r = Rng.D(sides);
            return r;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (count > 0) sb.Append(count).Append('d').Append(sides);
            if (count2 > 0) { if (sb.Length > 0) sb.Append('+'); sb.Append(count2).Append('d').Append(sides2); }
            if (bonus > 0) { if (sb.Length > 0) sb.Append('+'); sb.Append(bonus); }
            else if (bonus < 0) sb.Append(bonus);
            if (sb.Length == 0) sb.Append('0');
            return sb.ToString();
        }
    }

    public struct D20Result
    {
        public int natural, other, total, modifier, bonusDie;
        public RollMode mode;
        public bool Crit => natural == 20;
        public bool Fumble => natural == 1;
    }

    public static class D20
    {
        public static D20Result Roll(int modifier, RollMode mode, bool halflingLuck = false)
        {
            int a = Roll1(halflingLuck), b = Roll1(halflingLuck);
            var r = new D20Result { mode = mode, modifier = modifier };
            switch (mode)
            {
                case RollMode.Advantage: r.natural = Mathf.Max(a, b); r.other = Mathf.Min(a, b); break;
                case RollMode.Disadvantage: r.natural = Mathf.Min(a, b); r.other = Mathf.Max(a, b); break;
                default: r.natural = a; r.other = 0; break;
            }
            r.total = r.natural + modifier;
            return r;
        }

        static int Roll1(bool luck)
        {
            int r = Rng.D(20);
            if (luck && r == 1) r = Rng.D(20);
            return r;
        }

        public static RollMode Combine(int advSources, int disSources)
        {
            if (advSources > 0 && disSources == 0) return RollMode.Advantage;
            if (disSources > 0 && advSources == 0) return RollMode.Disadvantage;
            return RollMode.Normal;
        }

        /// <summary>Chance (0..1) that d20+mod >= dc under the roll mode. Nat 20 always hits, nat 1 always misses when attack=true.</summary>
        public static float Chance(int mod, int dc, RollMode mode, bool attack)
        {
            int need = dc - mod;
            float p = Mathf.Clamp01((21 - need) / 20f);
            if (attack) p = Mathf.Clamp(p, 0.05f, 0.95f);
            if (mode == RollMode.Advantage) p = 1 - (1 - p) * (1 - p);
            else if (mode == RollMode.Disadvantage) p = p * p;
            return p;
        }
    }
}
