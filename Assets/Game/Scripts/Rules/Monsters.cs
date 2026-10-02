using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.Rules
{
    public enum BodyKind { Humanoid, Wolf, DireWolf, Bear, Rat, Swarm, Mound, Grick, Broom, Spider }

    public class MonsterDef
    {
        public string id, name, desc;
        public CreatureType type = CreatureType.Undead;
        public SizeCat size = SizeCat.Medium;
        public Faction faction = Faction.Hostile;
        public int ac = 12, hp = 10, prof = 2, xp = 50, level = 1;
        public float speedM = 9f;
        public int[] abil = { 10, 10, 10, 10, 10, 10 };
        public Ability[] saves = new Ability[0];
        public Skill[] skills = new Skill[0];
        public DamageType[] resist = new DamageType[0], immune = new DamageType[0], vuln = new DamageType[0];
        public Cond[] condImmune = new Cond[0];
        public string[] actions = new string[0];
        public Ability spellAbility = Ability.WIS;
        public bool packTactics, undeadFortitude, incorporeal, sunlightSensitive, flies;
        public int regeneration;
        public string[] loot = new string[0];
        // visuals
        public BodyKind body = BodyKind.Humanoid;
        public Appearance look;
        public GearLook gear;
        public MotionStyle motion = MotionStyle.Normal;
        public float scale = 1f;
        public Color tint = Color.white;
    }

    public static class Monsters
    {
        public static readonly Dictionary<string, MonsterDef> All = new Dictionary<string, MonsterDef>();
        public static MonsterDef Get(string id) => All.TryGetValue(id, out var m) ? m : null;
        static MonsterDef Add(MonsterDef m) { All[m.id] = m; return m; }
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static readonly DamageType[] Phys = { DamageType.Bludgeoning, DamageType.Piercing, DamageType.Slashing };

        static Monsters()
        {
            Add(new MonsterDef
            {
                id = "wolf", name = "Wolf", desc = "A lean grey wolf of the Svalich Woods. In Barovia, wolves hunt in packs and answer to a master.",
                type = CreatureType.Beast, ac = 13, hp = 11, speedM = 12, xp = 50, abil = new[] { 12, 15, 12, 3, 12, 6 }, skills = new[] { Skill.Perception, Skill.Stealth },
                actions = new[] { "bite_wolf" }, packTactics = true, body = BodyKind.Wolf, tint = C("#5b5a58"), loot = new[] { "wolf_pelt" }
            });
            Add(new MonsterDef
            {
                id = "dire_wolf", name = "Dire Wolf", desc = "A wolf the size of a pony, black-furred, with eyes like coals.", type = CreatureType.Beast, size = SizeCat.Large,
                ac = 14, hp = 37, speedM = 15, xp = 200, level = 3, abil = new[] { 17, 15, 15, 3, 12, 7 }, skills = new[] { Skill.Perception, Skill.Stealth },
                actions = new[] { "bite_dire_wolf" }, packTactics = true, body = BodyKind.DireWolf, tint = C("#2b2a2c"), scale = 1.45f, loot = new[] { "wolf_pelt" }
            });
            Add(new MonsterDef
            {
                id = "swarm_bats", name = "Swarm of Bats", desc = "A shrieking cloud of leathery wings.", type = CreatureType.Beast, ac = 12, hp = 22, speedM = 9, xp = 50,
                abil = new[] { 5, 15, 10, 2, 12, 4 }, resist = Phys, actions = new[] { "bites_swarm" }, flies = true, body = BodyKind.Swarm, tint = C("#231d1d"),
                condImmune = new[] { Cond.Charmed, Cond.Frightened, Cond.Paralyzed, Cond.Prone, Cond.Restrained, Cond.Stunned }
            });
            Add(new MonsterDef
            {
                id = "rat", name = "Giant Rat", desc = "A rat the size of a dog, slick with sewer filth.", type = CreatureType.Beast, size = SizeCat.Small, ac = 12, hp = 7, speedM = 9, xp = 25,
                abil = new[] { 7, 15, 11, 2, 10, 4 }, actions = new[] { "bite_rat" }, packTactics = true, body = BodyKind.Rat, tint = C("#4a3f38"), scale = 0.6f
            });
            Add(new MonsterDef
            {
                id = "zombie", name = "Barovian Zombie", desc = "What remains of a villager who died in the mists. It shambles on, relentless, and is very hard to put down for good.",
                ac = 8, hp = 22, speedM = 6, xp = 50, abil = new[] { 13, 6, 16, 3, 6, 5 }, saves = new[] { Ability.WIS }, immune = new[] { DamageType.Poison }, condImmune = new[] { Cond.Poisoned },
                actions = new[] { "slam_zombie" }, undeadFortitude = true,
                look = new Appearance { race = RaceId.Human, skin = C("#8b8d72"), hair = C("#3a3530"), hairStyle = 8, rotting = true, hunched = true, cloth1 = C("#4a4238"), cloth2 = C("#2e2a26"), eyes = C("#d8d0b0"), glowEyes = C("#c8c090") },
                gear = new GearLook { armor = ArmorVisual.Cloth }, motion = MotionStyle.Zombie
            });
            Add(new MonsterDef
            {
                id = "skeleton", name = "Skeleton", desc = "Bones held together by old, spiteful magic.", ac = 13, hp = 13, speedM = 9, xp = 50,
                abil = new[] { 10, 14, 15, 6, 8, 5 }, vuln = new[] { DamageType.Bludgeoning }, immune = new[] { DamageType.Poison }, condImmune = new[] { Cond.Poisoned },
                actions = new[] { "shortsword_skeleton" },
                look = new Appearance { race = RaceId.Human, skeletal = true, hairStyle = 0, glowEyes = C("#6ad0ff") }, gear = new GearLook { armor = ArmorVisual.None, main = WeaponVisual.Shortsword, drawn = true }, motion = MotionStyle.Skeleton
            });
            Add(new MonsterDef
            {
                id = "ghoul", name = "Ghoul", desc = "A grey, hairless eater of the dead. Its claws carry a paralysing rot.", ac = 12, hp = 22, speedM = 9, xp = 200, level = 2,
                abil = new[] { 13, 15, 10, 7, 10, 6 }, immune = new[] { DamageType.Poison }, condImmune = new[] { Cond.Charmed, Cond.Poisoned },
                actions = new[] { "claws_ghoul", "bite_ghoul" },
                look = new Appearance { race = RaceId.Human, skin = C("#7c7f7a"), hairStyle = 0, gaunt = true, rotting = true, glowEyes = C("#e8e060"), cloth1 = C("#3a3530"), cloth2 = C("#2a2622") },
                gear = new GearLook { armor = ArmorVisual.None }, motion = MotionStyle.Ghoul
            });
            Add(new MonsterDef
            {
                id = "ghast", name = "Ghast", desc = "A ghoul's elder, reeking of the grave.", ac = 13, hp = 36, speedM = 9, xp = 450, level = 3,
                abil = new[] { 16, 17, 10, 11, 10, 8 }, resist = new[] { DamageType.Necrotic }, immune = new[] { DamageType.Poison }, condImmune = new[] { Cond.Charmed, Cond.Poisoned },
                actions = new[] { "claws_ghast", "bite_ghoul" },
                look = new Appearance { race = RaceId.Human, skin = C("#6a6e6a"), hairStyle = 0, gaunt = true, rotting = true, glowEyes = C("#f0a040"), strong = true, cloth1 = C("#2a2622"), cloth2 = C("#1a1614") },
                gear = new GearLook { armor = ArmorVisual.None }, motion = MotionStyle.Ghoul
            });
            Add(new MonsterDef
            {
                id = "shadow", name = "Shadow", desc = "A living darkness that feeds on strength. Its touch leaves you weaker; those it kills become shadows themselves.",
                ac = 12, hp = 16, speedM = 12, xp = 100, level = 2, abil = new[] { 6, 14, 13, 6, 10, 8 }, skills = new[] { Skill.Stealth },
                resist = new[] { DamageType.Acid, DamageType.Cold, DamageType.Fire, DamageType.Lightning, DamageType.Thunder, DamageType.Bludgeoning, DamageType.Piercing, DamageType.Slashing },
                immune = new[] { DamageType.Necrotic, DamageType.Poison }, vuln = new[] { DamageType.Radiant }, condImmune = new[] { Cond.Frightened, Cond.Paralyzed, Cond.Poisoned, Cond.Prone, Cond.Restrained },
                actions = new[] { "drain_shadow" }, incorporeal = true, sunlightSensitive = true,
                look = new Appearance { race = RaceId.Human, ghostly = true, hairStyle = 8, glowEyes = C("#ffffff") }, gear = new GearLook { armor = ArmorVisual.Robe }, motion = MotionStyle.Ghost, tint = new Color(0.05f, 0.05f, 0.08f)
            });
            Add(new MonsterDef
            {
                id = "specter", name = "Specter", desc = "The hateful spirit of someone who died by violence in this house.", ac = 12, hp = 22, speedM = 15, xp = 200, level = 3,
                abil = new[] { 1, 14, 11, 10, 10, 11 }, resist = new[] { DamageType.Acid, DamageType.Cold, DamageType.Fire, DamageType.Lightning, DamageType.Thunder, DamageType.Bludgeoning, DamageType.Piercing, DamageType.Slashing },
                immune = new[] { DamageType.Necrotic, DamageType.Poison }, condImmune = new[] { Cond.Charmed, Cond.Paralyzed, Cond.Poisoned, Cond.Prone, Cond.Restrained },
                actions = new[] { "life_drain_specter" }, incorporeal = true, flies = true,
                look = new Appearance { race = RaceId.Human, ghostly = true, hairStyle = 2, bodyType = 1 }, gear = new GearLook { armor = ArmorVisual.Robe }, motion = MotionStyle.Ghost
            });
            Add(new MonsterDef
            {
                id = "animated_armor", name = "Animated Armour", desc = "An empty suit of plate that still stands its lord's watch.", type = CreatureType.Construct, ac = 16, hp = 33, speedM = 7.5f, xp = 200, level = 2,
                abil = new[] { 14, 11, 13, 1, 3, 1 }, immune = new[] { DamageType.Poison, DamageType.Psychic },
                condImmune = new[] { Cond.Blinded, Cond.Charmed, Cond.Frightened, Cond.Paralyzed, Cond.Poisoned },
                actions = new[] { "slam_armor" },
                look = new Appearance { race = RaceId.Human, headless = true, strong = true, glowEyes = C("#ff6a3a") }, gear = new GearLook { armor = ArmorVisual.Plate }, motion = MotionStyle.Brute
            });
            Add(new MonsterDef
            {
                id = "broom", name = "Broom of Animated Attack", desc = "A broom that sweeps with alarming enthusiasm.", type = CreatureType.Construct, size = SizeCat.Small, ac = 15, hp = 17, speedM = 15, xp = 50,
                abil = new[] { 10, 17, 10, 1, 5, 1 }, immune = new[] { DamageType.Poison, DamageType.Psychic }, condImmune = new[] { Cond.Blinded, Cond.Charmed, Cond.Frightened, Cond.Paralyzed, Cond.Poisoned, Cond.Prone },
                actions = new[] { "broom_attack" }, flies = true, body = BodyKind.Broom
            });
            Add(new MonsterDef
            {
                id = "grick", name = "Grick", desc = "A worm-like horror with a beaked maw ringed by four barbed tentacles. It waits in the dark for warm things to pass.",
                type = CreatureType.Monstrosity, ac = 14, hp = 27, speedM = 9, xp = 100, level = 2, abil = new[] { 14, 14, 11, 3, 14, 5 }, skills = new[] { Skill.Stealth },
                resist = Phys, actions = new[] { "tentacles_grick" }, body = BodyKind.Grick, tint = C("#5a5f58")
            });
            Add(new MonsterDef
            {
                id = "cultist", name = "Hooded Cultist", desc = "A servant of the dark power that dwells beneath the house, eyes blank with devotion.", type = CreatureType.Humanoid, ac = 12, hp = 9, speedM = 9, xp = 25,
                abil = new[] { 11, 12, 10, 10, 11, 10 }, skills = new[] { Skill.Deception, Skill.Religion }, actions = new[] { "dagger_cultist" },
                look = new Appearance { race = RaceId.Human, hooded = true, cloth1 = C("#171314"), cloth2 = C("#2a1a1c"), skin = C("#c9a58c"), hairStyle = 1 }, gear = new GearLook { armor = ArmorVisual.Robe, main = WeaponVisual.Dagger, drawn = true }
            });
            Add(new MonsterDef
            {
                id = "cult_fanatic", name = "Cult Leader", desc = "A fanatic who speaks with the voices under the house.", type = CreatureType.Humanoid, ac = 13, hp = 33, speedM = 9, xp = 450, level = 3,
                abil = new[] { 11, 14, 12, 10, 13, 14 }, skills = new[] { Skill.Deception, Skill.Persuasion, Skill.Religion }, actions = new[] { "dagger_cultist", "toll_the_dead", "inflict_wounds", "hold_person" }, spellAbility = Ability.WIS,
                look = new Appearance { race = RaceId.Human, hooded = true, cloth1 = C("#3a0c10"), cloth2 = C("#170a0b"), skin = C("#b8937a"), hairStyle = 1 }, gear = new GearLook { armor = ArmorVisual.Robe, main = WeaponVisual.Dagger, drawn = true }
            });
            Add(new MonsterDef
            {
                id = "lorghoth", name = "Lorghoth the Decayer", desc = "The thing the cult fed. A heaving mound of rot and roots that remembers every sacrifice it was given. Dry rot burns — and lightning only feeds it.",
                type = CreatureType.Plant, size = SizeCat.Large, ac = 13, hp = 60, speedM = 6, xp = 1100, level = 4, abil = new[] { 18, 8, 16, 5, 10, 5 }, skills = new[] { Skill.Stealth },
                resist = new[] { DamageType.Cold, DamageType.Necrotic }, vuln = new[] { DamageType.Fire }, immune = new[] { DamageType.Lightning }, condImmune = new[] { Cond.Blinded, Cond.Frightened },
                actions = new[] { "slam_mound", "engulf_mound" }, body = BodyKind.Mound, tint = C("#3a4028"), scale = 1.35f
            });
            Add(new MonsterDef
            {
                id = "vampire_spawn", name = "Doru, Vampire Spawn", desc = "The priest's son, turned by the master of Castle Ravenloft. He is starving, and he remembers his father's voice.",
                type = CreatureType.Undead, ac = 13, hp = 44, speedM = 9, xp = 600, level = 3, abil = new[] { 16, 16, 16, 11, 10, 12 }, saves = new[] { Ability.DEX, Ability.WIS }, skills = new[] { Skill.Perception, Skill.Stealth },
                resist = new[] { DamageType.Necrotic }, regeneration = 3, sunlightSensitive = true,
                actions = new[] { "claws_spawn", "bite_spawn" },
                look = new Appearance { race = RaceId.Human, skin = C("#c8c2c0"), hair = C("#2b1e18"), hairStyle = 8, gaunt = true, glowEyes = C("#ff3030"), cloth1 = C("#4a4038"), cloth2 = C("#2a2420") },
                gear = new GearLook { armor = ArmorVisual.Cloth }, motion = MotionStyle.Ghoul
            });
            Add(new MonsterDef
            {
                id = "strahd", name = "Strahd von Zarovich", desc = "Lord of Barovia. Count, conqueror, vampire. He is everywhere the mists are.",
                type = CreatureType.Undead, faction = Faction.Neutral, ac = 16, hp = 144, speedM = 9, xp = 0, level = 15, prof = 4, abil = new[] { 18, 18, 18, 20, 15, 18 },
                resist = new[] { DamageType.Necrotic, DamageType.Bludgeoning, DamageType.Piercing, DamageType.Slashing }, regeneration = 20,
                actions = new[] { "claw_strahd", "strahd_charm" },
                look = new Appearance { race = RaceId.Human, skin = C("#d8d0cc"), hair = C("#120c0c"), hairStyle = 8, faceShape = 2, gaunt = true, cape = true, capeColor = C("#0d0a0c"), capeLining = C("#6a0610"), cloth1 = C("#141012"), cloth2 = C("#0c0a0b"), eyes = C("#5a1010"), heightAdj = 1f },
                gear = new GearLook { armor = ArmorVisual.Cloth }, motion = MotionStyle.Noble
            });
            // Wild Shape forms (used by druids)
            Add(new MonsterDef
            {
                id = "wildshape_wolf", name = "Wolf Form", type = CreatureType.Beast, ac = 13, hp = 11, speedM = 12, abil = new[] { 12, 15, 12, 3, 12, 6 }, skills = new[] { Skill.Perception, Skill.Stealth },
                actions = new[] { "bite_wildwolf" }, packTactics = true, body = BodyKind.Wolf, tint = C("#8a7a68"), faction = Faction.Party
            });
            Add(new MonsterDef
            {
                id = "wildshape_bear", name = "Bear Form", type = CreatureType.Beast, size = SizeCat.Large, ac = 11, hp = 34, speedM = 12, abil = new[] { 19, 10, 16, 2, 13, 7 },
                actions = new[] { "claw_bear" }, body = BodyKind.Bear, tint = C("#4a3526"), faction = Faction.Party, scale = 1.3f
            });
        }

        /// <summary>Villager & NPC appearance presets (non-combatants).</summary>
        public static Appearance Villager(System.Random r, bool fem)
        {
            var a = RaceLooks.Randomize(RaceId.Human, SubraceId.None, fem ? 1 : 0, r);
            a.cloth1 = Color.Lerp(C("#3a3028"), C("#5a4a3a"), (float)r.NextDouble());
            a.cloth2 = Color.Lerp(C("#20201e"), C("#3a342a"), (float)r.NextDouble());
            a.age = (float)r.NextDouble();
            a.gaunt = r.NextDouble() < 0.4;
            return a;
        }
    }
}
