using System.Collections.Generic;
using System.Linq;

namespace Dungine.Rules
{
    public class RaceDef
    {
        public RaceId id; public string name, desc;
        public float speedM = 9f; public SizeCat size = SizeCat.Medium;
        public bool darkvision, feyAncestry, dwarvenResilience, lucky, brave, gnomeCunning, relentless, savageAttacks, hellishResistance;
        public List<Skill> skills = new List<Skill>();
        public List<string> weaponProfs = new List<string>();
        public bool lightArmor, mediumArmor, shields;
        public List<string> cantrips = new List<string>();
        public string spellL3, spellL5;
        public List<string> traits = new List<string>();
        public List<SubraceId> subraces = new List<SubraceId>();
    }

    public class SubraceDef
    {
        public SubraceId id; public RaceId race; public string name, desc;
        public float speedBonus;
        public List<Skill> skills = new List<Skill>();
        public List<string> cantrips = new List<string>();
        public string spellL3, spellL5;
        public List<string> traits = new List<string>();
        public DamageType resist = DamageType.None;
        public DamageType breath = DamageType.None; public bool breathLine;
        public bool superiorDarkvision, poisonResist, extraHP, stealthAdv;
        public List<string> weaponProfs = new List<string>();
    }

    public class FeatureInfo
    {
        public int level; public string name, desc; public string[] actions = new string[0];
        public FeatureInfo(int l, string n, string d, params string[] a) { level = l; name = n; desc = d; actions = a; }
    }

    public enum CasterType { None, Full, Half, Pact }

    public class SubclassDef
    {
        public string id, name, desc; public ClassId cls;
        public List<FeatureInfo> features = new List<FeatureInfo>();
        public Dictionary<int, string[]> bonusSpells = new Dictionary<int, string[]>();
        public bool heavyArmor, martialWeapons, mediumArmor, shields;
    }

    public class ClassDef
    {
        public ClassId id; public string name, desc, tagline;
        public int hitDie;
        public Ability primary; public Ability[] saves;
        public bool lightArmor, mediumArmor, heavyArmor, shields, simpleWeapons, martialWeapons;
        public List<string> weaponFamilies = new List<string>();
        public int skillCount; public List<Skill> skillList;
        public CasterType caster; public Ability spellAbility;
        public bool prepared;
        public int[] cantripsKnown = new int[6];
        public int[] spellsKnown = new int[6];
        public int subclassLevel = 3;
        public string subclassLabel;
        public List<SubclassDef> subclasses = new List<SubclassDef>();
        public List<FeatureInfo> features = new List<FeatureInfo>();
        public string[] startingItems;
        public int[] recommended;       // point-buy base scores STR..CHA
        public ArmorVisual look;
        public bool martial;            // gets weapon actions
    }

    public class BackgroundDef
    {
        public BackgroundId id; public string name, desc; public Skill[] skills;
    }

    public static class RulesData
    {
        public static readonly Dictionary<RaceId, RaceDef> Races = new Dictionary<RaceId, RaceDef>();
        public static readonly Dictionary<SubraceId, SubraceDef> Subraces = new Dictionary<SubraceId, SubraceDef>();
        public static readonly Dictionary<ClassId, ClassDef> Classes = new Dictionary<ClassId, ClassDef>();
        public static readonly Dictionary<BackgroundId, BackgroundDef> Backgrounds = new Dictionary<BackgroundId, BackgroundDef>();

        public static readonly int[] XpForLevel = { 0, 0, 300, 900, 2700, 6500, 14000 };
        public const int MaxLevel = 5;
        public static int ProfBonus(int level) => level >= 5 ? 3 : 2;

        public static SubclassDef Subclass(ClassId c, string id) => Classes[c].subclasses.FirstOrDefault(s => s.id == id);

        static RaceDef R(RaceId id, string name, string desc) { var r = new RaceDef { id = id, name = name, desc = desc }; Races[id] = r; return r; }
        static SubraceDef S(SubraceId id, RaceId race, string name, string desc) { var s = new SubraceDef { id = id, race = race, name = name, desc = desc }; Subraces[id] = s; Races[race].subraces.Add(id); return s; }

