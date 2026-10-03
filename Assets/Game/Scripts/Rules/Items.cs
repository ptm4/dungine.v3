using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dungine.Rules
{
    public enum ItemKind { Weapon, Armor, Shield, Helmet, Cloak, Gloves, Boots, Amulet, Ring, Potion, Scroll, Throwable, Food, Misc, Valuable, Key, Quest, Book, Ammo }
    public enum Slot { None, MainHand, OffHand, Ranged, Armor, Helmet, Cloak, Gloves, Boots, Amulet, Ring1, Ring2 }
    public enum Rarity { Common, Uncommon, Rare, VeryRare, Legendary, Story }

    public class ItemDef
    {
        public string id, name, desc;
        public ItemKind kind;
        public float weight;
        public int value;
        public Rarity rarity;
        public string icon;                   // icon painter id
        public Color tint = Color.white;
        // weapon
        public WeaponCat wcat; public string dmg; public DamageType dtype; public WeaponProps props; public string versatile;
        public float rangeM = 1.5f, longRangeM; public WeaponVisual visual;
        public int magic;                      // +N to hit and damage
        public string extraDmg; public DamageType extraType;
        public string[] weaponActions = new string[0];
        public string family;                  // proficiency key e.g. "longsword"
        // armor
        public ArmorCat acat; public int baseAC; public int dexCap = 99; public bool stealthDis; public ArmorVisual avisual;
        // accessories
        public int acBonus, saveBonus, attackBonus;
        public DamageType[] resists = new DamageType[0];
        public Ability abilitySetStat = Ability.STR; public int abilitySetTo;
        public string grantsAction;
        public string useAction;               // consumables: action id executed on use
        public bool stackable;
        public string lore;                    // readable text for books/notes

        public bool IsWeapon => kind == ItemKind.Weapon;
        public bool IsRanged => (props & WeaponProps.Ranged) != 0;
        public bool Has(WeaponProps p) => (props & p) != 0;
        public bool Equippable => kind == ItemKind.Weapon || kind == ItemKind.Armor || kind == ItemKind.Shield || kind == ItemKind.Helmet || kind == ItemKind.Cloak || kind == ItemKind.Gloves || kind == ItemKind.Boots || kind == ItemKind.Amulet || kind == ItemKind.Ring;

        public string DamageString(int mod)
        {
            var d = DiceExpr.Parse(dmg).WithBonus(mod + magic);
            string s = d.ToString() + " " + dtype;
            if (!string.IsNullOrEmpty(extraDmg)) s += " + " + extraDmg + " " + extraType;
            return s;
        }
    }

    [Serializable]
    public class ItemStack
    {
        public string id;
        public int count = 1;
        public int uid;
        [NonSerialized] ItemDef _def;
        public ItemDef Def => _def ??= Items.Get(id);
        public ItemStack() { }
        public ItemStack(string id, int count = 1) { this.id = id; this.count = count; uid = Items.NextUid(); }
    }

    public static class Items
    {
        public static readonly Dictionary<string, ItemDef> All = new Dictionary<string, ItemDef>();
        static int uid = 1000;
        public static int NextUid() => ++uid;
        public static void BumpUid(int atLeast) { if (uid < atLeast) uid = atLeast; }

        public static ItemDef Get(string id) => id != null && All.TryGetValue(id, out var d) ? d : null;

        static ItemDef W(string id, string name, WeaponCat cat, string dmg, DamageType t, WeaponProps p, float w, int val, WeaponVisual vis, string versatile = null, float range = 1.5f, float longRange = 0, params string[] wa)
        {
            var d = new ItemDef
            {
                id = id, name = name, kind = ItemKind.Weapon, wcat = cat, dmg = dmg, dtype = t, props = p, weight = w, value = val, visual = vis,
                versatile = versatile, rangeM = (p & WeaponProps.Reach) != 0 ? 3f : range, longRangeM = longRange, weaponActions = wa, family = id, icon = IconFor(vis)
            };
            All[id] = d; return d;
        }

        static string IconFor(WeaponVisual v)
        {
            switch (v)
            {
                case WeaponVisual.Shortbow: case WeaponVisual.Longbow: return "bow";
                case WeaponVisual.LightCrossbow: case WeaponVisual.HeavyCrossbow: case WeaponVisual.HandCrossbow: return "crossbow";
                case WeaponVisual.Handaxe: case WeaponVisual.Battleaxe: case WeaponVisual.Greataxe: return "axe";
                case WeaponVisual.Mace: case WeaponVisual.Morningstar: case WeaponVisual.Warhammer: case WeaponVisual.Maul: case WeaponVisual.Club: case WeaponVisual.Greatclub: case WeaponVisual.Flail: return "mace";
                case WeaponVisual.Quarterstaff: case WeaponVisual.Staff: return "staff";
                case WeaponVisual.Spear: case WeaponVisual.Glaive: case WeaponVisual.Halberd: case WeaponVisual.Trident: case WeaponVisual.Javelin: return "spear";
                case WeaponVisual.Dagger: case WeaponVisual.Sickle: return "dagger";
                default: return "sword";
            }
        }

        static ItemDef A(string id, string name, ArmorCat cat, int ac, int dexCap, bool stealth, float w, int val, ArmorVisual vis)
        {
            var d = new ItemDef { id = id, name = name, kind = cat == ArmorCat.Shield ? ItemKind.Shield : ItemKind.Armor, acat = cat, baseAC = ac, dexCap = dexCap, stealthDis = stealth, weight = w, value = val, avisual = vis, icon = cat == ArmorCat.Shield ? "shield" : "armor" };
            All[id] = d; return d;
        }

        static ItemDef I(string id, string name, ItemKind k, string icon, float w, int val, string desc, Rarity r = Rarity.Common)
        {
            var d = new ItemDef { id = id, name = name, kind = k, icon = icon, weight = w, value = val, desc = desc, rarity = r, stackable = k == ItemKind.Potion || k == ItemKind.Scroll || k == ItemKind.Throwable || k == ItemKind.Food || k == ItemKind.Valuable || k == ItemKind.Ammo || k == ItemKind.Misc };
            All[id] = d; return d;
        }

        const WeaponProps F = WeaponProps.Finesse, L = WeaponProps.Light, H = WeaponProps.Heavy, T2 = WeaponProps.TwoHanded, V = WeaponProps.Versatile, R = WeaponProps.Reach, Th = WeaponProps.Thrown, Rg = WeaponProps.Ranged, Am = WeaponProps.Ammunition, Ld = WeaponProps.Loading, Mk = WeaponProps.Monk;

        static Items()
        {
            // ---------- simple melee ----------
            W("club", "Club", WeaponCat.Simple, "1d4", DamageType.Bludgeoning, L | Mk, 1, 1, WeaponVisual.Club, null, 1.5f, 0, "topple");
            W("dagger", "Dagger", WeaponCat.Simple, "1d4", DamageType.Piercing, F | L | Th | Mk, 0.5f, 2, WeaponVisual.Dagger, null, 1.5f, 0, "piercing_strike");
            W("greatclub", "Greatclub", WeaponCat.Simple, "1d8", DamageType.Bludgeoning, T2, 5, 1, WeaponVisual.Greatclub, null, 1.5f, 0, "topple", "concussive_smash");
            W("handaxe", "Handaxe", WeaponCat.Simple, "1d6", DamageType.Slashing, L | Th | Mk, 1, 5, WeaponVisual.Handaxe, null, 1.5f, 0, "lacerate");
            W("javelin", "Javelin", WeaponCat.Simple, "1d6", DamageType.Piercing, Th | Mk, 1, 1, WeaponVisual.Javelin, null, 1.5f, 0, "piercing_strike");
            W("light_hammer", "Light Hammer", WeaponCat.Simple, "1d4", DamageType.Bludgeoning, L | Th | Mk, 1, 2, WeaponVisual.Warhammer, null, 1.5f, 0, "concussive_smash");
            W("mace", "Mace", WeaponCat.Simple, "1d6", DamageType.Bludgeoning, Mk, 2, 5, WeaponVisual.Mace, null, 1.5f, 0, "concussive_smash");
            W("quarterstaff", "Quarterstaff", WeaponCat.Simple, "1d6", DamageType.Bludgeoning, V | Mk, 2, 2, WeaponVisual.Quarterstaff, "1d8", 1.5f, 0, "topple");
            W("sickle", "Sickle", WeaponCat.Simple, "1d4", DamageType.Slashing, L | Mk, 1, 1, WeaponVisual.Sickle, null, 1.5f, 0, "lacerate");
            W("spear", "Spear", WeaponCat.Simple, "1d6", DamageType.Piercing, Th | V | Mk, 1.5f, 1, WeaponVisual.Spear, "1d8", 1.5f, 0, "piercing_strike");
            // ---------- simple ranged ----------
            W("light_crossbow", "Light Crossbow", WeaponCat.Simple, "1d8", DamageType.Piercing, Rg | Am | Ld | T2, 2.5f, 25, WeaponVisual.LightCrossbow, null, 24f, 96f, "piercing_shot");
            W("shortbow", "Shortbow", WeaponCat.Simple, "1d6", DamageType.Piercing, Rg | Am | T2, 1, 25, WeaponVisual.Shortbow, null, 24f, 96f, "hamstring_shot", "mobile_shot");
            // ---------- martial melee ----------
            W("battleaxe", "Battleaxe", WeaponCat.Martial, "1d8", DamageType.Slashing, V, 2, 10, WeaponVisual.Battleaxe, "1d10", 1.5f, 0, "lacerate", "cleave");
            W("flail", "Flail", WeaponCat.Martial, "1d8", DamageType.Bludgeoning, WeaponProps.None, 1, 10, WeaponVisual.Flail, null, 1.5f, 0, "pommel_strike");
            W("glaive", "Glaive", WeaponCat.Martial, "1d10", DamageType.Slashing, H | R | T2, 3, 20, WeaponVisual.Glaive, null, 3f, 0, "cleave", "lacerate");
            W("greataxe", "Greataxe", WeaponCat.Martial, "1d12", DamageType.Slashing, H | T2, 3.5f, 30, WeaponVisual.Greataxe, null, 1.5f, 0, "cleave", "lacerate");
            W("greatsword", "Greatsword", WeaponCat.Martial, "2d6", DamageType.Slashing, H | T2, 3, 50, WeaponVisual.Greatsword, null, 1.5f, 0, "cleave", "pommel_strike");
            W("halberd", "Halberd", WeaponCat.Martial, "1d10", DamageType.Slashing, H | R | T2, 3, 20, WeaponVisual.Halberd, null, 3f, 0, "cleave", "topple");
            W("longsword", "Longsword", WeaponCat.Martial, "1d8", DamageType.Slashing, V, 1.5f, 15, WeaponVisual.Longsword, "1d10", 1.5f, 0, "pommel_strike", "lacerate");
            W("maul", "Maul", WeaponCat.Martial, "2d6", DamageType.Bludgeoning, H | T2, 5, 10, WeaponVisual.Maul, null, 1.5f, 0, "topple", "concussive_smash");
            W("morningstar", "Morningstar", WeaponCat.Martial, "1d8", DamageType.Piercing, WeaponProps.None, 2, 15, WeaponVisual.Morningstar, null, 1.5f, 0, "concussive_smash");
            W("rapier", "Rapier", WeaponCat.Martial, "1d8", DamageType.Piercing, F, 1, 25, WeaponVisual.Rapier, null, 1.5f, 0, "piercing_strike", "flourish");
            W("scimitar", "Scimitar", WeaponCat.Martial, "1d6", DamageType.Slashing, F | L, 1, 25, WeaponVisual.Scimitar, null, 1.5f, 0, "lacerate", "flourish");
            W("shortsword", "Shortsword", WeaponCat.Martial, "1d6", DamageType.Piercing, F | L, 1, 10, WeaponVisual.Shortsword, null, 1.5f, 0, "piercing_strike", "flourish");
            W("trident", "Trident", WeaponCat.Martial, "1d6", DamageType.Piercing, Th | V, 2, 5, WeaponVisual.Trident, "1d8", 1.5f, 0, "piercing_strike");
            W("warhammer", "Warhammer", WeaponCat.Martial, "1d8", DamageType.Bludgeoning, V, 1, 15, WeaponVisual.Warhammer, "1d10", 1.5f, 0, "concussive_smash", "topple");
            // ---------- martial ranged ----------
            W("hand_crossbow", "Hand Crossbow", WeaponCat.Martial, "1d6", DamageType.Piercing, Rg | Am | L | Ld, 1.5f, 75, WeaponVisual.HandCrossbow, null, 18f, 72f, "piercing_shot");
            W("heavy_crossbow", "Heavy Crossbow", WeaponCat.Martial, "1d10", DamageType.Piercing, Rg | Am | H | Ld | T2, 4.5f, 50, WeaponVisual.HeavyCrossbow, null, 30f, 120f, "piercing_shot", "brace");
            W("longbow", "Longbow", WeaponCat.Martial, "1d8", DamageType.Piercing, Rg | Am | H | T2, 1, 50, WeaponVisual.Longbow, null, 36f, 180f, "hamstring_shot", "mobile_shot");
            // unarmed
            W("unarmed", "Unarmed Strike", WeaponCat.Natural, "1", DamageType.Bludgeoning, Mk, 0, 0, WeaponVisual.None);

            // ---------- armour ----------
            A("padded", "Padded Armour", ArmorCat.Light, 11, 99, true, 4, 5, ArmorVisual.Cloth);
            A("leather", "Leather Armour", ArmorCat.Light, 11, 99, false, 5, 10, ArmorVisual.Leather);
            A("studded", "Studded Leather", ArmorCat.Light, 12, 99, false, 6.5f, 45, ArmorVisual.Leather);
            A("hide", "Hide Armour", ArmorCat.Medium, 12, 2, false, 6, 10, ArmorVisual.Hide);
            A("chain_shirt", "Chain Shirt", ArmorCat.Medium, 13, 2, false, 10, 50, ArmorVisual.Chain);
            A("scale_mail", "Scale Mail", ArmorCat.Medium, 14, 2, true, 22, 50, ArmorVisual.Scale);
            A("breastplate", "Breastplate", ArmorCat.Medium, 14, 2, false, 10, 400, ArmorVisual.Plate);
            A("half_plate", "Half Plate", ArmorCat.Medium, 15, 2, true, 20, 750, ArmorVisual.Plate);
            A("ring_mail", "Ring Mail", ArmorCat.Heavy, 14, 0, true, 20, 30, ArmorVisual.Chain);
            A("chain_mail", "Chain Mail", ArmorCat.Heavy, 16, 0, true, 27, 75, ArmorVisual.Chain);
            A("splint", "Splint Armour", ArmorCat.Heavy, 17, 0, true, 30, 200, ArmorVisual.Plate);
            A("plate", "Plate Armour", ArmorCat.Heavy, 18, 0, true, 32, 1500, ArmorVisual.Plate);
            A("shield", "Shield", ArmorCat.Shield, 2, 99, false, 3, 10, ArmorVisual.None);
            var robe = new ItemDef { id = "robe", name = "Traveller's Robe", kind = ItemKind.Armor, acat = ArmorCat.None, baseAC = 10, avisual = ArmorVisual.Robe, weight = 2, value = 5, icon = "robe", desc = "Layered wool robes, patched from long roads." }; All[robe.id] = robe;
            var clothes = new ItemDef { id = "clothes", name = "Common Clothes", kind = ItemKind.Armor, acat = ArmorCat.None, baseAC = 10, avisual = ArmorVisual.Cloth, weight = 1.5f, value = 1, icon = "robe", desc = "Plain, practical clothes." }; All[clothes.id] = clothes;
            var wraps = new ItemDef { id = "wraps", name = "Monk's Wraps", kind = ItemKind.Armor, acat = ArmorCat.None, baseAC = 10, avisual = ArmorVisual.None, weight = 1, value = 2, icon = "robe", desc = "Light cloth wraps that never slow a fist." }; All[wraps.id] = wraps;

            // ---------- consumables ----------
            var p1 = I("potion_healing", "Potion of Healing", ItemKind.Potion, "potion", 0.5f, 50, "Drink to regain 2d4+2 hit points. Drinking is a bonus action; it can also be thrown to heal an ally where it lands.");
            p1.useAction = "use_potion_healing"; p1.tint = new Color(.9f, .2f, .25f);
            var p2 = I("potion_greater_healing", "Potion of Greater Healing", ItemKind.Potion, "potion", 0.5f, 150, "Drink to regain 4d4+4 hit points.", Rarity.Uncommon);
            p2.useAction = "use_potion_greater"; p2.tint = new Color(1f, .35f, .5f);
            var el = I("elixir_vigilance", "Elixir of Vigilance", ItemKind.Potion, "potion", 0.5f, 90, "Until your next long rest, +5 to initiative and you cannot be surprised.", Rarity.Uncommon);
            el.useAction = "use_elixir_vigilance"; el.tint = new Color(.5f, .8f, 1f);
            var hw = I("holy_water", "Flask of Holy Water", ItemKind.Throwable, "flask", 0.5f, 25, "Throw at a fiend or undead: 2d6 radiant damage in a small splash.");
            hw.useAction = "throw_holy_water"; hw.tint = new Color(.8f, .9f, 1f);
            var af = I("alchemist_fire", "Alchemist's Fire", ItemKind.Throwable, "flask", 0.5f, 50, "Throw to burst into flame: 1d4 fire damage and sets targets Burning.");
            af.useAction = "throw_alchemist_fire"; af.tint = new Color(1f, .5f, .1f);
            var sc = I("scroll_revivify", "Scroll of Revivify", ItemKind.Scroll, "scroll", 0, 300, "Returns a companion who died within the last minute to life with 1 hit point.", Rarity.Rare);
            sc.useAction = "revivify_scroll";
            var sc2 = I("scroll_bless", "Scroll of Bless", ItemKind.Scroll, "scroll", 0, 60, "Casts Bless without spending a spell slot.", Rarity.Uncommon);
            sc2.useAction = "bless_scroll";
            var sc3 = I("scroll_magic_missile", "Scroll of Magic Missile", ItemKind.Scroll, "scroll", 0, 60, "Casts Magic Missile at 1st level.", Rarity.Uncommon);
            sc3.useAction = "magic_missile_scroll";
            var garlic = I("garlic", "Garlic Braid", ItemKind.Food, "food", 0.2f, 1, "Barovians hang these above their doors. Eating one restores 1 hit point.");
            garlic.useAction = "eat_food";
            var bread = I("bread", "Black Bread", ItemKind.Food, "food", 0.3f, 1, "Dense, sour and filling.");
            bread.useAction = "eat_food";
            var wine = I("wine", "Purple Grapemash No. 3", ItemKind.Food, "potion", 1f, 2, "A thin, bitter wine — the only kind left in the Village of Barovia.");
            wine.useAction = "eat_food"; wine.tint = new Color(.5f, .1f, .3f);
            var cand = I("candle", "Tallow Candle", ItemKind.Misc, "candle", 0.1f, 1, "Smells faintly of mutton.");
            var torch = I("torch", "Torch", ItemKind.Misc, "torch", 0.5f, 1, "Pitch-soaked rag on a stick.");

            // ---------- valuables & misc ----------
            I("silver_locket", "Tarnished Silver Locket", ItemKind.Valuable, "gem", 0.1f, 25, "A locket with a miniature portrait worn beyond recognition.");
            I("gold_ring", "Gold Signet", ItemKind.Valuable, "ring", 0.05f, 40, "A heavy ring bearing the arms of a family long dead.");
            I("candlesticks", "Silver Candlesticks", ItemKind.Valuable, "candle", 2f, 30, "A pair of heavy silver candlesticks.");
            I("gemstone", "Bloodstone", ItemKind.Valuable, "gem", 0.02f, 50, "A dark green stone flecked with red.");
            I("wolf_pelt", "Wolf Pelt", ItemKind.Valuable, "pelt", 3f, 8, "A coarse grey pelt. Someone in the village might pay for it.");
            I("bone_dice", "Bone Dice", ItemKind.Valuable, "dice", 0.05f, 3, "Carved from something that was probably not a cow.");

            // ---------- keys & quest ----------
            I("key_deathhouse_cellar", "Small Rusted Key", ItemKind.Key, "key", 0.1f, 0, "Found in the Dursts' wardrobe. It fits the attic door.");
            I("key_mansion", "Burgomaster's Key", ItemKind.Key, "key", 0.1f, 0, "Opens the gate of the burgomaster's manor.");
            var letter = I("letter_kolyan", "Letter from the Burgomaster", ItemKind.Quest, "letter", 0, 0, "A plea for help sealed with red wax, signed Kolyan Indirovich, Burgomaster.");
            letter.lore = "To whatever brave souls this finds,\n\nI write from a village the world has forgotten, beneath a castle that has not. Something walks our lanes at night that no lock will bar and no prayer will turn. My adopted daughter, Ireena, bears its marks upon her throat, and each night it calls for her again.\n\nI am old and my heart fails me. My son is brave but he is only one man. Our priest has troubles of his own.\n\nWhat coin and goods remain to this house are yours if you will stand between her and the dark. Follow the road east through the forest until the mists part. Do not stop for anyone on the road.\n\nCome quickly. I do not know how many nights we have.\n\n— Kolyan Indirovich, Burgomaster of Barovia";
            var journal = I("dh_journal", "Walter's Notes", ItemKind.Book, "book", 0.5f, 0, "A sheaf of notes found in the Death House library.");
            journal.lore = "Our patrons in the dark ask much and give much. Elisabeth says the one below grows hungry. We have sent the servants away. We shall not send the children away. Not yet.\n\nThe stair behind the nursery wall goes down farther than the house should allow. Every night I hear it breathing.";
            var deed = I("dh_deed", "Deed to the Townhouse", ItemKind.Quest, "scroll", 0, 0, "A deed naming Gustav and Elisabeth Durst as owners of a townhouse in the Village of Barovia.");
            deed.lore = "Granted by the Burgomaster of Barovia to Gustav and Elisabeth Durst, in perpetuity, a townhouse upon the Old Svalich Road — with the curious rider that the house shall never be sold, nor let, nor left empty.";
            var cn = I("courier_note", "Bloodstained Waybill", ItemKind.Book, "letter", 0, 0, "A waybill from the pocket of a dead wagon driver.");
            cn.lore = "Waybill — one wagon, two horses, goods for the mercantile of B. Bildrath, Village of Barovia.\n\n6 casks lamp oil. 2 bolts grey wool. 1 crate candles (tallow). 1 box nails. Salt.\n\nOn the back, in pencil, pressed hard: They follow the wagon but never come close. Grey ones. Five, six. The horses won't stop shaking. If I don't reach the village by dark\n\nThe rest is torn away.";
            var sp = I("sun_pendant", "Ireena's Sun Pendant", ItemKind.Quest, "amulet", 0, 0, "A small brass sun on a leather cord. Ireena pressed it into your hand for luck.");
            sp.lore = "Brass, cheaply made, polished smooth by a thumb that has worried at it through a great many nights.";
            var kd = I("key_undercroft", "Iron Undercroft Key", ItemKind.Key, "key", 0.1f, 0, "Father Donavich's key to the cellar beneath his church.");
            var bones = I("children_bones", "Small Bones", ItemKind.Quest, "skull", 1f, 0, "The remains of two children, found in the depths beneath the Durst house. They deserve a proper rest.");
            var ring = I("ring_protection", "Ring of the Grey Watch", ItemKind.Ring, "ring", 0.05f, 400, "+1 to Armour Class and saving throws. Worn by a Barovian watch-captain who never came home.", Rarity.Rare);
            ring.acBonus = 1; ring.saveBonus = 1;
            var amulet = I("amulet_morninglord", "Sunstone Amulet", ItemKind.Amulet, "amulet", 0.1f, 300, "A small disc of amber that is always warm. Grants resistance to necrotic damage.", Rarity.Rare);
            amulet.resists = new[] { DamageType.Necrotic };
            var cloak = I("cloak_mists", "Cloak of the Mists", ItemKind.Cloak, "cloak", 1f, 350, "Grey wool that frays into fog at the edges. +1 AC; grants the Misty Step spell once per long rest.", Rarity.Rare);
            cloak.acBonus = 1; cloak.grantsAction = "misty_step";
            var boots = I("boots_striding", "Boots of the Svalich Road", ItemKind.Boots, "boots", 1f, 200, "Your walking speed increases by 1.5m.", Rarity.Uncommon);
            var gloves = I("gloves_thievery", "Gloves of Deft Fingers", ItemKind.Gloves, "gloves", 0.2f, 180, "+2 to Sleight of Hand checks and the ability to pick locks as if proficient.", Rarity.Uncommon);
            var helm = I("helm_barovian", "Barovian Guard Helm", ItemKind.Helmet, "helm", 1.5f, 40, "Dented iron, rusted at the rivets.", Rarity.Common);
            // magic weapons
            var ls = W("dawnwarden", "Dawnwarden", WeaponCat.Martial, "1d8", DamageType.Slashing, V, 1.5f, 800, WeaponVisual.Longsword, "1d10", 1.5f, 0, "pommel_strike", "lacerate");
            ls.magic = 1; ls.extraDmg = "1d4"; ls.extraType = DamageType.Radiant; ls.rarity = Rarity.Rare; ls.family = "longsword"; ls.desc = "A longsword whose steel holds a sliver of a sunrise nobody in Barovia has seen in centuries. +1; deals an extra 1d4 radiant damage.";
            var dg = W("gravefang", "Gravefang", WeaponCat.Simple, "1d4", DamageType.Piercing, F | L | Th, 0.5f, 250, WeaponVisual.Dagger, null, 1.5f, 0, "piercing_strike");
            dg.magic = 1; dg.rarity = Rarity.Uncommon; dg.family = "dagger"; dg.desc = "A cultist's ritual dagger, +1. The hilt is carved like a ribcage.";
            var st = W("staff_ashes", "Staff of Cold Ashes", WeaponCat.Simple, "1d6", DamageType.Bludgeoning, V, 2, 400, WeaponVisual.Staff, "1d8", 1.5f, 0, "topple");
            st.magic = 1; st.rarity = Rarity.Rare; st.family = "quarterstaff"; st.desc = "A blackened staff. +1 to spell attack rolls and spell save DC while held.";
            var mc = W("mace_morninglord", "Mace of the Morninglord", WeaponCat.Simple, "1d6", DamageType.Bludgeoning, WeaponProps.None, 2, 500, WeaponVisual.Mace, null, 1.5f, 0, "concussive_smash");
            mc.magic = 1; mc.extraDmg = "1d6"; mc.extraType = DamageType.Radiant; mc.rarity = Rarity.Rare; mc.family = "mace"; mc.desc = "Blessed by Father Donavich's order long ago. +1; an extra 1d6 radiant damage.";
            var bw = W("bow_vistani", "Vistani Hunting Bow", WeaponCat.Simple, "1d6", DamageType.Piercing, Rg | Am | T2, 1, 300, WeaponVisual.Shortbow, null, 24f, 96f, "hamstring_shot", "mobile_shot");
            bw.magic = 1; bw.rarity = Rarity.Uncommon; bw.family = "shortbow"; bw.desc = "Painted with bright Vistani patterns. +1.";
        }

        // ---------- proficiency helpers ----------
        public static readonly string[] SimpleFamilies = { "club", "dagger", "greatclub", "handaxe", "javelin", "light_hammer", "mace", "quarterstaff", "sickle", "spear", "light_crossbow", "shortbow", "unarmed" };
    }
}
