using System;

namespace Dungine.Rules
{
    public enum Ability { STR, DEX, CON, INT, WIS, CHA }

    public enum Skill
    {
        Acrobatics, AnimalHandling, Arcana, Athletics, Deception, History, Insight, Intimidation, Investigation,
        Medicine, Nature, Perception, Performance, Persuasion, Religion, SleightOfHand, Stealth, Survival
    }

    public enum DamageType { Slashing, Piercing, Bludgeoning, Fire, Cold, Lightning, Thunder, Acid, Poison, Necrotic, Radiant, Force, Psychic, None }

    public enum RaceId { Human, Elf, Drow, HalfElf, HalfOrc, Halfling, Dwarf, Gnome, Tiefling, Githyanki, Dragonborn, Firbolg }

    public enum SubraceId
    {
        None,
        HighElf, WoodElf,
        LolthSworn, Seldarine,
        HalfElfHigh, HalfElfWood, HalfElfDrow,
        Lightfoot, Strongheart,
        GoldDwarf, ShieldDwarf, Duergar,
        ForestGnome, DeepGnome, RockGnome,
        Asmodeus, Mephistopheles, Zariel,
        Black, Blue, Brass, Bronze, Copper, Gold, Green, Red, Silver, White
    }

    public enum ClassId { Barbarian, Bard, Cleric, Druid, Fighter, Monk, Paladin, Ranger, Rogue, Sorcerer, Warlock, Wizard }

    public enum BackgroundId { Acolyte, Charlatan, Criminal, Entertainer, FolkHero, GuildArtisan, Noble, Outlander, Sage, Soldier, Urchin, HauntedOne }

    public enum SizeCat { Tiny, Small, Medium, Large, Huge }

    public enum Faction { Party, Hostile, Neutral, Ally }

    public enum CreatureType { Humanoid, Beast, Undead, Fiend, Fey, Construct, Plant, Monstrosity, Aberration, Dragon, Celestial }

    public enum ActionCost { Action, BonusAction, Reaction, Free }

    public enum School { None, Abjuration, Conjuration, Divination, Enchantment, Evocation, Illusion, Necromancy, Transmutation }

    public enum Cond
    {
        Prone, Frightened, Poisoned, Paralyzed, Restrained, Blinded, Charmed, Stunned, Incapacitated,
        Burning, Bleeding, Blessed, Baned, Hexed, HuntersMark, Turned, Dodging, Raging, Hasted, Lethargic,
        ShieldSpell, MageArmor, ShieldOfFaith, FaerieFire, Asleep, Laughing, Disengaged, Invisible,
        ProtectionEvil, Guidance, Resistance, BladeWard, Reeling, GapingWound, Dazed, Enlarged, Inspired,
        Downed, Stabilized, Dead, Concentrating, Wildshape, SpiritGuardians, Aided, ArmorOfAgathys,
        WeakenedStrength, Reckless, Frenzied, SacredWeapon, VowOfEnmity, Chilled, Silenced, Frozen, Wet,
        BlessedByChoice, Heroism, Sanctuary, Webbed, Ensnared, DivineFavor, Darkvision
    }

    public enum Rest { None, Short, Long }

    public enum ArmorCat { None, Light, Medium, Heavy, Shield }

    public enum WeaponCat { Simple, Martial, Natural }

    [Flags]
    public enum WeaponProps
    {
        None = 0, Finesse = 1, Light = 2, Heavy = 4, TwoHanded = 8, Versatile = 16, Reach = 32, Thrown = 64,
        Ranged = 128, Ammunition = 256, Loading = 512, Monk = 1024
    }

    public enum ArmorVisual { None, Cloth, Robe, Leather, Hide, Chain, Scale, Plate }

    public enum WeaponVisual
    {
        None, Dagger, Shortsword, Longsword, Greatsword, Rapier, Scimitar, Handaxe, Battleaxe, Greataxe, Mace, Morningstar,
        Warhammer, Maul, Club, Greatclub, Quarterstaff, Spear, Glaive, Halberd, Trident, Flail, Sickle, Javelin,
        Shortbow, Longbow, LightCrossbow, HeavyCrossbow, HandCrossbow, Staff, Wand, Shield, Lute, HolySymbol, Torch
    }

