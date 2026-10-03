using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.Rules
{
    /// <summary>A player character's permanent build choices. Serialised in saves.</summary>
    [Serializable]
    public class CharacterSheet
    {
        public string name = "Adventurer";
        public RaceId race = RaceId.Human;
        public SubraceId subrace = SubraceId.None;
        public ClassId cls = ClassId.Fighter;
        public string subclass;
        public BackgroundId background = BackgroundId.Soldier;
        public int level = 1;
        public int xp;
        public int[] baseAbilities = { 15, 13, 14, 10, 12, 8 };
        public Ability plus2 = Ability.STR, plus1 = Ability.CON;
        public List<Ability> asi = new List<Ability>();
        public List<Skill> classSkills = new List<Skill>();
        public List<Skill> extraSkills = new List<Skill>();
        public List<Skill> expertise = new List<Skill>();
        public string fightingStyle;
        public List<string> cantrips = new List<string>();
        public List<string> spells = new List<string>();
        public List<string> feats = new List<string>();
        public Appearance look = new Appearance();
        public int voice;
        /// <summary>A premade's own starting gear, when it differs from the class default (Arkus carries a glaive).</summary>
        public List<string> kit = new List<string>();
        public string[] StartingItems => kit != null && kit.Count > 0 ? kit.ToArray() : Class.startingItems;

        public int Score(Ability a)
        {
            int s = baseAbilities[(int)a];
            if (a == plus2) s += 2;
            if (a == plus1) s += 1;
            foreach (var x in asi) if (x == a) s++;
            return Mathf.Min(20, s);
        }

        public ClassDef Class => RulesData.Classes[cls];
        public SubclassDef Sub => string.IsNullOrEmpty(subclass) ? null : RulesData.Subclass(cls, subclass);
        public RaceDef Race => RulesData.Races[race];
        public SubraceDef Subrace => subrace == SubraceId.None ? null : RulesData.Subraces.TryGetValue(subrace, out var s) ? s : null;
        public string Title => $"{(Subrace != null ? Subrace.name : Race.name)} {Class.name}";
    }

    [Serializable]
    public class Equipment
    {
        public ItemStack main, off, ranged, armor, helmet, cloak, gloves, boots, amulet, ring1, ring2;

        public ItemStack Get(Slot s)
        {
            switch (s)
            {
                case Slot.MainHand: return main; case Slot.OffHand: return off; case Slot.Ranged: return ranged; case Slot.Armor: return armor;
                case Slot.Helmet: return helmet; case Slot.Cloak: return cloak; case Slot.Gloves: return gloves; case Slot.Boots: return boots;
                case Slot.Amulet: return amulet; case Slot.Ring1: return ring1; case Slot.Ring2: return ring2; default: return null;
            }
        }

        public void Set(Slot s, ItemStack v)
        {
            switch (s)
            {
                case Slot.MainHand: main = v; break; case Slot.OffHand: off = v; break; case Slot.Ranged: ranged = v; break; case Slot.Armor: armor = v; break;
                case Slot.Helmet: helmet = v; break; case Slot.Cloak: cloak = v; break; case Slot.Gloves: gloves = v; break; case Slot.Boots: boots = v; break;
                case Slot.Amulet: amulet = v; break; case Slot.Ring1: ring1 = v; break; case Slot.Ring2: ring2 = v; break;
            }
        }

        public IEnumerable<ItemDef> Worn()
        {
            foreach (var s in new[] { main, off, ranged, armor, helmet, cloak, gloves, boots, amulet, ring1, ring2 })
                if (s != null && s.Def != null) yield return s.Def;
        }

        public static readonly Slot[] AllSlots = { Slot.MainHand, Slot.OffHand, Slot.Ranged, Slot.Armor, Slot.Helmet, Slot.Cloak, Slot.Gloves, Slot.Boots, Slot.Amulet, Slot.Ring1, Slot.Ring2 };

        public static Slot SlotFor(ItemDef d, Equipment e)
        {
            switch (d.kind)
            {
                case ItemKind.Weapon: return d.IsRanged ? Slot.Ranged : Slot.MainHand;
                case ItemKind.Armor: return Slot.Armor;
                case ItemKind.Shield: return Slot.OffHand;
                case ItemKind.Helmet: return Slot.Helmet;
                case ItemKind.Cloak: return Slot.Cloak;
                case ItemKind.Gloves: return Slot.Gloves;
                case ItemKind.Boots: return Slot.Boots;
                case ItemKind.Amulet: return Slot.Amulet;
                case ItemKind.Ring: return e.ring1 == null ? Slot.Ring1 : Slot.Ring2;
                default: return Slot.None;
            }
        }
    }

    /// <summary>Runtime state of any combatant: PCs, NPCs and monsters.</summary>
    public class Creature
    {
        public string uid = Guid.NewGuid().ToString("N").Substring(0, 10);
        public string name;
        public bool isPC;
        public Faction faction = Faction.Hostile;
        public CreatureType type = CreatureType.Humanoid;
        public SizeCat size = SizeCat.Medium;
        public CharacterSheet sheet;
        public MonsterDef mdef;
        public int level = 1;
        public int[] abil = { 10, 10, 10, 10, 10, 10 };
        public int maxHP = 10, hp = 10, tempHP, maxHPBonus, maxHPPenalty;
        /// <summary>Set by radiant damage: regeneration is suppressed at the start of the creature's next turn.</summary>
        [System.NonSerialized] public bool noRegenThisRound;
        public int baseAC = 10;
        public float baseSpeed = 9f;
        public HashSet<Skill> skillProf = new HashSet<Skill>(), expertise = new HashSet<Skill>();
        public HashSet<Ability> saveProf = new HashSet<Ability>();
        public HashSet<DamageType> resist = new HashSet<DamageType>(), immune = new HashSet<DamageType>(), vuln = new HashSet<DamageType>();
        public HashSet<Cond> condImmune = new HashSet<Cond>();
        public List<CondInstance> conds = new List<CondInstance>();
        public Equipment eq = new Equipment();
        public int[] slotsMax = new int[4], slotsUsed = new int[4];
        public int pactMax, pactUsed, pactLevel;
        public Dictionary<string, int> resMax = new Dictionary<string, int>(), resUsed = new Dictionary<string, int>();
        public Dictionary<string, int> uses = new Dictionary<string, int>();
        public List<string> knownSpells = new List<string>();
        public List<string> extraActions = new List<string>();  // granted temporarily (spiritual weapon)
        public int xpValue;
        public bool relentlessUsed;
        public int strDrain;
        public bool vigilance;

        // combat state
        public int initiative;
        public bool hasAction, hasBonus, hasReaction;
        public float moveLeft;
        public int actionsExtra;             // action surge / haste
        public int attacksLeft;              // extra attack follow-ups this turn
        public bool sneakUsed, colossusUsed, frenzyUsed, attackedThisTurn, usedMonkAttack;
        public bool actedThisCombat;
        public int deathSucc, deathFail;
        public bool dead;
        public string concSpell;
        public List<(Creature target, Cond cond)> concLinks = new List<(Creature, Cond)>();
        public string pendingMeta;           // "quicken" / "twin"
        public bool destructiveWrath;
        public int spiritualWeaponTurns;
        public bool pactWeapon;
        public Creature wildshapeBackup;     // original stats while in beast form
        public string wildForm;
        public Actor actor;

        public bool Alive => !dead;
        public bool Downed => !dead && hp <= 0 && isPC;
        public bool Active => !dead && hp > 0;

        // ------------------------------------------------------------------ derived stats
        public static int ModOf(int score) => Mathf.FloorToInt((score - 10) / 2f);
        public int Score(Ability a) => Mathf.Max(1, abil[(int)a] - (a == Ability.STR ? strDrain : 0));
        public int Mod(Ability a) => ModOf(Score(a));
        public int Prof => isPC ? RulesData.ProfBonus(level) : (mdef != null ? mdef.prof : 2);
        public bool Has(Cond c) => conds.Any(x => x.id == c);
        public CondInstance Get(Cond c) => conds.FirstOrDefault(x => x.id == c);
        public ClassId? Cls => sheet?.cls;
        public string Sub => sheet?.subclass;
        public bool IsClass(ClassId c) => sheet != null && sheet.cls == c;
        public bool IsSub(string s) => sheet != null && sheet.subclass == s;
        public RaceDef Race => sheet?.Race;
        public SubraceDef Subrace => sheet?.Subrace;

        public ItemDef MainWeapon => eq.main?.Def ?? Items.Get("unarmed");
        public ItemDef OffWeapon => eq.off?.Def != null && eq.off.Def.kind == ItemKind.Weapon ? eq.off.Def : null;
        public ItemDef RangedWeapon => eq.ranged?.Def;
        public bool HasShield => eq.off?.Def != null && eq.off.Def.kind == ItemKind.Shield;
        public ItemDef Armor => eq.armor?.Def;
        public bool WearingArmor => Armor != null && Armor.acat != ArmorCat.None;

        public int AC
        {
            get
            {
                int dex = Mod(Ability.DEX);
                int ac;
                if (mdef != null && !isPC && wildshapeBackup == null) ac = baseAC;
                else if (wildshapeBackup != null) ac = baseAC;
                else if (WearingArmor)
                {
                    var a = Armor;
                    ac = a.baseAC + Mathf.Min(dex, a.acat == ArmorCat.Heavy ? 0 : a.dexCap);
                    if (sheet != null && sheet.fightingStyle == "defence") ac += 1;
                }
                else
                {
                    ac = 10 + dex;
                    if (IsClass(ClassId.Barbarian)) ac = Mathf.Max(ac, 10 + dex + Mod(Ability.CON));
                    if (IsClass(ClassId.Monk) && !HasShield) ac = Mathf.Max(ac, 10 + dex + Mod(Ability.WIS));
                    if (IsSub("draconic")) ac = Mathf.Max(ac, 13 + dex);
                    if (Has(Cond.MageArmor)) ac = Mathf.Max(ac, 13 + dex);
                }
                if (HasShield) ac += eq.off.Def.baseAC + eq.off.Def.magic;
                foreach (var d in eq.Worn()) ac += d.acBonus;
                if (Armor != null) ac += Armor.magic;
                foreach (var c in conds) ac += Conditions.Get(c.id).acBonus;
                return ac;
            }
        }

        public float Speed
        {
            get
            {
                float s = baseSpeed;
                if (sheet != null)
                {
                    if (Subrace != null) s += Subrace.speedBonus;
                    if (IsClass(ClassId.Barbarian) && level >= 5 && !(Armor != null && Armor.acat == ArmorCat.Heavy)) s += 3f;
                    if (IsClass(ClassId.Monk) && level >= 2 && !WearingArmor && !HasShield) s += 3f;
                    if (IsSub("gloomstalker")) s += 3f;
                    if (eq.boots?.id == "boots_striding") s += 1.5f;
                }
                foreach (var c in conds) s *= Conditions.Get(c.id).speedMult;
                if (conds.Any(c => Conditions.Get(c.id).noMove)) return 0;
                return s;
            }
        }

        public int MaxHPTotal => Mathf.Max(1, maxHP + maxHPBonus - maxHPPenalty);

        public bool ProficientWith(ItemDef w)
        {
            if (w == null) return true;
            if (!isPC) return true;
            if (w.id == "unarmed" || pactWeapon && eq.main?.Def == w) return true;
            var c = sheet.Class; var sub = sheet.Sub;
            string fam = w.family ?? w.id;
            if (w.wcat == WeaponCat.Simple && c.simpleWeapons) return true;
            if (w.wcat == WeaponCat.Martial && (c.martialWeapons || (sub != null && sub.martialWeapons))) return true;
            if (c.weaponFamilies.Contains(fam)) return true;
            if (Race != null && Race.weaponProfs.Contains(fam)) return true;
            if (Subrace != null && Subrace.weaponProfs.Contains(fam)) return true;
            return false;
        }

        public bool ProficientArmor(ItemDef a)
        {
            if (a == null || a.acat == ArmorCat.None || !isPC) return true;
            var c = sheet.Class; var sub = sheet.Sub;
            switch (a.acat)
            {
                case ArmorCat.Light: return c.lightArmor || Race.lightArmor || RaceGrantsArmor(ArmorCat.Light);
                case ArmorCat.Medium: return c.mediumArmor || Race.mediumArmor || (sub != null && sub.mediumArmor) || RaceGrantsArmor(ArmorCat.Medium);
                case ArmorCat.Heavy: return c.heavyArmor || (sub != null && sub.heavyArmor);
                case ArmorCat.Shield: return c.shields || Race.shields || (sub != null && sub.shields);
            }
            return true;
        }

        bool RaceGrantsArmor(ArmorCat c) => sheet.subrace == SubraceId.ShieldDwarf && (c == ArmorCat.Light || c == ArmorCat.Medium);

        /// <summary>Ability used for attacks with a weapon.</summary>
        public Ability WeaponAbility(ItemDef w)
        {
            if (w == null) return Ability.STR;
            if (pactWeapon && eq.main?.Def == w) return Ability.CHA;
            if (IsClass(ClassId.Monk) && (w.Has(WeaponProps.Monk) || w.id == "unarmed" || w.family == "shortsword") && !WearingArmor)
                return Mod(Ability.DEX) > Mod(Ability.STR) ? Ability.DEX : Ability.STR;
            if (w.IsRanged) return Ability.DEX;
            if (w.Has(WeaponProps.Finesse)) return Mod(Ability.DEX) > Mod(Ability.STR) ? Ability.DEX : Ability.STR;
            if (wildshapeBackup != null) return Ability.STR;
            return Ability.STR;
        }

        public int WeaponAttackBonus(ItemDef w)
        {
            int b = Mod(WeaponAbility(w)) + (ProficientWith(w) ? Prof : 0) + (w?.magic ?? 0);
            if (w != null && w.IsRanged && sheet?.fightingStyle == "archery") b += 2;
            if (Has(Cond.SacredWeapon)) b += Mathf.Max(1, Mod(Ability.CHA));
            foreach (var d in eq.Worn()) b += d.attackBonus;
            return b;
        }

        public Ability SpellAbility
        {
            get
            {
                if (sheet != null && sheet.Class.caster != CasterType.None) return sheet.Class.spellAbility;
                if (mdef != null) return mdef.spellAbility;
                return Ability.CHA;
            }
        }

        public int SpellMod => Mod(SpellAbility);
        int StaffBonus => eq.main?.id == "staff_ashes" ? 1 : 0;
        public int SpellAttackBonus => Prof + SpellMod + StaffBonus;
        public int SpellDC => 8 + Prof + SpellMod + StaffBonus;

        public int SaveBonus(Ability a)
        {
            int b = Mod(a) + (saveProf.Contains(a) ? Prof : 0);
            foreach (var d in eq.Worn()) b += d.saveBonus;
            return b;
        }

        public int SkillBonus(Skill s)
        {
            int b = Mod(s.Ab());
            if (expertise.Contains(s)) b += Prof * 2;
            else if (skillProf.Contains(s)) b += Prof;
            else if (IsClass(ClassId.Bard) && level >= 2) b += Prof / 2;
            if (s == Skill.SleightOfHand && eq.gloves?.id == "gloves_thievery") b += 2;
            return b;
        }

        public int PassivePerception => 10 + SkillBonus(Skill.Perception);

        public int InitiativeBonus
        {
            get
            {
                int b = Mod(Ability.DEX);
                if (IsSub("gloomstalker")) b += Mod(Ability.WIS);
                if (vigilance) b += 5;
                if (sheet != null && sheet.feats.Contains("alert")) b += 5;
                return b;
            }
        }

        public int ExtraAttacks
        {
            get
            {
                if (mdef != null && !isPC) return 0;
                if (level >= 5 && sheet != null && (sheet.cls == ClassId.Barbarian || sheet.cls == ClassId.Fighter || sheet.cls == ClassId.Monk || sheet.cls == ClassId.Paladin || sheet.cls == ClassId.Ranger)) return 1;
                return 0;
            }
        }

        public int CritThreshold => IsSub("champion") && level >= 3 ? 19 : 20;

        public int SneakDice => IsClass(ClassId.Rogue) ? (level + 1) / 2 : 0;

        public bool Resists(DamageType t)
        {
            if (resist.Contains(t)) return true;
            if (Has(Cond.Raging) && (t == DamageType.Bludgeoning || t == DamageType.Piercing || t == DamageType.Slashing)) return true;
            if (Has(Cond.Raging) && IsSub("wildheart") && t != DamageType.Psychic) return true;
            if (Has(Cond.BladeWard) && (t == DamageType.Bludgeoning || t == DamageType.Piercing || t == DamageType.Slashing)) return true;
            if (Has(Cond.Wet) && t == DamageType.Fire) return true;
            foreach (var d in eq.Worn()) if (d.resists.Contains(t)) return true;
            return false;
        }

        public int ResourceLeft(string r) => (resMax.TryGetValue(r, out var m) ? m : 0) - (resUsed.TryGetValue(r, out var u) ? u : 0);
        public void SpendResource(string r, int n) { resUsed.TryGetValue(r, out var u); resUsed[r] = u + n; }
        public int UsesLeft(ActionDef a) => a.maxUses <= 0 ? 99 : a.maxUses - (uses.TryGetValue(a.id, out var u) ? u : 0);
        public void SpendUse(ActionDef a) { uses.TryGetValue(a.id, out var u); uses[a.id] = u + 1; }

        public int SlotsLeft(int lvl) => lvl >= 1 && lvl <= 3 ? slotsMax[lvl] - slotsUsed[lvl] : 0;
        public bool HasAnySlot(int minLevel)
        {
            for (int l = minLevel; l <= 3; l++) if (SlotsLeft(l) > 0) return true;
            return pactMax - pactUsed > 0 && pactLevel >= minLevel;
        }

        public int LowestSlotFor(int spellLevel)
        {
            for (int l = spellLevel; l <= 3; l++) if (SlotsLeft(l) > 0) return l;
            if (pactMax - pactUsed > 0 && pactLevel >= spellLevel) return pactLevel;
            return -1;
        }

        // ------------------------------------------------------------------ actions
        /// <summary>Everything this creature can currently do, in hotbar order.</summary>
        public List<ActionDef> AvailableActions()
        {
            var list = new List<ActionDef>();
            if (mdef != null && !isPC)
            {
                foreach (var id in mdef.actions) { var a = ActionLibrary.Get(id); if (a != null) list.Add(a); }
                list.Add(ActionLibrary.Get("dash"));
                return list;
            }
            if (wildshapeBackup != null)
            {
                foreach (var id in mdef.actions) { var a = ActionLibrary.Get(id); if (a != null) list.Add(a); }
                list.Add(ActionLibrary.Get("revert_form"));
                list.Add(ActionLibrary.Get("dash"));
                return list;
            }
            var main = MainWeapon;
            list.Add(ActionLibrary.Get(main != null && main.id != "unarmed" ? "attack" : "unarmed_strike"));
            if (RangedWeapon != null) list.Add(ActionLibrary.Get("ranged_attack"));
            if (OffWeapon != null && OffWeapon.Has(WeaponProps.Light) && main != null && main.Has(WeaponProps.Light)) list.Add(ActionLibrary.Get("offhand_attack"));
            // weapon actions
            if (sheet.Class.martial)
            {
                var seen = new HashSet<string>();
                foreach (var w in new[] { main, RangedWeapon })
                    if (w != null && ProficientWith(w))
                        foreach (var wa in w.weaponActions)
                            if (seen.Add(wa)) { var a = ActionLibrary.Get(wa); if (a != null) list.Add(a); }
            }
            // class & subclass features up to level
            foreach (var f in sheet.Class.features.Where(f => f.level <= level))
                foreach (var id in f.actions) { var a = ActionLibrary.Get(id); if (a != null) list.Add(a); }
            var sub = sheet.Sub;
            if (sub != null)
                foreach (var f in sub.features.Where(f => f.level <= level))
                    foreach (var id in f.actions) { var a = ActionLibrary.Get(id); if (a != null) list.Add(a); }
            // cleric channel options
            if (IsClass(ClassId.Druid) && level < 2) list.RemoveAll(a => a.id.StartsWith("wild_shape"));
            // racial
            if (sheet.race == RaceId.Dragonborn)
            {
                var b = BreathAction();
                if (b != null) list.Add(b);
            }
            foreach (var id in RacialSpells()) { var a = ActionLibrary.Get(id); if (a != null && !list.Contains(a)) list.Add(a); }
            // spells
            foreach (var id in knownSpells) { var a = ActionLibrary.Get(id); if (a != null && !list.Contains(a)) list.Add(a); }
            foreach (var id in extraActions) { var a = ActionLibrary.Get(id); if (a != null && !list.Contains(a)) list.Add(a); }
            foreach (var d in eq.Worn()) if (!string.IsNullOrEmpty(d.grantsAction)) { var a = ActionLibrary.Get(d.grantsAction); if (a != null && !list.Contains(a)) list.Add(a); }
            // common
            foreach (var id in new[] { "dash", "disengage", "hide", "shove", "help", "dodge" }) list.Add(ActionLibrary.Get(id));
            return list.Where(a => a != null && !a.hidden).ToList();
        }

        public IEnumerable<string> RacialSpells()
        {
            if (sheet == null) yield break;
            var r = Race; var s = Subrace;
            foreach (var c in r.cantrips) yield return c;
            if (s != null) foreach (var c in s.cantrips) yield return c;
            if (level >= 3) { if (!string.IsNullOrEmpty(r.spellL3)) yield return r.spellL3; if (s != null && !string.IsNullOrEmpty(s.spellL3)) yield return s.spellL3; }
            if (level >= 5) { if (!string.IsNullOrEmpty(r.spellL5)) yield return r.spellL5; if (s != null && !string.IsNullOrEmpty(s.spellL5)) yield return s.spellL5; }
        }

        public bool IsRacialSpell(string id) => RacialSpells().Contains(id);

        ActionDef _breath;
        public ActionDef BreathAction()
        {
            var s = Subrace;
            if (s == null || s.breath == DamageType.None) return null;
            if (_breath == null)
            {
                _breath = ActionLibrary.Get("breath_weapon").Clone();
                _breath.dtype = s.breath;
                _breath.hasSave = true;
                _breath.saveAb = s.breath == DamageType.Poison || s.breath == DamageType.Cold ? Ability.CON : Ability.DEX;
                if (s.breathLine) { _breath.target = TargetKind.Line; _breath.radius = 9; _breath.lineWidth = 1.5f; }
                _breath.fx = s.breath == DamageType.Fire ? FxKind.Fire : s.breath == DamageType.Cold ? FxKind.Frost : s.breath == DamageType.Lightning ? FxKind.Lightning : s.breath == DamageType.Poison ? FxKind.Poison : FxKind.Acid;
                _breath.desc = $"Exhale {s.breath.ToString().ToLower()} in a {(s.breathLine ? "9m line" : "5m cone")}: {_breath.saveAb} save, 2d6 {s.breath} damage (3d6 at level 5), half on a success.";
            }
            _breath.dmg = level >= 5 ? "3d6" : "2d6";
            return _breath;
        }

        // ------------------------------------------------------------------ construction
        public static Creature FromSheet(CharacterSheet s)
        {
            var c = new Creature { isPC = true, faction = Faction.Party, sheet = s, name = s.name };
            c.Rebuild(true);
            return c;
        }

        /// <summary>Recompute everything derived from the sheet (after level up, equipment or rest).</summary>
        public void Rebuild(bool fullHeal)
        {
            var s = sheet; var cd = s.Class; var sub = s.Sub; var r = s.Race; var sr = s.Subrace;
            name = s.name;
            level = s.level;
            for (int i = 0; i < 6; i++) abil[i] = s.Score((Ability)i);
            size = r.size;
            baseSpeed = r.speedM;
            saveProf = new HashSet<Ability>(cd.saves);
            skillProf = new HashSet<Skill>(s.classSkills);
            foreach (var k in s.extraSkills) skillProf.Add(k);
            foreach (var k in RulesData.Backgrounds[s.background].skills) skillProf.Add(k);
            foreach (var k in r.skills) skillProf.Add(k);
            if (sr != null) foreach (var k in sr.skills) skillProf.Add(k);
            expertise = new HashSet<Skill>(s.expertise);
            resist = new HashSet<DamageType>(); immune = new HashSet<DamageType>(); vuln = new HashSet<DamageType>(); condImmune = new HashSet<Cond>();
            if (r.hellishResistance) resist.Add(DamageType.Fire);
            if (r.dwarvenResilience) resist.Add(DamageType.Poison);
            if (sr != null && sr.resist != DamageType.None) resist.Add(sr.resist);
            if (r.feyAncestry) condImmune.Add(Cond.Asleep);
            if (IsClass(ClassId.Paladin) && level >= 3) { }
            // hit points
            int con = ModOf(abil[(int)Ability.CON]);
            int hpv = cd.hitDie + con;
            for (int l = 2; l <= level; l++) hpv += cd.hitDie / 2 + 1 + con;
            if (sr != null && sr.extraHP) hpv += level;
            if (s.subclass == "draconic") hpv += level;
            if (s.feats.Contains("tough")) hpv += 2 * level;
            int oldMax = maxHP;
            maxHP = Mathf.Max(1, hpv);
            if (fullHeal) hp = MaxHPTotal; else hp = Mathf.Min(MaxHPTotal, hp + Mathf.Max(0, maxHP - oldMax));
            // spell slots
            slotsMax = RulesData.SlotsFor(cd.caster, level);
            if (cd.caster == CasterType.Pact) { pactMax = RulesData.PactSlots(level); pactLevel = RulesData.PactSlotLevel(level); }
            else { pactMax = 0; pactLevel = 0; }
            // resources
            resMax.Clear();
            if (IsClass(ClassId.Barbarian)) resMax["rage"] = level >= 3 ? 3 : 2;
            if (IsClass(ClassId.Monk) && level >= 2) resMax["ki"] = level;
            if ((IsClass(ClassId.Cleric) && level >= 2) || IsClass(ClassId.Paladin)) resMax["channel"] = 1;
            if (IsClass(ClassId.Sorcerer) && level >= 2) resMax["sorcery"] = level;
            if (IsSub("battlemaster") && level >= 3) resMax["superiority"] = 4;
            if (IsClass(ClassId.Bard)) resMax["inspiration"] = Mathf.Max(1, Mod(Ability.CHA));
            if (IsClass(ClassId.Paladin)) resMax["layonhands"] = 5 * level;
            if (IsClass(ClassId.Druid) && level >= 2) resMax["wildshape"] = 2;
            if (IsSub("war")) resMax["warpriest"] = Mathf.Max(1, Mod(Ability.WIS));
            // spells known = chosen + domain/subclass bonus
            knownSpells = new List<string>(s.cantrips);
            knownSpells.AddRange(s.spells);
            if (sub != null)
                foreach (var kv in sub.bonusSpells)
                    if (kv.Key <= level)
                        foreach (var sp in kv.Value) if (!knownSpells.Contains(sp)) knownSpells.Add(sp);
            knownSpells = knownSpells.Where(id => ActionLibrary.Get(id) != null).Distinct().ToList();
            type = CreatureType.Humanoid;
            if (fullHeal)
            {
                slotsUsed = new int[4]; pactUsed = 0; resUsed.Clear(); uses.Clear();
            }
        }

        public static Creature FromMonster(MonsterDef m)
        {
            var c = new Creature { isPC = false, mdef = m, name = m.name, faction = m.faction, type = m.type, size = m.size, baseAC = m.ac, baseSpeed = m.speedM, level = Mathf.Max(1, m.level), xpValue = m.xp };
            Array.Copy(m.abil, c.abil, 6);
            c.maxHP = m.hp; c.hp = m.hp;
            foreach (var a in m.saves) c.saveProf.Add(a);
            foreach (var s in m.skills) c.skillProf.Add(s);
            foreach (var d in m.resist) c.resist.Add(d);
            foreach (var d in m.immune) c.immune.Add(d);
            foreach (var d in m.vuln) c.vuln.Add(d);
            foreach (var x in m.condImmune) c.condImmune.Add(x);
            c.knownSpells = new List<string>(m.actions);
            return c;
        }

        public void AddCond(Cond id, int turns, Creature src = null, int dc = 0, Ability saveAb = Ability.WIS, bool saveEach = false, string tag = null, int value = 0)
        {
            if (condImmune.Contains(id)) return;
            var ex = Get(id);
            if (ex != null) { ex.turns = turns < 0 || ex.turns < 0 ? -1 : Mathf.Max(ex.turns, turns); ex.source = src ?? ex.source; ex.dc = Mathf.Max(ex.dc, dc); return; }
            conds.Add(new CondInstance { id = id, turns = turns, source = src, dc = dc, saveAb = saveAb, saveEachTurn = saveEach, tag = tag, value = value });
        }

        public void RemoveCond(Cond id) => conds.RemoveAll(c => c.id == id);

        public bool Incapacitated => conds.Any(c => Conditions.Get(c.id).incapacitated) || hp <= 0 || dead;
    }
}
