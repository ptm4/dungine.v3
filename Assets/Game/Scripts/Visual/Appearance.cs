using System;
using System.Collections.Generic;
using Dungine.Rules;
using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>Everything that decides how a humanoid looks. Serialised with saves.</summary>
    [Serializable]
    public class Appearance
    {
        public RaceId race = RaceId.Human;
        public SubraceId subrace = SubraceId.None;
        public int bodyType;            // 0 = masculine, 1 = feminine
        public bool strong;             // heavier build
        public float heightAdj;         // -1..1
        public Color skin = new Color(.78f, .6f, .48f);
        public Color hair = new Color(.2f, .13f, .08f);
        public Color eyes = new Color(.3f, .45f, .6f);
        public Color cloth1 = new Color(.35f, .12f, .12f);
        public Color cloth2 = new Color(.2f, .18f, .16f);
        public Color horn = new Color(.15f, .12f, .12f);
        public int hairStyle = 1;
        public int beardStyle;
        public int hornStyle;
        public int faceShape;
        public bool tail;
        public float age;               // 0 young .. 1 old

        // Monster / NPC dressing
        public bool ghostly, skeletal, rotting, hunched, headless, gaunt, hooded, cape, crown, child;
        public Color glowEyes = Color.clear;
        public Color capeColor = new Color(.1f, .05f, .06f);
        public Color capeLining = new Color(.45f, .04f, .06f);
        public float bulk = 1f;         // extra girth multiplier (fat merchant etc.)
        // cosmetic: how the armour is drawn regardless of what it is (-1 = as the item), and the steel's colour
        public int armorLook = -1;
        public Color metalTint = new Color(.58f, .58f, .6f);

        public Appearance Clone() => (Appearance)MemberwiseClone();
    }

    [Serializable]
    public class GearLook
    {
        public ArmorVisual armor = ArmorVisual.Cloth;
        public WeaponVisual main = WeaponVisual.None;
        public WeaponVisual off = WeaponVisual.None;
        public bool helmet;
        public bool drawn;
    }

    public enum EarType { Human, Elf, LongElf, HalfElf, Gnome, Githyanki, Orc, Firbolg, None }

    public class RaceLook
    {
        public float height;      // meters, average masculine
        public float headRatio;   // total height / head height
        public float legFrac;     // hip joint height / total
        public float width;       // girth multiplier
        public EarType ears;
        public bool dragon, horns, tailDefault, tusks, pointedTeeth;
        public Color[] skins;
        public Color[] hairs;
        public Color[] eyeColors;
        public float femHeight = 0.94f;
    }

    public static class RaceLooks
    {
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static readonly Color[] HumanSkins = { C("#e3bba2"), C("#d6a684"), C("#c4916c"), C("#ae7a56"), C("#925f41"), C("#724730"), C("#553322"), C("#3c2418") };
        static readonly Color[] NaturalHair = { C("#1b1411"), C("#3a261a"), C("#5b3a22"), C("#8a5a32"), C("#b48a55"), C("#d8b878"), C("#8e2f1c"), C("#b54e25"), C("#9a9591"), C("#e8e2d6") };
        static readonly Color[] NaturalEyes = { C("#3b6e9e"), C("#4d7d4a"), C("#6b4b2e"), C("#2e1f16"), C("#7b8a91"), C("#9b7a3a") };

        public static readonly Dictionary<RaceId, RaceLook> Looks = new Dictionary<RaceId, RaceLook>
        {
            [RaceId.Human] = new RaceLook { height = 1.78f, headRatio = 7.4f, legFrac = .505f, width = 1f, ears = EarType.Human, skins = HumanSkins, hairs = NaturalHair, eyeColors = NaturalEyes },
            [RaceId.Elf] = new RaceLook { height = 1.80f, headRatio = 7.6f, legFrac = .52f, width = .88f, ears = EarType.LongElf, skins = new[] { C("#e4c7b0"), C("#d9b294"), C("#c69676"), C("#ae7c58"), C("#85583c"), C("#d6c0ae") }, hairs = NaturalHair, eyeColors = new[] { C("#4a9a7a"), C("#6aa0c8"), C("#8a6ac0"), C("#c8a040"), C("#5b7b3a") } },
            [RaceId.Drow] = new RaceLook { height = 1.74f, headRatio = 7.6f, legFrac = .52f, width = .88f, ears = EarType.LongElf, skins = new[] { C("#3b3a4a"), C("#4a4058"), C("#2f2c3a"), C("#565068"), C("#3e3548"), C("#5b5a6c") }, hairs = new[] { C("#f2f0f4"), C("#d8d4e0"), C("#bfb8c8"), C("#a8a0b8"), C("#e8e0c8"), C("#302838") }, eyeColors = new[] { C("#d83a3a"), C("#c860c8"), C("#e8e0f0"), C("#e0a030") } },
            [RaceId.HalfElf] = new RaceLook { height = 1.78f, headRatio = 7.5f, legFrac = .51f, width = .94f, ears = EarType.HalfElf, skins = HumanSkins, hairs = NaturalHair, eyeColors = NaturalEyes },
            [RaceId.HalfOrc] = new RaceLook { height = 1.90f, headRatio = 7.3f, legFrac = .5f, width = 1.18f, ears = EarType.Orc, tusks = true, skins = new[] { C("#7d8f6a"), C("#6a7d5a"), C("#8a9a78"), C("#5e6e52"), C("#9a8f7a"), C("#7a7468"), C("#556048") }, hairs = NaturalHair, eyeColors = new[] { C("#c8a040"), C("#8a3a2a"), C("#4d7d4a"), C("#2e1f16") } },
            [RaceId.Halfling] = new RaceLook { height = 0.98f, headRatio = 5.6f, legFrac = .44f, width = 1.15f, ears = EarType.HalfElf, skins = HumanSkins, hairs = NaturalHair, eyeColors = NaturalEyes },
            [RaceId.Dwarf] = new RaceLook { height = 1.36f, headRatio = 6.0f, legFrac = .41f, width = 1.36f, ears = EarType.Human, skins = HumanSkins, hairs = NaturalHair, eyeColors = NaturalEyes, femHeight = .96f },
            [RaceId.Gnome] = new RaceLook { height = 1.00f, headRatio = 5.0f, legFrac = .42f, width = 1.08f, ears = EarType.Gnome, skins = HumanSkins, hairs = NaturalHair, eyeColors = new[] { C("#3b6e9e"), C("#4d9a7a"), C("#8a5ab0"), C("#c87a30") } },
            [RaceId.Tiefling] = new RaceLook { height = 1.78f, headRatio = 7.4f, legFrac = .51f, width = 1f, ears = EarType.HalfElf, horns = true, tailDefault = true, skins = new[] { C("#b04438"), C("#8e2e2e"), C("#6e3a6e"), C("#4a3a78"), C("#3a5a8a"), C("#d08a7a"), C("#c89a88"), C("#7a2a3a") }, hairs = new[] { C("#1b1411"), C("#2a1a2e"), C("#4a1a2a"), C("#1e2a4a"), C("#d8d0c8"), C("#8a2a2a") }, eyeColors = new[] { C("#e8c040"), C("#d83a2a"), C("#e8e8e8"), C("#202020"), C("#a0e040") } },
            [RaceId.Githyanki] = new RaceLook { height = 1.86f, headRatio = 7.8f, legFrac = .53f, width = .86f, ears = EarType.Githyanki, skins = new[] { C("#b3b56a"), C("#9ea55a"), C("#c2b87a"), C("#8a9a58"), C("#a8a878"), C("#cfc38e") }, hairs = new[] { C("#1b1411"), C("#3a261a"), C("#6a2a1a"), C("#2a3a2a"), C("#8a7a5a") }, eyeColors = new[] { C("#d8a030"), C("#303030"), C("#c86a20") } },
            [RaceId.Dragonborn] = new RaceLook { height = 1.98f, headRatio = 7.0f, legFrac = .5f, width = 1.22f, ears = EarType.None, dragon = true, skins = new[] { C("#2a2a30") }, hairs = new[] { C("#2a2020") }, eyeColors = new[] { C("#e8c040"), C("#e87a30"), C("#e8e8d0"), C("#60c8e8") } },
            [RaceId.Firbolg] = new RaceLook { height = 2.2f, headRatio = 7.3f, legFrac = .5f, width = 1.22f, ears = EarType.Firbolg, skins = new[] { C("#8f9db0"), C("#7f90a6"), C("#9aa4b6"), C("#a8a6b4"), C("#b6a4a6"), C("#8a9a94") }, hairs = new[] { C("#e8e2d6"), C("#bdb6aa"), C("#8a8078"), C("#5b4a3a"), C("#b48a55"), C("#3a2d24") }, eyeColors = new[] { C("#6aa0c8"), C("#8ab070"), C("#c8a040"), C("#7b8a91") } },
        };

        public static Color DragonColor(SubraceId s)
        {
            switch (s)
            {
                case SubraceId.Black: return C("#2b2a2e");
                case SubraceId.Blue: return C("#2f5f9e");
                case SubraceId.Brass: return C("#b08a3e");
                case SubraceId.Bronze: return C("#8a5e2e");
                case SubraceId.Copper: return C("#b0603a");
                case SubraceId.Gold: return C("#c9a23a");
                case SubraceId.Green: return C("#3a7040");
                case SubraceId.Red: return C("#9e2a22");
                case SubraceId.Silver: return C("#a8b0b8");
                case SubraceId.White: return C("#dde2e6");
                default: return C("#9e2a22");
            }
        }

        public static readonly string[] HairStyleNames = { "Shaved", "Short Crop", "Long Loose", "Ponytail", "Topknot", "Braids", "Mohawk", "Bun", "Shoulder Length", "Wild Mane", "Tousled" };
        public static readonly string[] BeardNames = { "None", "Stubble", "Short Beard", "Full Beard", "Long Braided", "Moustache" };
        public static readonly string[] HornNames = { "Ram Curl", "Swept Back", "Tall Spires", "Short Nubs", "Crown of Spikes" };
        public static readonly string[] FaceNames = { "Angular", "Round", "Long", "Heavy-browed" };

        public static Color[] SkinPalette(RaceId r, SubraceId s)
        {
            if (r == RaceId.Dragonborn)
            {
                var b = DragonColor(s);
                return new[] { b, b * 0.85f + Color.black * 0.15f, Color.Lerp(b, Color.white, 0.15f), Color.Lerp(b, Color.black, 0.3f) };
            }
            if (s == SubraceId.Duergar) return new[] { C("#6e6e72"), C("#5a5a60"), C("#7e7a78"), C("#4a4a50") };
            if (s == SubraceId.DeepGnome) return new[] { C("#6e6a6e"), C("#5a5660"), C("#7a7478"), C("#8a8288") };
            if (s == SubraceId.HalfElfDrow) return new[] { C("#6a5e70"), C("#7a6a78"), C("#8a7a80"), C("#b09a90"), C("#c8a890") };
            return Looks[r].skins;
        }

        public static Appearance Randomize(RaceId r, SubraceId s, int bodyType, System.Random rnd)
        {
            var look = Looks[r];
            var sk = SkinPalette(r, s);
            var a = new Appearance
            {
                race = r, subrace = s, bodyType = bodyType,
                strong = rnd.NextDouble() < 0.25,
                heightAdj = (float)(rnd.NextDouble() * 1.2 - 0.6),
                skin = sk[rnd.Next(sk.Length)],
                hair = look.hairs[rnd.Next(look.hairs.Length)],
                eyes = look.eyeColors[rnd.Next(look.eyeColors.Length)],
                hairStyle = r == RaceId.Dragonborn ? 0 : 1 + rnd.Next(HairStyleNames.Length - 1),
                beardStyle = bodyType == 0 && r != RaceId.Dragonborn && r != RaceId.Elf && r != RaceId.Drow && r != RaceId.Githyanki ? rnd.Next(BeardNames.Length) : 0,
                hornStyle = rnd.Next(HornNames.Length),
                faceShape = rnd.Next(FaceNames.Length),
                tail = look.tailDefault,
            };
            if (r == RaceId.Dwarf && bodyType == 0) a.beardStyle = 3 + rnd.Next(2);
            if (s == SubraceId.Duergar) { a.hairStyle = 0; a.hair = C("#5a5a5a"); }
            if (r == RaceId.Tiefling) a.horn = Color.Lerp(C("#1a1414"), C("#4a3a30"), (float)rnd.NextDouble());
            if (r == RaceId.Dragonborn) a.horn = Color.Lerp(C("#d8ccb0"), C("#3a3430"), (float)rnd.NextDouble());
            return a;
        }
    }
}