    public static class EnumUtil
    {
        public static string Nice(this Skill s)
        {
            switch (s)
            {
                case Skill.AnimalHandling: return "Animal Handling";
                case Skill.SleightOfHand: return "Sleight of Hand";
                default: return s.ToString();
            }
        }

        public static Ability Ab(this Skill s)
        {
            switch (s)
            {
                case Skill.Athletics: return Ability.STR;
                case Skill.Acrobatics: case Skill.SleightOfHand: case Skill.Stealth: return Ability.DEX;
                case Skill.Arcana: case Skill.History: case Skill.Investigation: case Skill.Nature: case Skill.Religion: return Ability.INT;
                case Skill.AnimalHandling: case Skill.Insight: case Skill.Medicine: case Skill.Perception: case Skill.Survival: return Ability.WIS;
                default: return Ability.CHA;
            }
        }

        public static string Long(this Ability a)
        {
            switch (a)
            {
                case Ability.STR: return "Strength";
                case Ability.DEX: return "Dexterity";
                case Ability.CON: return "Constitution";
                case Ability.INT: return "Intelligence";
                case Ability.WIS: return "Wisdom";
                default: return "Charisma";
            }
        }

        public static string Nice(this RaceId r)
        {
            switch (r)
            {
                case RaceId.HalfElf: return "Half-Elf";
                case RaceId.HalfOrc: return "Half-Orc";
                default: return r.ToString();
            }
        }

        public static string Nice(this SubraceId s)
        {
            switch (s)
            {
                case SubraceId.HighElf: return "High Elf";
                case SubraceId.WoodElf: return "Wood Elf";
                case SubraceId.LolthSworn: return "Lolth-Sworn Drow";
                case SubraceId.Seldarine: return "Seldarine Drow";
                case SubraceId.HalfElfHigh: return "High Half-Elf";
                case SubraceId.HalfElfWood: return "Wood Half-Elf";
                case SubraceId.HalfElfDrow: return "Drow Half-Elf";
                case SubraceId.Lightfoot: return "Lightfoot Halfling";
                case SubraceId.Strongheart: return "Strongheart Halfling";
                case SubraceId.GoldDwarf: return "Gold Dwarf";
                case SubraceId.ShieldDwarf: return "Shield Dwarf";
                case SubraceId.Duergar: return "Duergar";
                case SubraceId.ForestGnome: return "Forest Gnome";
                case SubraceId.DeepGnome: return "Deep Gnome";
                case SubraceId.RockGnome: return "Rock Gnome";
                case SubraceId.Asmodeus: return "Asmodeus Tiefling";
                case SubraceId.Mephistopheles: return "Mephistopheles Tiefling";
                case SubraceId.Zariel: return "Zariel Tiefling";
                case SubraceId.None: return "";
                default: return s + " Dragonborn";
            }
        }

        public static string Nice(this BackgroundId b)
        {
            switch (b)
            {
                case BackgroundId.FolkHero: return "Folk Hero";
                case BackgroundId.GuildArtisan: return "Guild Artisan";
                case BackgroundId.HauntedOne: return "Haunted One";
                default: return b.ToString();
            }
        }

        public static string Nice(this Cond c)
        {
            switch (c)
            {
                case Cond.HuntersMark: return "Hunter's Mark";
                case Cond.ShieldSpell: return "Shield";
                case Cond.MageArmor: return "Mage Armour";
                case Cond.ShieldOfFaith: return "Shield of Faith";
                case Cond.FaerieFire: return "Faerie Fire";
                case Cond.ProtectionEvil: return "Protection from Evil";
                case Cond.BladeWard: return "Blade Ward";
                case Cond.GapingWound: return "Gaping Wound";
                case Cond.SpiritGuardians: return "Spirit Guardians";
                case Cond.ArmorOfAgathys: return "Armour of Agathys";
                case Cond.WeakenedStrength: return "Strength Drained";
                case Cond.SacredWeapon: return "Sacred Weapon";
                case Cond.VowOfEnmity: return "Vow of Enmity";
                case Cond.DivineFavor: return "Divine Favour";
                case Cond.BlessedByChoice: return "Blessed";
                default: return c.ToString();
            }
        }

        public static string Nice(this DamageType d) => d.ToString();
    }
}