        static RulesData()
        {
            // ======================= RACES =======================
            var r = R(RaceId.Human, "Human", "The most common folk of the Sword Coast and, for that matter, of Barovia. Humans are adaptable, ambitious and short-lived, which makes them bold.");
            r.skills.Add(Skill.Persuasion); r.lightArmor = true; r.shields = true; r.weaponProfs.AddRange(new[] { "spear", "halberd", "glaive" });
            r.traits.Add("Civil Militia: proficiency with spears, halberds, glaives, light armour and shields.");
            r.traits.Add("Human Versatility: proficiency in one extra skill.");

            r = R(RaceId.Elf, "Elf", "Graceful, long-lived and never entirely of this world. Elves see in the dark, cannot be magically put to sleep and remember slights for centuries.");
            r.darkvision = true; r.feyAncestry = true; r.skills.Add(Skill.Perception); r.weaponProfs.AddRange(new[] { "longsword", "shortsword", "shortbow", "longbow" });
            r.traits.Add("Darkvision: see in the dark within 12m.");
            r.traits.Add("Fey Ancestry: advantage on saves against being charmed; magic can't put you to sleep.");
            r.traits.Add("Keen Senses: proficiency in Perception.");
            r.traits.Add("Elven Weapon Training: longswords, shortswords, shortbows and longbows.");
            var s = S(SubraceId.HighElf, RaceId.Elf, "High Elf", "Heirs of an old magical tradition. You know one wizard cantrip.");
            s.cantrips.Add("fire_bolt"); s.traits.Add("Cantrip: Fire Bolt.");
            s = S(SubraceId.WoodElf, RaceId.Elf, "Wood Elf", "Quick and quiet folk of the deep forests.");
            s.speedBonus = 1.5f; s.skills.Add(Skill.Stealth); s.traits.Add("Fleet of Foot: +1.5m movement."); s.traits.Add("Mask of the Wild: proficiency in Stealth.");

            r = R(RaceId.Drow, "Drow", "Elves of the Underdark, raised in a society of spiders and poison. Some still serve the goddess of their mothers; some have turned their faces to the surface gods.");
            r.darkvision = true; r.feyAncestry = true; r.skills.Add(Skill.Perception); r.weaponProfs.AddRange(new[] { "rapier", "shortsword", "hand_crossbow" });
            r.cantrips.Add("dancing_lights"); r.spellL3 = "faerie_fire"; r.spellL5 = "darkness";
            r.traits.Add("Superior Darkvision: see in the dark within 24m.");
            r.traits.Add("Fey Ancestry: advantage on saves against being charmed; magic can't put you to sleep.");
            r.traits.Add("Drow Weapon Training: rapiers, shortswords and hand crossbows.");
            r.traits.Add("Drow Magic: Faerie Fire at level 3, Darkness at level 5, once per long rest each.");
            S(SubraceId.LolthSworn, RaceId.Drow, "Lolth-Sworn Drow", "Raised in the cult of the Spider Queen; your eyes glow a deep crimson.").superiorDarkvision = true;
            S(SubraceId.Seldarine, RaceId.Drow, "Seldarine Drow", "You have turned from Lolth toward the wider elven pantheon.").superiorDarkvision = true;

            r = R(RaceId.HalfElf, "Half-Elf", "Belonging fully to neither parent's people, half-elves make their own way — and often make good diplomats.");
            r.darkvision = true; r.feyAncestry = true; r.lightArmor = true; r.shields = true; r.weaponProfs.AddRange(new[] { "spear", "halberd", "glaive", "pike" });
            r.traits.Add("Darkvision: see in the dark within 12m.");
            r.traits.Add("Fey Ancestry: advantage on saves against being charmed; magic can't put you to sleep.");
            r.traits.Add("Civil Militia: spears, halberds, glaives, light armour and shields.");
            s = S(SubraceId.HalfElfHigh, RaceId.HalfElf, "High Half-Elf", "Your elven parent's magic runs in your blood."); s.cantrips.Add("fire_bolt"); s.traits.Add("Cantrip: Fire Bolt.");
            s = S(SubraceId.HalfElfWood, RaceId.HalfElf, "Wood Half-Elf", "Light-footed like your forest kin."); s.speedBonus = 1.5f; s.skills.Add(Skill.Stealth); s.traits.Add("Fleet of Foot: +1.5m movement."); s.traits.Add("Mask of the Wild: proficiency in Stealth.");
            s = S(SubraceId.HalfElfDrow, RaceId.HalfElf, "Drow Half-Elf", "Half of you remembers the Underdark."); s.spellL3 = "faerie_fire"; s.traits.Add("Drow Magic: Faerie Fire at level 3.");

            r = R(RaceId.HalfOrc, "Half-Orc", "Strong, fierce and hard to kill. Half-orcs are used to proving themselves twice over.");
            r.darkvision = true; r.relentless = true; r.savageAttacks = true; r.skills.Add(Skill.Intimidation);
            r.traits.Add("Darkvision: see in the dark within 12m.");
            r.traits.Add("Menacing: proficiency in Intimidation.");
            r.traits.Add("Relentless Endurance: once per long rest, when reduced to 0 hit points you drop to 1 instead.");
            r.traits.Add("Savage Attacks: critical hits with melee weapons roll one extra weapon die.");

            r = R(RaceId.Halfling, "Halfling", "Small, cheerful and uncommonly lucky. Halflings get out of trouble as easily as they get into it.");
            r.size = SizeCat.Small; r.speedM = 7.5f; r.lucky = true; r.brave = true;
            r.traits.Add("Lucky: when you roll a 1 on an attack, check or save, you reroll it.");
            r.traits.Add("Brave: advantage on saves against being frightened.");
            r.traits.Add("Halfling Nimbleness: move through the space of larger creatures.");
            s = S(SubraceId.Lightfoot, RaceId.Halfling, "Lightfoot Halfling", "Easily overlooked and good at hiding."); s.skills.Add(Skill.Stealth); s.stealthAdv = true; s.traits.Add("Naturally Stealthy: proficiency and advantage in Stealth.");
            s = S(SubraceId.Strongheart, RaceId.Halfling, "Strongheart Halfling", "Some say there's dwarven blood in the line."); s.poisonResist = true; s.resist = DamageType.Poison; s.traits.Add("Stout Resilience: advantage on saves against poison and resistance to poison damage.");

            r = R(RaceId.Dwarf, "Dwarf", "Stubborn, loyal and nearly impossible to knock over. Dwarves are forged by mountain halls and long grudges.");
            r.speedM = 7.5f; r.darkvision = true; r.dwarvenResilience = true; r.weaponProfs.AddRange(new[] { "battleaxe", "handaxe", "light_hammer", "warhammer" });
            r.traits.Add("Darkvision: see in the dark within 12m.");
            r.traits.Add("Dwarven Resilience: advantage on saves against poison and resistance to poison damage.");
            r.traits.Add("Dwarven Combat Training: battleaxes, handaxes, light hammers and warhammers.");
            r.traits.Add("Heavy armour doesn't reduce your speed.");
            s = S(SubraceId.GoldDwarf, RaceId.Dwarf, "Gold Dwarf", "Confident and keenly intuitive."); s.extraHP = true; s.traits.Add("Dwarven Toughness: +1 hit point per level.");
            s = S(SubraceId.ShieldDwarf, RaceId.Dwarf, "Shield Dwarf", "Hardy folk of the northern holds."); s.traits.Add("Dwarven Armour Training: light and medium armour.");
            s = S(SubraceId.Duergar, RaceId.Dwarf, "Duergar", "Grey dwarves of the Underdark, freed from mind flayer thralldom by sheer spite."); s.superiorDarkvision = true; s.spellL3 = "enlarge"; s.spellL5 = "duergar_invisibility"; s.skills.Add(Skill.Stealth);
            s.traits.Add("Superior Darkvision: see in the dark within 24m."); s.traits.Add("Duergar Resilience: advantage on saves against illusions, charm and paralysis."); s.traits.Add("Duergar Magic: Enlarge at level 3, Invisibility at level 5.");

            r = R(RaceId.Gnome, "Gnome", "Small, curious and relentlessly inventive. Gnomes treat the world as a puzzle to be taken apart.");
            r.size = SizeCat.Small; r.speedM = 7.5f; r.darkvision = true; r.gnomeCunning = true;
            r.traits.Add("Darkvision: see in the dark within 12m.");
            r.traits.Add("Gnome Cunning: advantage on Intelligence, Wisdom and Charisma saves.");
            s = S(SubraceId.ForestGnome, RaceId.Gnome, "Forest Gnome", "Friends of small animals and masters of little illusions."); s.cantrips.Add("minor_illusion"); s.traits.Add("Speak with Animals, once per long rest.");
            s = S(SubraceId.DeepGnome, RaceId.Gnome, "Deep Gnome", "Svirfneblin of the Underdark, grey-skinned and unseen."); s.superiorDarkvision = true; s.stealthAdv = true; s.skills.Add(Skill.Stealth); s.traits.Add("Superior Darkvision: 24m."); s.traits.Add("Stone Camouflage: advantage on Stealth checks.");
            s = S(SubraceId.RockGnome, RaceId.Gnome, "Rock Gnome", "Tinkerers and artificers."); s.skills.Add(Skill.History); s.traits.Add("Artificer's Lore: add double proficiency to History checks about magic items.");

            r = R(RaceId.Tiefling, "Tiefling", "Descendants of mortals bound in pacts with the Nine Hells. Horns, tails and uncanny eyes earn tieflings suspicion wherever they go.");
            r.darkvision = true; r.hellishResistance = true;
            r.traits.Add("Darkvision: see in the dark within 12m.");
            r.traits.Add("Hellish Resistance: resistance to fire damage.");
            s = S(SubraceId.Asmodeus, RaceId.Tiefling, "Asmodeus Tiefling", "Heirs of the Lord of the Nine Hells."); s.cantrips.Add("produce_flame"); s.spellL3 = "hellish_rebuke"; s.spellL5 = "darkness"; s.traits.Add("Infernal Legacy: Produce Flame; Hellish Rebuke at level 3; Darkness at level 5.");
            s = S(SubraceId.Mephistopheles, RaceId.Tiefling, "Mephistopheles Tiefling", "Bound to the archdevil of arcane lore."); s.cantrips.Add("fire_bolt"); s.spellL3 = "burning_hands"; s.traits.Add("Legacy of Cania: Fire Bolt; Burning Hands at level 3.");
            s = S(SubraceId.Zariel, RaceId.Tiefling, "Zariel Tiefling", "Descended from the fallen angel of Avernus."); s.cantrips.Add("sacred_flame"); s.spellL3 = "divine_favor"; s.traits.Add("Legacy of Avernus: Sacred Flame; Divine Favour at level 3.");

            r = R(RaceId.Githyanki, "Githyanki", "Warriors of the Astral Plane, once enslaved by mind flayers and never again. Githyanki ride red dragons and answer to a lich-queen.");
            r.lightArmor = true; r.mediumArmor = true; r.weaponProfs.AddRange(new[] { "shortsword", "longsword", "greatsword" }); r.skills.Add(Skill.Arcana);
            r.spellL3 = "githyanki_leap"; r.spellL5 = "misty_step";
            r.traits.Add("Astral Knowledge: proficiency in Arcana.");
            r.traits.Add("Martial Prodigy: light and medium armour, shortswords, longswords and greatswords.");
            r.traits.Add("Githyanki Psionics: Enhanced Leap at level 3, Misty Step at level 5.");

            r = R(RaceId.Dragonborn, "Dragonborn", "Proud people with draconic blood. Dragonborn carry their ancestry in their scales and exhale its fury.");
            r.traits.Add("Draconic Ancestry: resistance to your ancestry's damage type.");
            r.traits.Add("Breath Weapon: exhale your ancestry's element in a cone, once per short rest.");
            void Dr(SubraceId id, string n, DamageType t, bool line) { var d = S(id, RaceId.Dragonborn, n + " Dragonborn", $"Your ancestor was a {n.ToLower()} dragon. You resist {t} damage and breathe {t.ToString().ToLower()}."); d.resist = t; d.breath = t; d.breathLine = line; }
            Dr(SubraceId.Black, "Black", DamageType.Acid, true);
            Dr(SubraceId.Blue, "Blue", DamageType.Lightning, true);
            Dr(SubraceId.Brass, "Brass", DamageType.Fire, true);
            Dr(SubraceId.Bronze, "Bronze", DamageType.Lightning, true);
            Dr(SubraceId.Copper, "Copper", DamageType.Acid, true);
            Dr(SubraceId.Gold, "Gold", DamageType.Fire, false);
            Dr(SubraceId.Green, "Green", DamageType.Poison, false);
            Dr(SubraceId.Red, "Red", DamageType.Fire, false);
            Dr(SubraceId.Silver, "Silver", DamageType.Cold, false);
            Dr(SubraceId.White, "White", DamageType.Cold, false);

            r = R(RaceId.Firbolg, "Firbolg", "Gentle giants of the deep forests, who keep to the old groves and tend them quietly. Firbolgs can slip from sight in a breath and speak with beasts and trees.");
            r.cantrips.Add("hidden_step");
            r.traits.Add("Hidden Step: as a bonus action, turn invisible until the start of your next turn, once per short rest.");
            r.traits.Add("Powerful Build: you count as one size larger when carrying and pushing.");
            r.traits.Add("Speech of Beast and Leaf: you can make yourself understood by beasts and plants.");

            // ======================= CLASSES =======================
            var all = System.Enum.GetValues(typeof(Skill)).Cast<Skill>().ToList();
            var c = new ClassDef
            {
                id = ClassId.Barbarian, name = "Barbarian", tagline = "Fury given form.", desc = "A warrior who draws on a primal rage to shrug off wounds and hit harder than anyone should be able to.",
                hitDie = 12, primary = Ability.STR, saves = new[] { Ability.STR, Ability.CON }, lightArmor = true, mediumArmor = true, shields = true, simpleWeapons = true, martialWeapons = true,
                skillCount = 2, skillList = new List<Skill> { Skill.AnimalHandling, Skill.Athletics, Skill.Intimidation, Skill.Nature, Skill.Perception, Skill.Survival },
                subclassLabel = "Primal Path", startingItems = new[] { "greataxe", "handaxe", "hide", "potion_healing" }, recommended = new[] { 15, 13, 14, 8, 12, 10 }, look = ArmorVisual.Hide, martial = true
            };
            c.features.Add(new FeatureInfo(1, "Rage", "Enter a battle fury as a bonus action for extra damage and resistance to physical damage.", "rage"));
            c.features.Add(new FeatureInfo(1, "Unarmoured Defence", "Without armour, your AC is 10 + Dexterity + Constitution."));
            c.features.Add(new FeatureInfo(2, "Reckless Attack", "Attack with advantage at the cost of giving enemies advantage against you.", "reckless_attack"));
            c.features.Add(new FeatureInfo(2, "Danger Sense", "Advantage on Dexterity saves against effects you can see."));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Extra Attack", "Attack twice whenever you take the Attack action."));
            c.features.Add(new FeatureInfo(5, "Fast Movement", "+3m movement while not wearing heavy armour."));
            var sc = new SubclassDef { id = "berserker", name = "Path of the Berserker", cls = c.id, desc = "Rage becomes a red haze of violence." };
            sc.features.Add(new FeatureInfo(3, "Frenzy", "While raging, make an extra melee attack as a bonus action each turn.", "frenzy_attack")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "wildheart", name = "Path of the Wildheart: Bear", cls = c.id, desc = "The spirit of the bear makes you unbreakable." };
            sc.features.Add(new FeatureInfo(3, "Bear Heart", "While raging you resist all damage except psychic.")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Bard, name = "Bard", tagline = "Words are weapons.", desc = "A performer whose songs and stories carry real magic — inspiring allies, confounding foes and knowing a little of everything.",
                hitDie = 8, primary = Ability.CHA, saves = new[] { Ability.DEX, Ability.CHA }, lightArmor = true, simpleWeapons = true, weaponFamilies = { "hand_crossbow", "longsword", "rapier", "shortsword" },
                skillCount = 3, skillList = all, caster = CasterType.Full, spellAbility = Ability.CHA, cantripsKnown = new[] { 0, 2, 2, 2, 3, 3 }, spellsKnown = new[] { 0, 4, 5, 6, 7, 8 },
                subclassLabel = "College", startingItems = new[] { "rapier", "leather", "potion_healing" }, recommended = new[] { 8, 14, 13, 10, 12, 15 }, look = ArmorVisual.Leather
            };
            c.features.Add(new FeatureInfo(1, "Bardic Inspiration", "Grant an ally a d6 to add to a roll. Uses equal to your Charisma modifier per long rest.", "bardic_inspiration"));
            c.features.Add(new FeatureInfo(1, "Spellcasting", "Cast bard spells using Charisma."));
            c.features.Add(new FeatureInfo(2, "Jack of All Trades", "Add half your proficiency bonus to checks you aren't proficient in."));
            c.features.Add(new FeatureInfo(2, "Song of Rest", "Allies regain extra hit points during a short rest."));
            c.features.Add(new FeatureInfo(3, "Expertise", "Double proficiency in two skills."));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Font of Inspiration", "Bardic Inspiration recharges on a short rest and becomes a d8."));
            sc = new SubclassDef { id = "lore", name = "College of Lore", cls = c.id, desc = "Collectors of secrets and forbidden stanzas." };
            sc.features.Add(new FeatureInfo(3, "Bonus Proficiencies", "Proficiency in three extra skills.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "valour", name = "College of Valour", cls = c.id, desc = "Skalds who sing in the thick of battle.", mediumArmor = true, shields = true, martialWeapons = true };
            sc.features.Add(new FeatureInfo(3, "Combat Inspiration", "Medium armour, shields and martial weapons.")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Cleric, name = "Cleric", tagline = "The gods answer.", desc = "A priest who channels divine power to heal, protect and smite. In Barovia, where the dead do not rest, a cleric's light is precious.",
                hitDie = 8, primary = Ability.WIS, saves = new[] { Ability.WIS, Ability.CHA }, lightArmor = true, mediumArmor = true, shields = true, simpleWeapons = true,
                skillCount = 2, skillList = new List<Skill> { Skill.History, Skill.Insight, Skill.Medicine, Skill.Persuasion, Skill.Religion },
                caster = CasterType.Full, spellAbility = Ability.WIS, prepared = true, cantripsKnown = new[] { 0, 3, 3, 3, 4, 4 }, subclassLevel = 1,
                subclassLabel = "Divine Domain", startingItems = new[] { "mace", "chain_shirt", "shield", "potion_healing", "holy_water" }, recommended = new[] { 12, 10, 14, 8, 15, 13 }, look = ArmorVisual.Chain
            };
            c.features.Add(new FeatureInfo(1, "Spellcasting", "Prepare cleric spells each day using Wisdom."));
            c.features.Add(new FeatureInfo(2, "Channel Divinity: Turn Undead", "Force undead to flee from your holy symbol. Once per short rest.", "turn_undead"));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Destroy Undead", "Weak undead that fail against Turn Undead are destroyed outright."));
            sc = new SubclassDef { id = "life", name = "Life Domain", cls = c.id, desc = "Healers devoted to the sanctity of life.", heavyArmor = true };
            sc.features.Add(new FeatureInfo(1, "Disciple of Life", "Your healing spells restore 2 + spell level extra hit points. Heavy armour proficiency."));
            sc.features.Add(new FeatureInfo(2, "Channel Divinity: Preserve Life", "Heal allies around you.", "preserve_life"));
            sc.bonusSpells[1] = new[] { "bless", "cure_wounds" }; sc.bonusSpells[3] = new[] { "aid", "lesser_restoration" }; sc.bonusSpells[5] = new[] { "revivify", "mass_healing_word" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "light", name = "Light Domain", cls = c.id, desc = "Servants of the sun who burn away darkness — of which Barovia has plenty." };
            sc.features.Add(new FeatureInfo(1, "Warding Flare", "Blinding light protects you; you know the Light cantrip."));
            sc.features.Add(new FeatureInfo(2, "Channel Divinity: Radiance of the Dawn", "Sear enemies around you with sunlight.", "radiance_of_dawn"));
            sc.bonusSpells[1] = new[] { "burning_hands", "faerie_fire" }; sc.bonusSpells[3] = new[] { "scorching_ray" }; sc.bonusSpells[5] = new[] { "daylight", "fireball" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "tempest", name = "Tempest Domain", cls = c.id, desc = "Storm-priests who speak with thunder.", heavyArmor = true, martialWeapons = true };
            sc.features.Add(new FeatureInfo(1, "Tempest Proficiencies", "Heavy armour and martial weapons."));
            sc.features.Add(new FeatureInfo(2, "Channel Divinity: Destructive Wrath", "Your next thunder or lightning spell deals maximum damage.", "destructive_wrath"));
            sc.bonusSpells[1] = new[] { "thunderwave" }; sc.bonusSpells[3] = new[] { "shatter" }; sc.bonusSpells[5] = new[] { "call_lightning" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "war", name = "War Domain", cls = c.id, desc = "Priests of battle, as happy with a warhammer as with a prayer.", heavyArmor = true, martialWeapons = true };
            sc.features.Add(new FeatureInfo(1, "War Priest", "Make a bonus weapon attack, a number of times equal to your Wisdom modifier per long rest.", "war_priest"));
            sc.bonusSpells[1] = new[] { "divine_favor", "shield_of_faith" }; sc.bonusSpells[3] = new[] { "spiritual_weapon" }; sc.bonusSpells[5] = new[] { "spirit_guardians" }; c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Druid, name = "Druid", tagline = "The wild remembers.", desc = "A keeper of the old ways who calls on moon, storm and root — and who can become the beast when words fail.",
                hitDie = 8, primary = Ability.WIS, saves = new[] { Ability.INT, Ability.WIS }, lightArmor = true, mediumArmor = true, shields = true,
                weaponFamilies = { "club", "dagger", "javelin", "mace", "quarterstaff", "scimitar", "sickle", "spear" },
                skillCount = 2, skillList = new List<Skill> { Skill.Arcana, Skill.AnimalHandling, Skill.Insight, Skill.Medicine, Skill.Nature, Skill.Perception, Skill.Religion, Skill.Survival },
                caster = CasterType.Full, spellAbility = Ability.WIS, prepared = true, cantripsKnown = new[] { 0, 2, 2, 2, 3, 3 }, subclassLevel = 2,
                subclassLabel = "Circle", startingItems = new[] { "quarterstaff", "leather", "potion_healing" }, recommended = new[] { 10, 13, 14, 12, 15, 8 }, look = ArmorVisual.Hide
            };
            c.features.Add(new FeatureInfo(1, "Spellcasting", "Prepare druid spells using Wisdom."));
            c.features.Add(new FeatureInfo(2, "Wild Shape", "Transform into a beast twice per short rest.", "wild_shape_wolf", "wild_shape_bear"));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            sc = new SubclassDef { id = "land", name = "Circle of the Land", cls = c.id, desc = "Mystics tied to a particular land." };
            sc.features.Add(new FeatureInfo(2, "Natural Recovery", "Recover spell slots on a short rest once per day."));
            sc.bonusSpells[3] = new[] { "hold_person", "moonbeam" }; sc.bonusSpells[5] = new[] { "call_lightning" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "moon", name = "Circle of the Moon", cls = c.id, desc = "Shapeshifters who fight in beast form." };
            sc.features.Add(new FeatureInfo(2, "Combat Wild Shape", "Wild Shape as a bonus action, and your beast forms are tougher.")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Fighter, name = "Fighter", tagline = "Steel and discipline.", desc = "A master of arms and armour, the fighter wins through training, endurance and more attacks than anyone else.",
                hitDie = 10, primary = Ability.STR, saves = new[] { Ability.STR, Ability.CON }, lightArmor = true, mediumArmor = true, heavyArmor = true, shields = true, simpleWeapons = true, martialWeapons = true,
                skillCount = 2, skillList = new List<Skill> { Skill.Acrobatics, Skill.AnimalHandling, Skill.Athletics, Skill.History, Skill.Insight, Skill.Intimidation, Skill.Perception, Skill.Survival },
                subclassLabel = "Martial Archetype", startingItems = new[] { "longsword", "shield", "chain_mail", "potion_healing" }, recommended = new[] { 15, 13, 14, 10, 12, 8 }, look = ArmorVisual.Plate, martial = true
            };
            c.features.Add(new FeatureInfo(1, "Fighting Style", "Choose a style of combat to specialise in."));
            c.features.Add(new FeatureInfo(1, "Second Wind", "Regain 1d10 + fighter level hit points as a bonus action. Once per short rest.", "second_wind"));
            c.features.Add(new FeatureInfo(2, "Action Surge", "Take an additional action on your turn. Once per short rest.", "action_surge"));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Extra Attack", "Attack twice whenever you take the Attack action."));
            sc = new SubclassDef { id = "champion", name = "Champion", cls = c.id, desc = "Raw physical excellence." };
            sc.features.Add(new FeatureInfo(3, "Improved Critical", "Your weapon attacks score a critical hit on a roll of 19 or 20.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "battlemaster", name = "Battle Master", cls = c.id, desc = "A tactician who controls the fight with manoeuvres." };
            sc.features.Add(new FeatureInfo(3, "Combat Superiority", "Four d8 superiority dice per short rest fuel Trip, Menacing and Precision attacks.", "trip_attack", "menacing_attack", "precision_attack")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Monk, name = "Monk", tagline = "The body is the weapon.", desc = "A martial artist who channels ki into flurries of blows, uncanny speed and strikes that can stop a heart.",
                hitDie = 8, primary = Ability.DEX, saves = new[] { Ability.STR, Ability.DEX }, simpleWeapons = true, weaponFamilies = { "shortsword" },
                skillCount = 2, skillList = new List<Skill> { Skill.Acrobatics, Skill.Athletics, Skill.History, Skill.Insight, Skill.Religion, Skill.Stealth },
                subclassLabel = "Monastic Tradition", startingItems = new[] { "quarterstaff", "wraps", "potion_healing" }, recommended = new[] { 10, 15, 14, 8, 15, 8 }, look = ArmorVisual.None, martial = true
            };
            c.features.Add(new FeatureInfo(1, "Martial Arts", "Use Dexterity for unarmed strikes and monk weapons, and make a bonus unarmed strike after attacking.", "martial_bonus"));
            c.features.Add(new FeatureInfo(1, "Unarmoured Defence", "Without armour or shield, your AC is 10 + Dexterity + Wisdom."));
            c.features.Add(new FeatureInfo(2, "Ki", "Spend ki points on Flurry of Blows, Patient Defence and Step of the Wind.", "flurry", "patient_defense", "step_of_the_wind"));
            c.features.Add(new FeatureInfo(2, "Unarmoured Movement", "+3m movement while unarmoured."));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Extra Attack", "Attack twice whenever you take the Attack action."));
            c.features.Add(new FeatureInfo(5, "Stunning Strike", "Spend ki to try to stun a creature you hit.", "stunning_strike"));
            sc = new SubclassDef { id = "openhand", name = "Way of the Open Hand", cls = c.id, desc = "The purest martial artistry." };
            sc.features.Add(new FeatureInfo(3, "Open Hand Technique", "Your Flurry of Blows can topple or push enemies.", "flurry_topple", "flurry_push")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Paladin, name = "Paladin", tagline = "An oath made flesh.", desc = "A holy warrior bound by a sacred oath, able to heal with a touch and smite the wicked with divine fire.",
                hitDie = 10, primary = Ability.STR, saves = new[] { Ability.WIS, Ability.CHA }, lightArmor = true, mediumArmor = true, heavyArmor = true, shields = true, simpleWeapons = true, martialWeapons = true,
                skillCount = 2, skillList = new List<Skill> { Skill.Athletics, Skill.Insight, Skill.Intimidation, Skill.Medicine, Skill.Persuasion, Skill.Religion },
                caster = CasterType.Half, spellAbility = Ability.CHA, prepared = true, subclassLevel = 1,
                subclassLabel = "Sacred Oath", startingItems = new[] { "longsword", "shield", "chain_mail", "potion_healing", "holy_water" }, recommended = new[] { 15, 10, 13, 8, 10, 15 }, look = ArmorVisual.Plate, martial = true
            };
            c.features.Add(new FeatureInfo(1, "Lay on Hands", "Heal from a pool of 5 hit points per paladin level.", "lay_on_hands"));
            c.features.Add(new FeatureInfo(2, "Fighting Style", "Choose a style of combat."));
            c.features.Add(new FeatureInfo(2, "Spellcasting", "Prepare paladin spells using Charisma."));
            c.features.Add(new FeatureInfo(2, "Divine Smite", "Expend a spell slot on a melee hit for extra radiant damage.", "divine_smite"));
            c.features.Add(new FeatureInfo(3, "Divine Health", "Immune to disease."));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Extra Attack", "Attack twice whenever you take the Attack action."));
            sc = new SubclassDef { id = "devotion", name = "Oath of Devotion", cls = c.id, desc = "The classic knight: honesty, courage, compassion." };
            sc.features.Add(new FeatureInfo(1, "Sacred Weapon", "Channel Oath: add Charisma to weapon attack rolls.", "sacred_weapon"));
            sc.bonusSpells[3] = new[] { "protection_evil" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "ancients", name = "Oath of the Ancients", cls = c.id, desc = "Keepers of the light, beauty and life in the world." };
            sc.features.Add(new FeatureInfo(1, "Healing Radiance", "Channel Oath: heal allies around you.", "healing_radiance"));
            sc.bonusSpells[3] = new[] { "entangle" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "vengeance", name = "Oath of Vengeance", cls = c.id, desc = "Avengers who punish the wicked at any cost." };
            sc.features.Add(new FeatureInfo(1, "Vow of Enmity", "Channel Oath: gain advantage on attacks against one creature.", "vow_of_enmity"));
            sc.bonusSpells[3] = new[] { "bane", "hunters_mark" }; c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Ranger, name = "Ranger", tagline = "Hunter of monsters.", desc = "A warrior of the wilds who tracks prey across any land, strikes from afar and knows a little nature magic.",
                hitDie = 10, primary = Ability.DEX, saves = new[] { Ability.STR, Ability.DEX }, lightArmor = true, mediumArmor = true, shields = true, simpleWeapons = true, martialWeapons = true,
                skillCount = 3, skillList = new List<Skill> { Skill.AnimalHandling, Skill.Athletics, Skill.Insight, Skill.Investigation, Skill.Nature, Skill.Perception, Skill.Stealth, Skill.Survival },
                caster = CasterType.Half, spellAbility = Ability.WIS, spellsKnown = new[] { 0, 0, 2, 3, 3, 4 },
                subclassLabel = "Ranger Archetype", startingItems = new[] { "longbow", "shortsword", "shortsword", "studded", "potion_healing" }, recommended = new[] { 10, 15, 14, 8, 14, 10 }, look = ArmorVisual.Leather, martial = true
            };
            c.features.Add(new FeatureInfo(1, "Favoured Enemy", "You have studied the creatures of the night: advantage on checks to recall lore about undead and a bonus to tracking."));
            c.features.Add(new FeatureInfo(1, "Natural Explorer", "You move easily through wild lands."));
            c.features.Add(new FeatureInfo(2, "Fighting Style", "Choose a style of combat."));
            c.features.Add(new FeatureInfo(2, "Spellcasting", "Cast ranger spells using Wisdom."));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Extra Attack", "Attack twice whenever you take the Attack action."));
            sc = new SubclassDef { id = "hunter", name = "Hunter", cls = c.id, desc = "The bulwark between civilisation and the terrors of the wild." };
            sc.features.Add(new FeatureInfo(3, "Colossus Slayer", "Once per turn, deal an extra 1d8 damage to a creature that is already wounded.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "gloomstalker", name = "Gloom Stalker", cls = c.id, desc = "At home in the darkest places — like Barovia." };
            sc.features.Add(new FeatureInfo(3, "Dread Ambusher", "+Wisdom to initiative, +3m movement and an extra 1d8 on your first attack in the first round of combat.")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Rogue, name = "Rogue", tagline = "Strike from the shadows.", desc = "A scoundrel of skill and precision who finds the weak point in every lock and every enemy.",
                hitDie = 8, primary = Ability.DEX, saves = new[] { Ability.DEX, Ability.INT }, lightArmor = true, simpleWeapons = true, weaponFamilies = { "hand_crossbow", "longsword", "rapier", "shortsword" },
                skillCount = 4, skillList = new List<Skill> { Skill.Acrobatics, Skill.Athletics, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Investigation, Skill.Perception, Skill.Performance, Skill.Persuasion, Skill.SleightOfHand, Skill.Stealth },
                subclassLabel = "Roguish Archetype", startingItems = new[] { "rapier", "dagger", "shortbow", "leather", "potion_healing" }, recommended = new[] { 8, 15, 14, 12, 13, 10 }, look = ArmorVisual.Leather, martial = true
            };
            c.features.Add(new FeatureInfo(1, "Sneak Attack", "Once per turn, deal extra damage when you have advantage or an ally is next to your target."));
            c.features.Add(new FeatureInfo(1, "Expertise", "Double proficiency in two skills."));
            c.features.Add(new FeatureInfo(2, "Cunning Action", "Dash, Disengage or Hide as a bonus action.", "cunning_dash", "cunning_disengage", "cunning_hide"));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            c.features.Add(new FeatureInfo(5, "Uncanny Dodge", "Use your reaction to halve the damage of an attack that hits you."));
            sc = new SubclassDef { id = "thief", name = "Thief", cls = c.id, desc = "Burglar, cutpurse, climber of walls." };
            sc.features.Add(new FeatureInfo(3, "Fast Hands", "Gain an additional bonus action each turn.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "assassin", name = "Assassin", cls = c.id, desc = "A professional killer." };
            sc.features.Add(new FeatureInfo(3, "Assassinate", "Advantage against creatures that haven't acted yet in combat; hits against surprised creatures are critical.")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Sorcerer, name = "Sorcerer", tagline = "Magic in the blood.", desc = "Magic isn't something a sorcerer studies — it's something they are. They bend their spells with raw willpower.",
                hitDie = 6, primary = Ability.CHA, saves = new[] { Ability.CON, Ability.CHA }, weaponFamilies = { "dagger", "quarterstaff", "light_crossbow" },
                skillCount = 2, skillList = new List<Skill> { Skill.Arcana, Skill.Deception, Skill.Insight, Skill.Intimidation, Skill.Persuasion, Skill.Religion },
                caster = CasterType.Full, spellAbility = Ability.CHA, cantripsKnown = new[] { 0, 4, 4, 4, 5, 5 }, spellsKnown = new[] { 0, 2, 3, 4, 5, 6 }, subclassLevel = 1,
                subclassLabel = "Sorcerous Origin", startingItems = new[] { "quarterstaff", "robe", "potion_healing" }, recommended = new[] { 8, 13, 14, 10, 12, 15 }, look = ArmorVisual.Robe
            };
            c.features.Add(new FeatureInfo(1, "Spellcasting", "Cast sorcerer spells using Charisma."));
            c.features.Add(new FeatureInfo(2, "Font of Magic", "Sorcery points equal to your level, recovered on a long rest.", "create_slot"));
            c.features.Add(new FeatureInfo(2, "Metamagic", "Quickened and Twinned Spell.", "quickened_spell", "twinned_spell"));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            sc = new SubclassDef { id = "draconic", name = "Draconic Bloodline", cls = c.id, desc = "A dragon in your family tree." };
            sc.features.Add(new FeatureInfo(1, "Draconic Resilience", "+1 hit point per level, and unarmoured AC of 13 + Dexterity.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "wildmagic", name = "Wild Magic", cls = c.id, desc = "Chaos courses through you; sometimes your spells surge in strange ways." };
            sc.features.Add(new FeatureInfo(1, "Wild Magic Surge", "When you cast a spell there is a chance of a random magical surge.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "storm", name = "Storm Sorcery", cls = c.id, desc = "Born of the tempest." };
            sc.features.Add(new FeatureInfo(1, "Tempestuous Magic", "After casting a levelled spell, fly up to 3m as a bonus action without provoking opportunity attacks."));
            sc.bonusSpells[1] = new[] { "thunderwave" }; c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Warlock, name = "Warlock", tagline = "Power has a price.", desc = "A seeker of forbidden knowledge who has struck a bargain with an otherworldly patron. Few spells, always recovered quickly.",
                hitDie = 8, primary = Ability.CHA, saves = new[] { Ability.WIS, Ability.CHA }, lightArmor = true, simpleWeapons = true,
                skillCount = 2, skillList = new List<Skill> { Skill.Arcana, Skill.Deception, Skill.History, Skill.Intimidation, Skill.Investigation, Skill.Nature, Skill.Religion },
                caster = CasterType.Pact, spellAbility = Ability.CHA, cantripsKnown = new[] { 0, 2, 2, 2, 3, 3 }, spellsKnown = new[] { 0, 2, 3, 4, 5, 6 }, subclassLevel = 1,
                subclassLabel = "Otherworldly Patron", startingItems = new[] { "dagger", "light_crossbow", "leather", "potion_healing" }, recommended = new[] { 8, 14, 14, 10, 10, 15 }, look = ArmorVisual.Leather
            };
            c.features.Add(new FeatureInfo(1, "Pact Magic", "Few spell slots, always cast at your highest level, recovered on a short rest."));
            c.features.Add(new FeatureInfo(2, "Eldritch Invocations", "Agonizing Blast (add Charisma to Eldritch Blast) and Repelling Blast (push targets 3m)."));
            c.features.Add(new FeatureInfo(3, "Pact Boon: Blade", "Summon or bind a pact weapon and attack with Charisma.", "pact_weapon"));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            sc = new SubclassDef { id = "fiend", name = "The Fiend", cls = c.id, desc = "A pact with a lord of the Hells." };
            sc.features.Add(new FeatureInfo(1, "Dark One's Blessing", "When you reduce an enemy to 0 hit points, gain temporary hit points equal to Charisma + warlock level."));
            sc.bonusSpells[1] = new[] { "burning_hands", "command" }; sc.bonusSpells[3] = new[] { "scorching_ray" }; sc.bonusSpells[5] = new[] { "fireball" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "archfey", name = "The Archfey", cls = c.id, desc = "A pact with a lord or lady of the Feywild." };
            sc.features.Add(new FeatureInfo(1, "Fey Presence", "Frighten creatures around you.", "fey_presence"));
            sc.bonusSpells[1] = new[] { "faerie_fire", "sleep" }; sc.bonusSpells[3] = new[] { "misty_step" }; c.subclasses.Add(sc);
            sc = new SubclassDef { id = "greatoldone", name = "The Great Old One", cls = c.id, desc = "A pact with an entity beyond the stars." };
            sc.features.Add(new FeatureInfo(1, "Mortal Reminder", "Your critical hits frighten the target and nearby enemies."));
            sc.bonusSpells[1] = new[] { "dissonant_whispers", "hideous_laughter" }; sc.bonusSpells[3] = new[] { "hold_person" }; c.subclasses.Add(sc);
            Classes[c.id] = c;

            c = new ClassDef
            {
                id = ClassId.Wizard, name = "Wizard", tagline = "Knowledge is power.", desc = "A scholar of the arcane with the largest spellbook of anyone, able to recover spent magic by study.",
                hitDie = 6, primary = Ability.INT, saves = new[] { Ability.INT, Ability.WIS }, weaponFamilies = { "dagger", "quarterstaff", "light_crossbow" },
                skillCount = 2, skillList = new List<Skill> { Skill.Arcana, Skill.History, Skill.Insight, Skill.Investigation, Skill.Medicine, Skill.Religion },
                caster = CasterType.Full, spellAbility = Ability.INT, prepared = true, cantripsKnown = new[] { 0, 3, 3, 3, 4, 4 }, subclassLevel = 2,
                subclassLabel = "Arcane Tradition", startingItems = new[] { "quarterstaff", "robe", "potion_healing", "scroll_magic_missile" }, recommended = new[] { 8, 14, 13, 15, 12, 10 }, look = ArmorVisual.Robe
            };
            c.features.Add(new FeatureInfo(1, "Spellcasting", "Prepare wizard spells using Intelligence."));
            c.features.Add(new FeatureInfo(1, "Arcane Recovery", "Once per day, recover spell slots during a short rest."));
            c.features.Add(new FeatureInfo(4, "Ability Score Improvement", "Increase your ability scores."));
            sc = new SubclassDef { id = "evocation", name = "School of Evocation", cls = c.id, desc = "Masters of fire, ice and lightning." };
            sc.features.Add(new FeatureInfo(2, "Sculpt Spells", "Your area spells never harm your allies.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "abjuration", name = "School of Abjuration", cls = c.id, desc = "Wardens against harm." };
            sc.features.Add(new FeatureInfo(2, "Arcane Ward", "Casting abjuration spells charges a ward that absorbs damage.")); c.subclasses.Add(sc);
            sc = new SubclassDef { id = "necromancy", name = "School of Necromancy", cls = c.id, desc = "Students of life and death." };
            sc.features.Add(new FeatureInfo(2, "Grim Harvest", "When your spell kills a creature, you regain hit points.")); c.subclasses.Add(sc);
            Classes[c.id] = c;

            // ======================= BACKGROUNDS =======================
            void Bg(BackgroundId id, string d, Skill a, Skill b) => Backgrounds[id] = new BackgroundDef { id = id, name = id.Nice(), desc = d, skills = new[] { a, b } };
            Bg(BackgroundId.Acolyte, "You served in a temple and learned the rites of your faith.", Skill.Insight, Skill.Religion);
            Bg(BackgroundId.Charlatan, "You've always had a way with people — especially their purses.", Skill.Deception, Skill.SleightOfHand);
            Bg(BackgroundId.Criminal, "You have a history of breaking the law and knowing people who do.", Skill.Deception, Skill.Stealth);
            Bg(BackgroundId.Entertainer, "You thrive before an audience.", Skill.Acrobatics, Skill.Performance);
            Bg(BackgroundId.FolkHero, "You stood up to a tyrant once, and common folk remember it.", Skill.AnimalHandling, Skill.Survival);
            Bg(BackgroundId.GuildArtisan, "You are a member of a trade guild, skilled in a craft.", Skill.Insight, Skill.Persuasion);
            Bg(BackgroundId.Noble, "You were born to wealth and privilege — and obligation.", Skill.History, Skill.Persuasion);
            Bg(BackgroundId.Outlander, "You grew up far from towns, among wild things.", Skill.Athletics, Skill.Survival);
            Bg(BackgroundId.Sage, "You spent years studying the lore of the multiverse.", Skill.Arcana, Skill.History);
            Bg(BackgroundId.Soldier, "War was your trade, and it taught you hard lessons.", Skill.Athletics, Skill.Intimidation);
            Bg(BackgroundId.Urchin, "You grew up on the streets, alone, orphaned and poor.", Skill.SleightOfHand, Skill.Stealth);
            Bg(BackgroundId.HauntedOne, "Something terrible happened to you once, and it follows you still. The Mists feel almost familiar.", Skill.Medicine, Skill.Survival);
        }

        public static int[] SlotsFor(CasterType t, int level)
        {
            var s = new int[4];
            switch (t)
            {
                case CasterType.Full:
                    int[][] full = { new[] { 0, 0, 0 }, new[] { 2, 0, 0 }, new[] { 3, 0, 0 }, new[] { 4, 2, 0 }, new[] { 4, 3, 0 }, new[] { 4, 3, 2 } };
                    for (int i = 0; i < 3; i++) s[i + 1] = full[level][i];
                    break;
                case CasterType.Half:
                    int[][] half = { new[] { 0, 0 }, new[] { 0, 0 }, new[] { 2, 0 }, new[] { 3, 0 }, new[] { 3, 0 }, new[] { 4, 2 } };
                    for (int i = 0; i < 2; i++) s[i + 1] = half[level][i];
                    break;
            }
            return s;
        }

        public static int PactSlots(int level) => level <= 1 ? 1 : 2;
        public static int PactSlotLevel(int level) => level >= 5 ? 3 : level >= 3 ? 2 : 1;

        public static int MaxSpellLevel(ClassDef c, int level)
        {
            switch (c.caster)
            {
                case CasterType.Full: return level >= 5 ? 3 : level >= 3 ? 2 : 1;
                case CasterType.Half: return level >= 5 ? 2 : level >= 2 ? 1 : 0;
                case CasterType.Pact: return PactSlotLevel(level);
                default: return 0;
            }
        }

        public static readonly int[] PointCost = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 7, 9 };
        public const int PointBudget = 27;

        public static readonly string[] FightingStyles = { "archery", "defence", "duelling", "greatweapon", "twoweapon" };
        public static string StyleName(string id)
        {
            switch (id)
            {
                case "archery": return "Archery";
                case "defence": return "Defence";
                case "duelling": return "Duelling";
                case "greatweapon": return "Great Weapon Fighting";
                case "twoweapon": return "Two-Weapon Fighting";
                default: return id;
            }
        }
        public static string StyleDesc(string id)
        {
            switch (id)
            {
                case "archery": return "+2 to attack rolls with ranged weapons.";
                case "defence": return "+1 AC while wearing armour.";
                case "duelling": return "+2 damage with a one-handed melee weapon and nothing in the other hand.";
                case "greatweapon": return "Reroll 1s and 2s on damage dice for two-handed melee weapons.";
                case "twoweapon": return "Add your ability modifier to the damage of off-hand attacks.";
                default: return "";
            }
        }
    }
}
