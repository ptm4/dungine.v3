using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;

namespace Dungine
{
    /// <summary>How the named people of Barovia look. All original designs.</summary>
    public static partial class Campaign
    {
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        public static Appearance IsmarkLook => new Appearance { race = RaceId.Human, bodyType = 0, strong = true, heightAdj = 0.8f, skin = C("#d9b39a"), hair = C("#2a1a12"), hairStyle = 1, beardStyle = 2, eyes = C("#4d6a7e"), cloth1 = C("#3c4a3a"), cloth2 = C("#2a2420") };
        public static GearLook IsmarkGear => new GearLook { armor = ArmorVisual.Leather, main = WeaponVisual.Longsword };

        public static Appearance IreenaLook => new Appearance { race = RaceId.Human, bodyType = 1, heightAdj = 0.2f, skin = C("#e8cbb6"), hair = C("#8e2f1c"), hairStyle = 8, eyes = C("#4d7d4a"), cloth1 = C("#5a1e22"), cloth2 = C("#2a2224") };
        public static GearLook IreenaGear => new GearLook { armor = ArmorVisual.Cloth, main = WeaponVisual.Rapier };

        public static Appearance ArikLook => new Appearance { race = RaceId.Human, bodyType = 0, bulk = 1.25f, skin = C("#c9a489"), hair = C("#6b5a4a"), hairStyle = 0, beardStyle = 0, eyes = C("#6b4b2e"), cloth1 = C("#5a4a3a"), cloth2 = C("#e0d6c4"), age = 0.5f };
        public static Appearance BildrathLook => new Appearance { race = RaceId.Human, bodyType = 0, bulk = 1.35f, skin = C("#d6a684"), hair = C("#9a9591"), hairStyle = 0, beardStyle = 3, eyes = C("#3b6e9e"), cloth1 = C("#4a3a52"), cloth2 = C("#2a2430"), age = 0.8f };
        public static Appearance ParriwimpleLook => new Appearance { race = RaceId.Human, bodyType = 0, strong = true, heightAdj = 1f, bulk = 1.15f, skin = C("#e3bba2"), hair = C("#b48a55"), hairStyle = 3, eyes = C("#3b6e9e"), cloth1 = C("#6a5a3a"), cloth2 = C("#3a3020") };
        public static Appearance DonavichLook => new Appearance { race = RaceId.Human, bodyType = 0, gaunt = true, skin = C("#d6b49c"), hair = C("#d8d2c6"), hairStyle = 1, beardStyle = 3, eyes = C("#4d6a7e"), cloth1 = C("#2e2a28"), cloth2 = C("#c9a45a"), age = 0.9f };
        public static Appearance VistaniLook(int i) => new Appearance
        {
            race = RaceId.Human, bodyType = 1, skin = new[] { C("#ae7a56"), C("#925f41"), C("#c4916c") }[i % 3], hair = new[] { C("#1b1411"), C("#3a261a"), C("#1b1411") }[i % 3], hairStyle = new[] { 8, 2, 9 }[i % 3],
            eyes = C("#6b4b2e"), cloth1 = new[] { C("#7a1e2a"), C("#1e4a6a"), C("#6a5a1a") }[i % 3], cloth2 = new[] { C("#c9a45a"), C("#8a2a3a"), C("#2a4a3a") }[i % 3]
        };
        public static Appearance MaryLook => new Appearance { race = RaceId.Human, bodyType = 1, gaunt = true, hunched = true, skin = C("#d6b8a4"), hair = C("#9a9591"), hairStyle = 9, eyes = C("#7b8a91"), cloth1 = C("#4a4442"), cloth2 = C("#2a2624"), age = 0.7f };
        public static Appearance KolyanLook => new Appearance { race = RaceId.Human, bodyType = 0, bulk = 1.15f, skin = C("#bfb2aa"), hair = C("#9a9591"), hairStyle = 0, beardStyle = 3, cloth1 = C("#2a2a3a"), cloth2 = C("#6a5a3a"), age = 1f };
    }
}
