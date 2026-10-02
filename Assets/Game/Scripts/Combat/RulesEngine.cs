using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine.Combat
{
    public class AttackInfo
    {
        public int bonus; public int ac; public RollMode mode;
        public List<string> adv = new List<string>(), dis = new List<string>();
        public float chance;
        public int critOn = 20;
    }

    public class ActionContext
    {
        public ActionDef a;
        public Creature user;
        public List<Creature> targets = new List<Creature>();
        public Vector3 point;
        public int slot;                 // slot level used (0 for cantrips/features)
        public ItemStack item;           // consumable
        public bool free;                // out-of-combat, no action economy
        public bool racialFree;
    }

    /// <summary>All the dice and bookkeeping of the 5e rules as used by Dungine.</summary>
    public static class RulesEngine
    {
        // ---------------------------------------------------------------- helpers
        public static float Dist(Creature a, Creature b) => a.actor && b.actor ? FlatDist(a.actor.transform.position, b.actor.transform.position) - (a.actor.radius + b.actor.radius) * 0.5f : 999f;
        public static float FlatDist(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
        static int Side(Creature c) => c.faction == Faction.Party || c.faction == Faction.Ally ? 0 : c.faction == Faction.Hostile ? 1 : 2;
        /// <summary>True when a and b are on opposing sides (neutrals are hostile to no one until provoked).</summary>
        public static bool Hostile(Creature a, Creature b) { if (a == null || b == null || a == b) return false; int sa = Side(a), sb = Side(b); return sa != sb && sa != 2 && sb != 2; }
        public static bool Allied(Creature a, Creature b) => a != null && b != null && Side(a) == Side(b) && Side(a) != 2;

        public static ItemDef WeaponFor(Creature u, ActionDef a)
        {
            if (a.offHand) return u.OffWeapon;
            if (a.attack == AttackKind.RangedWeapon) return u.RangedWeapon ?? (u.MainWeapon != null && u.MainWeapon.IsRanged ? u.MainWeapon : null);
            var m = u.MainWeapon;
            if (m != null && m.IsRanged && u.eq.main != null) return m;
            return m;
        }

        public static float Range(Creature u, ActionDef a)
        {
            if (a.fixedToHit != int.MinValue && a.attack == AttackKind.RangedWeapon) return a.range;
            if (a.usesWeapon || a.attack == AttackKind.MeleeWeapon && a.fixedToHit == int.MinValue)
            {
                var w = WeaponFor(u, a);
                if (a.attack == AttackKind.RangedWeapon) return w != null ? Mathf.Min(18f, w.rangeM) : 18f;
                float reach = w != null && w.Has(WeaponProps.Reach) ? 3f : 1.5f;
                if (u.size >= SizeCat.Large) reach += 0.5f;
                return a.target == TargetKind.Cone ? a.radius : reach;
            }
            if (a.attack == AttackKind.MeleeWeapon) return 1.5f + (u.size >= SizeCat.Large ? 0.6f : 0);
            return a.range;
        }

        public static int SlotLevelFor(Creature u, ActionDef a)
        {
            if (!a.IsSpell || a.spellLevel == 0) return 0;
            return u.LowestSlotFor(a.spellLevel);
        }

        public static bool CanUse(Creature u, ActionDef a, bool inCombat, out string why)
        {
            why = null;
            if (u.Incapacitated) { why = "Incapacitated"; return false; }
            if (inCombat && a.outOfCombatOnly) { why = "Can't be used in combat"; return false; }
            if (!inCombat && a.combatOnly) { why = "Only in combat"; return false; }
            if (inCombat)
            {
                var cost = EffectiveCost(u, a);
                if (cost == ActionCost.Action && !u.hasAction && !(IsAttackAction(a) && u.attacksLeft > 0)) { why = "No action left"; return false; }
                if (cost == ActionCost.BonusAction && !u.hasBonus) { why = "No bonus action left"; return false; }
                if (cost == ActionCost.Reaction && !u.hasReaction) { why = "No reaction left"; return false; }
            }
            if (a.IsSpell)
            {
                if (u.Has(Cond.Raging)) { why = "Can't cast while raging"; return false; }
                if (u.wildshapeBackup != null) { why = "Can't cast in beast form"; return false; }
                if (u.Has(Cond.Silenced)) { why = "Silenced"; return false; }
                if (a.spellLevel > 0)
                {
                    bool racial = u.IsRacialSpell(a.id) && u.UsesLeft(RacialUseDef(a)) > 0;
                    if (!racial && u.LowestSlotFor(a.spellLevel) < 0) { why = "No spell slots"; return false; }
                }
            }
            if (a.maxUses > 0 && u.UsesLeft(a) <= 0) { why = a.recharge == Recharge.ShortRest ? "Recharges on a short rest" : "Recharges on a long rest"; return false; }
            if (!string.IsNullOrEmpty(a.resource) && u.ResourceLeft(a.resource) < a.resourceCost) { why = "Not enough " + ResourceName(a.resource); return false; }
            if (a.weaponAction && u.UsesLeft(a) <= 0) { why = "Recharges on a short rest"; return false; }
            if (a.special == "frenzy" && (!u.Has(Cond.Raging) || !u.IsSub("berserker") || u.level < 3)) { why = "Requires Rage"; return false; }
            if (a.special == "smite" && u.LowestSlotFor(1) < 0) { why = "No spell slots"; return false; }
            if (a.id == "martial_bonus" && !u.usedMonkAttack) { why = "Attack with an unarmed strike or monk weapon first"; return false; }
            if (a.id == "offhand_attack" && inCombat && !u.attackedThisTurn) { why = "Attack with your main hand first"; return false; }
            if (a.special == "lay_on_hands" && u.ResourceLeft("layonhands") <= 0) { why = "Healing pool empty"; return false; }
            if (a.usesWeapon && a.attack == AttackKind.RangedWeapon && WeaponFor(u, a) == null) { why = "No ranged weapon"; return false; }
            if (a.special != null && a.special.StartsWith("wildshape") && u.wildshapeBackup != null) { why = "Already transformed"; return false; }
            if (a.special == "consume" || (a.special != null && a.special.Contains("consume"))) { }
            return true;
        }

        public static ActionDef RacialUseDef(ActionDef a) { var d = new ActionDef { id = "racial:" + a.id, maxUses = 1 }; return d; }

        public static string ResourceName(string r)
        {
            switch (r)
            {
                case "ki": return "ki points";
                case "rage": return "rage charges";
                case "channel": return "Channel Divinity charges";
                case "sorcery": return "sorcery points";
                case "superiority": return "superiority dice";
                case "inspiration": return "Bardic Inspiration";
                case "wildshape": return "Wild Shape charges";
                case "warpriest": return "War Priest charges";
                default: return r;
            }
        }

        public static bool IsAttackAction(ActionDef a) => a.IsAttack && !a.IsSpell && a.cost == ActionCost.Action && (a.usesWeapon || a.id == "unarmed_strike" || a.fixedToHit != int.MinValue);

        public static ActionCost EffectiveCost(Creature u, ActionDef a)
        {
            if (a.IsSpell && u.pendingMeta == "quicken" && a.cost == ActionCost.Action) return ActionCost.BonusAction;
            return a.cost;
        }

        // ---------------------------------------------------------------- attacks
        public static int AttackBonus(Creature u, ActionDef a, Creature target = null)
        {
            if (a.fixedToHit != int.MinValue) return a.fixedToHit;
            if (a.attack == AttackKind.MeleeSpell || a.attack == AttackKind.RangedSpell) return u.SpellAttackBonus;
            if (a.special == "martial_arts" || a.id == "unarmed_strike")
            {
                var ab = u.IsClass(ClassId.Monk) ? (u.Mod(Ability.DEX) > u.Mod(Ability.STR) ? Ability.DEX : Ability.STR) : Ability.STR;
                return u.Mod(ab) + u.Prof;
            }
            var w = WeaponFor(u, a);
            if (w == null) return u.Mod(Ability.STR) + u.Prof;
            return u.WeaponAttackBonus(w);
        }

        public static AttackInfo AttackPreview(Creature u, Creature t, ActionDef a)
        {
            var info = new AttackInfo { bonus = AttackBonus(u, a, t), ac = t.AC, critOn = a.IsSpell ? 20 : u.CritThreshold };
            bool melee = a.attack == AttackKind.MeleeWeapon || a.attack == AttackKind.MeleeSpell;
            bool ranged = !melee;
            float d = Dist(u, t);
            foreach (var c in t.conds)
            {
                var cd = Conditions.Get(c.id);
                if (c.id == Cond.Prone) { if (melee && d <= 2.2f) info.adv.Add("Target prone"); else info.dis.Add("Target prone"); continue; }
                if (cd.attackersAdv) info.adv.Add(c.id.Nice());
                if (cd.attackersDis) info.dis.Add(c.id.Nice());
            }
            if (t.Downed) info.adv.Add("Target downed");
            foreach (var c in u.conds)
            {
                var cd = Conditions.Get(c.id);
                if (cd.attackDis) info.dis.Add(c.id.Nice());
                if (cd.attackAdv) info.adv.Add(c.id.Nice());
            }
            if (a.special == "reckless") info.adv.Add("Reckless");
            if (t.conds.Any(c => c.id == Cond.VowOfEnmity && c.source == u)) info.adv.Add("Vow of Enmity");
            if (u.mdef != null && u.mdef.packTactics && CombatManager.I.AllyAdjacent(u, t)) info.adv.Add("Pack Tactics");
            if (u.IsSub("assassin") && u.level >= 3 && !t.actedThisCombat && CombatManager.I.Active) info.adv.Add("Assassinate");
            if (ranged && CombatManager.I.HostileWithin(u, 1.6f)) info.dis.Add("Enemy in melee range");
            if (t.Has(Cond.ProtectionEvil) && (u.type == CreatureType.Undead || u.type == CreatureType.Fiend || u.type == CreatureType.Fey || u.type == CreatureType.Aberration)) info.dis.Add("Protection from Evil");
            if (u.isPC && u.Armor != null && !u.ProficientArmor(u.Armor)) info.dis.Add("Armour not proficient");
            info.mode = D20.Combine(info.adv.Count, info.dis.Count);
            info.chance = D20.Chance(info.bonus + BlessAvg(u), info.ac, info.mode, true);
            if (a.autoHit) info.chance = 1f;
            return info;
        }

        static int BlessAvg(Creature u) => (u.Has(Cond.Blessed) ? 2 : 0) - (u.Has(Cond.Baned) ? 2 : 0) + (u.Has(Cond.Inspired) ? 3 : 0);

        public static int RollBonusDice(Creature u, List<string> parts)
        {
            int b = 0;
            if (u.Has(Cond.Blessed)) { int r = Rng.D(4); b += r; parts?.Add($"Bless +{r}"); }
            if (u.Has(Cond.Baned)) { int r = Rng.D(4); b -= r; parts?.Add($"Bane -{r}"); }
            if (u.Has(Cond.Inspired)) { int r = Rng.D(u.level >= 5 ? 8 : 6); b += r; parts?.Add($"Inspiration +{r}"); u.RemoveCond(Cond.Inspired); }
            return b;
        }

        public class AttackRoll { public bool hit, crit; public D20Result d20; public int total; public AttackInfo info; public List<string> parts = new List<string>(); }

        public static AttackRoll RollAttack(Creature u, Creature t, ActionDef a, int extraBonus = 0)
        {
            var info = AttackPreview(u, t, a);
            var r = new AttackRoll { info = info };
            bool lucky = u.Race != null && u.Race.lucky;
            r.d20 = D20.Roll(info.bonus, info.mode, lucky);
            int bonusDice = RollBonusDice(u, r.parts) + extraBonus;
            if (a.special == "precision") { int p = Rng.D(8); bonusDice += p; r.parts.Add($"Precision +{p}"); }
            r.total = r.d20.total + bonusDice;
            r.crit = r.d20.natural >= info.critOn;
            if (t.conds.Any(c => Conditions.Get(c.id).meleeAutoCrit) && (a.attack == AttackKind.MeleeWeapon || a.attack == AttackKind.MeleeSpell) && Dist(u, t) <= 2.2f) r.crit = true;
            r.hit = r.d20.natural != 1 && (r.crit && r.d20.natural >= info.critOn || r.total >= info.ac);
            if (r.d20.natural == 1) { r.hit = false; r.crit = false; }
            if (!r.hit) r.crit = false;
            // Shield reaction
            if (r.hit && !r.crit && t.isPC && t.hasReaction && t.knownSpells.Contains("shield") && t.LowestSlotFor(1) >= 0 && r.total < info.ac + 5 && Settings.AutoReactions)
            {
                t.hasReaction = false;
                int lvl = t.LowestSlotFor(1); SpendSlot(t, lvl);
                t.AddCond(Cond.ShieldSpell, 1, t);
                CombatLog.Add($"{Name(t)} casts <color=#9ab0ff>Shield</color>! (+5 AC)");
                FX.Burst(t.actor ? t.actor.Chest : Vector3.zero, FxKind.Force, 1f);
                r.hit = r.total >= info.ac + 5;
            }
            return r;
        }

        // ---------------------------------------------------------------- saves
        public class SaveRoll { public bool success; public D20Result d20; public int total; public RollMode mode; public List<string> parts = new List<string>(); }

        public static SaveRoll RollSave(Creature t, Ability ab, int dc, Creature src, ActionDef a, Cond? threatened = null)
        {
            var r = new SaveRoll();
            int adv = 0, dis = 0;
            if (t.conds.Any(c => (ab == Ability.DEX && Conditions.Get(c.id).dexSaveFail) || (ab == Ability.STR && Conditions.Get(c.id).strSaveFail))) { r.success = false; r.parts.Add("Auto-fail"); return r; }
            if (t.Race != null && t.Race.gnomeCunning && (ab == Ability.INT || ab == Ability.WIS || ab == Ability.CHA) && a != null && a.IsSpell) adv++;
            if (t.Race != null && t.Race.feyAncestry && threatened == Cond.Charmed) adv++;
            if (t.Race != null && t.Race.brave && threatened == Cond.Frightened) adv++;
            if (t.Race != null && t.Race.dwarvenResilience && threatened == Cond.Poisoned) adv++;
            if (t.Subrace != null && t.Subrace.poisonResist && threatened == Cond.Poisoned) adv++;
            if (t.sheet?.subrace == SubraceId.Duergar && (threatened == Cond.Charmed || threatened == Cond.Paralyzed)) adv++;
            if (t.Has(Cond.Raging) && ab == Ability.STR) adv++;
            if (t.IsClass(ClassId.Barbarian) && t.level >= 2 && ab == Ability.DEX) adv++;
            if (t.Has(Cond.Dodging) && ab == Ability.DEX) adv++;
            if (t.Has(Cond.Dazed) && ab == Ability.WIS) dis++;
            if (t.Has(Cond.Heroism) && threatened == Cond.Frightened) { r.success = true; return r; }
            r.mode = D20.Combine(adv, dis);
            int bonus = t.SaveBonus(ab);
            r.d20 = D20.Roll(bonus, r.mode, t.Race != null && t.Race.lucky);
            int extra = RollBonusDice(t, r.parts);
            if (t.Has(Cond.Resistance)) { int x = Rng.D(4); extra += x; r.parts.Add($"Resistance +{x}"); t.RemoveCond(Cond.Resistance); }
            r.total = r.d20.total + extra;
            r.success = r.total >= dc;
            return r;
        }

        public static int SaveDC(Creature u, ActionDef a)
        {
            if (a.fixedDC > 0) return a.fixedDC;
            if (u.mdef != null && !u.isPC && a.IsSpell) return 8 + u.Prof + u.Mod(u.mdef.spellAbility);
            if (a.IsSpell || a.resource == "channel" || a.id == "fey_presence" || a.id == "turn_undead" || a.id == "radiance_of_dawn") return u.SpellDC;
            if (a.special == "breath") return 8 + u.Mod(Ability.CON) + u.Prof;
            if (a.special == "martial_arts") return 8 + u.Prof + u.Mod(Ability.WIS);
            if (a.resource == "superiority") return 8 + u.Prof + Mathf.Max(u.Mod(Ability.STR), u.Mod(Ability.DEX));
            return 8 + u.Prof + Mathf.Max(u.Mod(Ability.STR), u.Mod(Ability.DEX));
        }

        // ---------------------------------------------------------------- damage
        public struct DmgPart { public int amount; public DamageType type; public string label; }

        public static List<DmgPart> RollDamage(Creature u, Creature t, ActionDef a, bool crit, int slot, bool firstHitThisTurn)
        {
            var parts = new List<DmgPart>();
            bool gwf = false;
            void Add(string dice, DamageType type, string label, int flat = 0, bool critDoubles = true)
            {
                if (string.IsNullOrEmpty(dice) && flat == 0) return;
                var d = DiceExpr.Parse(dice ?? "0");
                int v = d.Roll(crit && critDoubles, gwf) + flat;
                if (a.special == "brace") v = Mathf.Max(v, d.Roll(crit, gwf) + flat);
                if (u.destructiveWrath && (type == DamageType.Thunder || type == DamageType.Lightning)) v = (crit ? 2 : 1) * (d.count * d.sides + d.count2 * d.sides2) + d.bonus + flat;
                parts.Add(new DmgPart { amount = Mathf.Max(0, v), type = type, label = label });
            }
            if (a.fixedToHit != int.MinValue && !a.usesWeapon)
            {
                Add(a.dmg, a.dtype, null);
                if (!string.IsNullOrEmpty(a.dmg2)) Add(a.dmg2, a.dtype2, null);
                return parts;
            }
            if (a.usesWeapon)
            {
                var w = WeaponFor(u, a);
                if (w == null) { Add("1", DamageType.Bludgeoning, null, u.Mod(Ability.STR)); return parts; }
                string dice = w.dmg;
                bool twoHandedUse = u.eq.off == null && !string.IsNullOrEmpty(w.versatile);
                if (twoHandedUse) dice = w.versatile;
                gwf = u.sheet?.fightingStyle == "greatweapon" && (w.Has(WeaponProps.TwoHanded) || twoHandedUse) && !w.IsRanged;
                int mod = a.addMod || (a.offHand && u.sheet?.fightingStyle == "twoweapon") ? u.Mod(u.WeaponAbility(w)) : Mathf.Min(0, u.Mod(u.WeaponAbility(w)));
                int flat = mod + w.magic;
                bool melee = !w.IsRanged;
                if (u.sheet?.fightingStyle == "duelling" && melee && !w.Has(WeaponProps.TwoHanded) && u.OffWeapon == null && !twoHandedUse) flat += 2;
                if (u.Has(Cond.Raging) && melee && u.WeaponAbility(w) == Ability.STR) flat += 2;
                var dexp = DiceExpr.Parse(dice);
                if (crit && melee && u.Race != null && u.Race.savageAttacks) dexp = dexp.WithExtraDice(1);
                Add(dexp.ToString(), w.dtype, null, flat);
                if (!string.IsNullOrEmpty(w.extraDmg)) Add(w.extraDmg, w.extraType, w.name);
                if (u.Has(Cond.Enlarged) && melee) Add("1d4", w.dtype, "Enlarge");
                if (!string.IsNullOrEmpty(a.dmg2)) Add(a.dmg2, w.dtype, a.name);
            }
            else if (a.special == "martial_arts" || a.id == "unarmed_strike")
            {
                string die = u.IsClass(ClassId.Monk) ? (u.level >= 5 ? "1d6" : "1d4") : "1";
                var ab = u.IsClass(ClassId.Monk) ? (u.Mod(Ability.DEX) > u.Mod(Ability.STR) ? Ability.DEX : Ability.STR) : Ability.STR;
                int flat = u.Mod(ab) + (u.Has(Cond.Raging) ? 2 : 0);
                Add(die, DamageType.Bludgeoning, null, flat);
            }
            else if (!string.IsNullOrEmpty(a.dmg))
            {
                var d = DiceExpr.Parse(a.dmg);
                if (a.cantripScales && u.level >= 5) d = new DiceExpr { count = d.count * 2, sides = d.sides, bonus = d.bonus };
                if (a.special == "toll" && t.hp < t.MaxHPTotal) d = new DiceExpr { count = d.count, sides = 12 };
                if (!string.IsNullOrEmpty(a.upcast) && slot > a.spellLevel && a.spellLevel > 0)
                {
                    var up = DiceExpr.Parse(a.upcast);
                    d.count += up.count * (slot - a.spellLevel);
                }
                int flat = 0;
                if (a.addMod) flat += u.SpellMod;
                if (a.special == "eldritch_blast" && u.IsClass(ClassId.Warlock) && u.level >= 2) flat += u.Mod(Ability.CHA);
                if (a.special == "radiance") flat += u.level;
                Add(d.ToString(), a.dtype, null, flat);
                if (!string.IsNullOrEmpty(a.dmg2)) Add(a.dmg2, a.dtype2, null);
            }
            // riders on weapon hits
            bool weaponHit = a.usesWeapon || a.special == "martial_arts" || a.id == "unarmed_strike";
            if (weaponHit || a.attack == AttackKind.RangedSpell || a.attack == AttackKind.MeleeSpell)
            {
                if (t.conds.Any(c => c.id == Cond.Hexed && c.source == u)) Add("1d6", DamageType.Necrotic, "Hex");
            }
            if (weaponHit)
            {
                var w = WeaponFor(u, a);
                if (t.conds.Any(c => c.id == Cond.HuntersMark && c.source == u)) Add("1d6", w?.dtype ?? DamageType.Piercing, "Hunter's Mark");
                if (u.Has(Cond.DivineFavor)) Add("1d4", DamageType.Radiant, "Divine Favour");
                if (t.Has(Cond.GapingWound)) Add("1d4", w?.dtype ?? DamageType.Piercing, "Gaping Wound");
                // sneak attack
                if (u.SneakDice > 0 && !u.sneakUsed && w != null && (w.Has(WeaponProps.Finesse) || w.IsRanged))
                {
                    var pv = AttackPreview(u, t, a);
                    if ((pv.mode == RollMode.Advantage || CombatManager.I.AllyAdjacent(u, t)) && pv.mode != RollMode.Disadvantage)
                    {
                        u.sneakUsed = true;
                        var sd = DiceExpr.Parse($"{u.SneakDice}d6");
                        int sv = sd.Roll(crit);
                        parts.Add(new DmgPart { amount = sv, type = w.dtype, label = "Sneak Attack" });
                    }
                }
                if (u.IsSub("hunter") && u.level >= 3 && !u.colossusUsed && t.hp < t.MaxHPTotal) { u.colossusUsed = true; Add("1d8", w?.dtype ?? DamageType.Piercing, "Colossus Slayer"); }
                if (u.IsSub("gloomstalker") && u.level >= 3 && CombatManager.I.Round == 1 && firstHitThisTurn) Add("1d8", w?.dtype ?? DamageType.Piercing, "Dread Ambusher");
                if (a.special == "smite")
                {
                    int lvl = Mathf.Max(1, slot);
                    int dice = 1 + lvl + (t.type == CreatureType.Undead || t.type == CreatureType.Fiend ? 1 : 0);
                    Add($"{dice}d8", DamageType.Radiant, "Divine Smite");
                }
            }
            return parts;
        }

        public static int Apply(Creature src, Creature t, List<DmgPart> parts, bool crit, ActionDef a, float mult = 1f)
        {
            int total = 0;
            foreach (var p in parts) total += DealDamage(src, t, Mathf.Max(0, Mathf.FloorToInt(p.amount * mult)), p.type, crit, a, p.label, false);
            ShowDamage(t, total, parts.Count > 0 ? parts[0].type : DamageType.None, crit);
            AfterDamaged(src, t, total, a);
            return total;
        }

        static readonly Dictionary<Creature, float> lastFloat = new Dictionary<Creature, float>();

        static void ShowDamage(Creature t, int total, DamageType type, bool crit)
        {
            if (t.actor == null) return;
            FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.3f, crit ? $"{total}!" : total.ToString(), DamageColor(type), crit ? 1.5f : 1.1f);
        }

        public static Color DamageColor(DamageType t)
        {
            switch (t)
            {
                case DamageType.Fire: return new Color(1f, .55f, .2f);
                case DamageType.Cold: return new Color(.55f, .85f, 1f);
                case DamageType.Lightning: return new Color(.6f, .7f, 1f);
                case DamageType.Thunder: return new Color(.7f, .7f, .95f);
                case DamageType.Acid: return new Color(.6f, 1f, .3f);
                case DamageType.Poison: return new Color(.45f, .85f, .35f);
                case DamageType.Necrotic: return new Color(.55f, .9f, .6f);
                case DamageType.Radiant: return new Color(1f, .92f, .5f);
                case DamageType.Force: return new Color(.8f, .6f, 1f);
                case DamageType.Psychic: return new Color(1f, .5f, .85f);
                default: return new Color(1f, .9f, .85f);
            }
        }

        public static string Name(Creature c) => c.isPC ? $"<color=#e6c67a>{c.name}</color>" : c.faction == Faction.Hostile ? $"<color=#e87a6a>{c.name}</color>" : $"<color=#d8d0a0>{c.name}</color>";

        /// <summary>Deal damage of one type. Returns damage actually applied to hit points (after resistances).</summary>
        public static int DealDamage(Creature src, Creature t, int amount, DamageType type, bool crit, ActionDef a, string label = null, bool show = true)
        {
            if (t.dead || amount <= 0) return 0;
            if (type == DamageType.Radiant || type == DamageType.Fire && t.mdef != null && t.mdef.id == "lorghoth") t.noRegenThisRound = true;
            if (t.immune.Contains(type)) { if (show) FloatingText.Show(t.actor ? t.actor.HeadPos : Vector3.zero, "Immune", Theme.TextDim); return 0; }
            bool magical = a != null && (a.IsSpell || (a.usesWeapon && src != null && (WeaponFor(src, a)?.magic ?? 0) > 0));
            if ((t.Resists(type) && !(t.mdef != null && t.mdef.incorporeal && magical && (type == DamageType.Bludgeoning || type == DamageType.Piercing || type == DamageType.Slashing) && false))) amount /= 2;
            if (t.vuln.Contains(type)) amount *= 2;
            if (t.Has(Cond.Wet) && (type == DamageType.Cold || type == DamageType.Lightning)) amount *= 2;
            // uncanny dodge
            if (a != null && a.IsAttack && t.IsClass(ClassId.Rogue) && t.level >= 5 && t.hasReaction && amount >= 6 && CombatManager.I.Active && Settings.AutoReactions)
            {
                t.hasReaction = false; amount /= 2;
                CombatLog.Add($"{Name(t)} uses <color=#c8c8c8>Uncanny Dodge</color>.");
            }
            int absorbed = 0;
            if (t.tempHP > 0) { absorbed = Mathf.Min(t.tempHP, amount); t.tempHP -= absorbed; amount -= absorbed; }
            // Armour of Agathys retaliation
            if (absorbed > 0 && t.Has(Cond.ArmorOfAgathys) && src != null && a != null && (a.attack == AttackKind.MeleeWeapon || a.attack == AttackKind.MeleeSpell))
            {
                var aa = t.Get(Cond.ArmorOfAgathys);
                int cold = 5;
                CombatLog.Add($"Frost lashes back at {Name(src)}.");
                DealDamage(t, src, cold, DamageType.Cold, false, null);
            }
            if (t.tempHP <= 0) t.RemoveCond(Cond.ArmorOfAgathys);
            if (amount <= 0) { if (show) FloatingText.Show(t.actor ? t.actor.HeadPos : Vector3.zero, "Absorbed", Theme.Magic); return 0; }
            bool wasDown = t.hp <= 0;
            if (wasDown && t.isPC)
            {
                t.deathFail += crit ? 2 : 1;
                CombatLog.Add($"{Name(t)} takes damage while down — death save failure!");
                if (t.deathFail >= 3) Kill(t, src, a);
                return amount;
            }
            t.hp -= amount;
            if (show) ShowDamage(t, amount, type, crit);
            CombatLog.Add($"{Name(t)} takes <b>{amount}</b> <color={Theme.Hex(DamageColor(type))}>{type.Nice()}</color> damage" + (label != null ? $" ({label})" : "") + (src != null ? $" from {Name(src)}." : "."));
            if (t.Has(Cond.Asleep)) { t.RemoveCond(Cond.Asleep); CombatLog.Add($"{Name(t)} wakes up!"); }
            // concentration
            if (!string.IsNullOrEmpty(t.concSpell) && t.hp > 0)
            {
                int dc = Mathf.Max(10, amount / 2);
                var sr = RollSave(t, Ability.CON, dc, src, null);
                if (!sr.success) { CombatLog.Add($"{Name(t)} loses concentration on {ActionLibrary.Get(t.concSpell)?.name}."); EndConcentration(t); }
            }
            // laughter repeat save when damaged
            if (t.Has(Cond.Laughing) && t.hp > 0)
            {
                var ci = t.Get(Cond.Laughing);
                if (RollSave(t, Ability.WIS, ci.dc, src, null).success) { t.RemoveCond(Cond.Laughing); t.RemoveCond(Cond.Prone); }
            }
            if (t.hp <= 0)
            {
                // undead fortitude
                if (t.mdef != null && t.mdef.undeadFortitude && type != DamageType.Radiant && !crit)
                {
                    var sr = RollSave(t, Ability.CON, 5 + amount, src, null);
                    if (sr.success) { t.hp = 1; CombatLog.Add($"{Name(t)} refuses to fall! (Undead Fortitude)"); FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.6f, "Undead Fortitude", Theme.Neutral); return amount; }
                }
                if (t.isPC && t.Race != null && t.Race.relentless && !t.relentlessUsed && t.wildshapeBackup == null)
                {
                    t.relentlessUsed = true; t.hp = 1;
                    CombatLog.Add($"{Name(t)} endures! (Relentless Endurance)");
                    FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.6f, "Relentless Endurance", Theme.Gold);
                    return amount;
                }
                if (t.wildshapeBackup != null)
                {
                    int over = -t.hp;
                    Wildshape.Revert(t);
                    if (over > 0) return amount + DealDamage(src, t, over, type, false, a, label, show);
                    return amount;
                }
                if (t.isPC)
                {
                    int over = -t.hp;
                    t.hp = 0;
                    if (over >= t.MaxHPTotal) { Kill(t, src, a); return amount; }
                    t.deathSucc = 0; t.deathFail = 0;
                    t.AddCond(Cond.Downed, -1);
                    EndConcentration(t);
                    CombatLog.Add($"{Name(t)} is <color=#ff6a5a>downed</color>!");
                    t.actor?.RefreshLife();
                    Audio.Sfx.Play("downed");
                    Game.I.CheckGameOver();
                }
                else Kill(t, src, a);
            }
            else if (t.actor != null && t.actor.anim != null && !t.actor.anim.Busy) t.actor.anim.Play(AnimAct.Hit);
            return amount;
        }

        static void AfterDamaged(Creature src, Creature t, int total, ActionDef a)
        {
            if (total <= 0 || src == null || t.dead) return;
            // Hellish Rebuke reaction
            if (t.isPC && t.hp > 0 && t.hasReaction && Settings.AutoReactions && CombatManager.I.Active && src.Active && Hostile(t, src))
            {
                bool knows = t.knownSpells.Contains("hellish_rebuke") || t.IsRacialSpell("hellish_rebuke");
                if (knows)
                {
                    var hr = ActionLibrary.Get("hellish_rebuke");
                    bool racial = t.IsRacialSpell("hellish_rebuke") && t.UsesLeft(RacialUseDef(hr)) > 0;
                    int slot = racial ? 1 : t.LowestSlotFor(1);
                    if (slot >= 0)
                    {
                        t.hasReaction = false;
                        if (racial) t.SpendUse(RacialUseDef(hr)); else SpendSlot(t, slot);
                        CombatLog.Add($"{Name(t)} retaliates with <color=#ff9050>Hellish Rebuke</color>!");
                        FX.Burst(src.actor ? src.actor.Chest : Vector3.zero, FxKind.Fire, 1.3f);
                        var sv = RollSave(src, Ability.DEX, t.SpellDC, t, hr);
                        var dmg = DiceExpr.Parse($"{1 + slot}d10").Roll();
                        DealDamage(t, src, sv.success ? dmg / 2 : dmg, DamageType.Fire, false, hr);
                    }
                }
            }
        }

        public static void Kill(Creature t, Creature src, ActionDef a)
        {
            if (t.dead) return;
            t.hp = Mathf.Min(t.hp, 0);
            t.dead = true;
            t.conds.Clear();
            t.AddCond(Cond.Dead, -1);
            EndConcentration(t);
            CombatLog.Add($"{Name(t)} {(t.isPC ? "has <color=#ff4040>died</color>" : "is slain")}.");
            t.actor?.RefreshLife();
            if (t.actor && t.actor.agent) t.actor.agent.enabled = false;
            if (!t.isPC && t.actor != null) Game.I.SetFlag("dead:" + t.actor.npcId, 1);
            if (!t.isPC && t.faction == Faction.Hostile) Game.I.AddFlag("stat_kills");
            if (t.isPC) Game.I.AddFlag("stat_deaths");
            Audio.Sfx.Play(t.isPC ? "death_pc" : "death");
            // on-kill features
            if (src != null && src.isPC && Hostile(src, t))
            {
                if (src.IsSub("fiend") && src.IsClass(ClassId.Warlock)) { int thp = Mathf.Max(1, src.Mod(Ability.CHA) + src.level); src.tempHP = Mathf.Max(src.tempHP, thp); FloatingText.Show(src.actor.HeadPos, $"+{thp} temp HP", Theme.Magic); }
                if (src.IsSub("necromancy") && src.level >= 2 && a != null && a.IsSpell && a.spellLevel > 0) { int h = a.spellLevel * (a.school == School.Necromancy ? 3 : 2); Heal(src, src, h, null); }
            }
            if (t.isPC) Game.I.CheckGameOver();
            CombatManager.I.OnDeath(t);
        }

        public static void Heal(Creature src, Creature t, int amount, ActionDef a)
        {
            if (t.dead) return;
            if (src != null && src.IsSub("life") && a != null && a.IsSpell && a.spellLevel > 0) amount += 2 + a.spellLevel;
            bool wasDown = t.hp <= 0;
            int before = t.hp;
            t.hp = Mathf.Min(t.MaxHPTotal, Mathf.Max(0, t.hp) + amount);
            int healed = t.hp - Mathf.Max(0, before);
            if (t.actor) FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.2f, $"+{healed}", Theme.Friendly, 1.1f);
            CombatLog.Add($"{Name(t)} regains <color=#70e090>{healed}</color> hit points.");
            if (wasDown && t.hp > 0) Revive(t, t.hp);
        }

        public static void Revive(Creature t, int hp)
        {
            t.dead = false;
            t.hp = Mathf.Max(t.hp, hp);
            t.RemoveCond(Cond.Downed); t.RemoveCond(Cond.Stabilized); t.RemoveCond(Cond.Dead);
            t.deathFail = 0; t.deathSucc = 0;
            t.actor?.RefreshLife();
            Game.I.NotifyPartyChanged();
        }

        public static void EndConcentration(Creature u)
        {
            if (string.IsNullOrEmpty(u.concSpell)) return;
            foreach (var (tg, cond) in u.concLinks)
            {
                if (tg == null) continue;
                if (cond == Cond.Hasted && tg.Has(Cond.Hasted)) { tg.RemoveCond(Cond.Hasted); tg.AddCond(Cond.Lethargic, 1); }
                else tg.conds.RemoveAll(c => c.id == cond && c.source == u);
            }
            u.concLinks.Clear();
            u.concSpell = null;
            u.RemoveCond(Cond.Concentrating);
            u.RemoveCond(Cond.SpiritGuardians);
            u.RemoveCond(Cond.DivineFavor);
            FX.I?.StopAura(u);
        }

        public static void SpendSlot(Creature u, int lvl)
        {
            if (u.pactMax > 0 && lvl == u.pactLevel && u.pactUsed < u.pactMax && u.SlotsLeft(lvl) <= 0) { u.pactUsed++; return; }
            if (lvl >= 1 && lvl <= 3 && u.SlotsLeft(lvl) > 0) { u.slotsUsed[lvl]++; return; }
            if (u.pactMax > 0 && u.pactUsed < u.pactMax) u.pactUsed++;
        }

        public static void Push(Creature src, Creature t, float meters)
        {
            if (t.actor == null || src.actor == null || t.size >= SizeCat.Large && meters > 0) return;
            Vector3 dir = t.actor.transform.position - src.actor.transform.position; dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) dir = src.actor.transform.forward;
            dir.Normalize();
            Vector3 dest = t.actor.transform.position + dir * meters;
            if (NavMesh.Raycast(t.actor.transform.position, dest, out var hit, NavMesh.AllAreas)) dest = hit.position;
            CombatManager.I.StartCoroutine(Slide(t.actor, dest, 0.25f));
        }

        static IEnumerator Slide(Actor a, Vector3 dest, float dur)
        {
            Vector3 s = a.transform.position; float t = 0;
            if (a.agent && a.agent.enabled) a.agent.updatePosition = false;
            while (t < dur && a) { t += Time.deltaTime; a.transform.position = Vector3.Lerp(s, dest, MathX.Smoothstep(0, 1, t / dur)); if (a.agent && a.agent.enabled) a.agent.nextPosition = a.transform.position; yield return null; }
            if (a && a.agent && a.agent.enabled) { a.agent.Warp(dest); a.agent.updatePosition = true; }
        }

        public static void ApplyConds(ActionDef a, Creature u, Creature t, bool failedSave, int dc)
        {
            foreach (var ca in a.conds)
            {
                if (ca.onlyOnFail && a.hasSave && !failedSave) continue;
                if (t.condImmune.Contains(ca.cond)) { FloatingText.Show(t.actor ? t.actor.HeadPos : Vector3.zero, "Immune", Theme.TextDim); continue; }
                if (ca.cond == Cond.Paralyzed && a.fixedToHit != int.MinValue && t.Race != null && t.Race.feyAncestry) { FloatingText.Show(t.actor.HeadPos, "Immune", Theme.TextDim); continue; }
                if (ca.cond == Cond.Asleep && t.Race != null && t.Race.feyAncestry) continue;
                t.AddCond(ca.cond, ca.turns, u, dc, a.saveAb, ca.saveEachTurn, a.id);
                if (ca.cond == Cond.Laughing) t.AddCond(Cond.Prone, ca.turns, u);
                if (a.concentration) u.concLinks.Add((t, ca.cond));
                if (t.actor) FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.55f, ca.cond.Nice(), Conditions.Get(ca.cond).color, 0.85f);
                CombatLog.Add($"{Name(t)} is now <color={Theme.Hex(Conditions.Get(ca.cond).color)}>{ca.cond.Nice()}</color>.");
                if (ca.cond == Cond.Prone && t.actor?.anim is HumanoidAnimator) { }
            }
        }

        // ---------------------------------------------------------------- resource payment
        public static void Pay(ActionContext ctx, bool inCombat)
        {
            var u = ctx.user; var a = ctx.a;
            if (inCombat && !ctx.free)
            {
                var cost = EffectiveCost(u, a);
                if (cost == ActionCost.Action)
                {
                    if (IsAttackAction(a) && !u.hasAction && u.attacksLeft > 0) u.attacksLeft--;
                    else { u.hasAction = false; if (IsAttackAction(a)) u.attacksLeft = u.ExtraAttacks; }
                }
                else if (cost == ActionCost.BonusAction) u.hasBonus = false;
                else if (cost == ActionCost.Reaction) u.hasReaction = false;
                if (a.IsSpell && u.pendingMeta == "quicken" && a.cost == ActionCost.Action) { u.pendingMeta = null; }
            }
            if (a.IsSpell && a.spellLevel > 0)
            {
                bool racial = u.IsRacialSpell(a.id) && u.UsesLeft(RacialUseDef(a)) > 0 && (u.LowestSlotFor(a.spellLevel) < 0 || !u.knownSpells.Contains(a.id));
                if (racial) { u.SpendUse(RacialUseDef(a)); ctx.slot = Mathf.Max(a.spellLevel, 1); ctx.racialFree = true; }
                else { if (ctx.slot <= 0) ctx.slot = u.LowestSlotFor(a.spellLevel); SpendSlot(u, ctx.slot); }
            }
            if (a.maxUses > 0) u.SpendUse(a);
            if (a.weaponAction) u.SpendUse(a);
            if (!string.IsNullOrEmpty(a.resource)) u.SpendResource(a.resource, a.resourceCost);
            if (a.special == "smite") { ctx.slot = u.LowestSlotFor(1); SpendSlot(u, ctx.slot); }
            if (ctx.item != null) { Game.I.stash.Remove(ctx.item.id, 1); }
            if (a.IsAttack || (a.IsSpell && a.spellLevel >= 0 && a.who == Who.Enemy)) { u.RemoveCond(Cond.Invisible); u.RemoveCond(Cond.Sanctuary); }
            if (a.concentration && !string.IsNullOrEmpty(u.concSpell)) EndConcentration(u);
            if (a.concentration) { u.concSpell = a.id; u.AddCond(Cond.Concentrating, -1, u); }
        }

        // ---------------------------------------------------------------- execution
        public static IEnumerator Execute(ActionContext ctx, bool inCombat)
        {
            var u = ctx.user; var a = ctx.a;
            if (u.actor == null) yield break;
            Pay(ctx, inCombat);
            string castLabel = a.IsSpell ? $"casts <color={Theme.Hex(a.color)}>{a.name}</color>" : $"uses <color={Theme.Hex(a.color)}>{a.name}</color>";
            CombatLog.Add($"{Name(u)} {castLabel}" + (ctx.targets.Count == 1 && ctx.targets[0] != u ? $" on {Name(ctx.targets[0])}." : "."));
            if (a.IsSpell) FloatingText.Show(u.actor.HeadPos + Vector3.up * 0.4f, a.name, a.color, 0.9f);
            // face target
            Vector3 aim = ctx.targets.Count > 0 && ctx.targets[0] != u && ctx.targets[0].actor ? ctx.targets[0].actor.transform.position : ctx.point;
            if (aim != Vector3.zero && (aim - u.actor.transform.position).sqrMagnitude > 0.01f) u.actor.Face(aim, true);

            // special self-contained actions
            if (a.special != null && Specials.TryHandle(ctx, inCombat, out var routine))
            {
                if (routine != null) yield return routine;
                yield return new WaitForSeconds(0.2f);
                yield break;
            }

            // wind up
            bool impact = false;
            var anim = a.anim;
            if (a.usesWeapon && a.attack == AttackKind.RangedWeapon)
            {
                var w = WeaponFor(u, a);
                anim = w != null && HumanoidBuilder.IsCrossbow(w.visual) ? AnimAct.Crossbow : AnimAct.Bow;
            }
            if (a.IsSpell && a.fx != FxKind.None) FX.I.CastGlow(u.actor, a.color, 0.9f);
            if (anim != AnimAct.None) u.actor.PlayAnim(anim, () => impact = true); else impact = true;
            float guard = 0;
            while (!impact && guard < 3f) { guard += Time.deltaTime; yield return null; }

            int hits = Mathf.Max(1, a.multi);
            // AoE targets
            if (a.IsAoE)
            {
                if (a.proj != ProjKind.None && a.target == TargetKind.Point) yield return FX.I.Projectile(u.actor.Chest + u.actor.transform.forward * 0.4f, ctx.point + Vector3.up * 0.5f, a.proj, a.color);
                FX.I.AoE(a, u.actor.transform.position, ctx.point, u.actor.transform.forward);
                var victims = Targeting.CollectArea(a, u, ctx.point);
                if (a.target == TargetKind.Aura && a.who == Who.Ally) victims = victims.Where(v => !Hostile(u, v)).ToList();
                if (u.IsSub("evocation") && u.level >= 2 && a.IsSpell) victims = victims.Where(v => Hostile(u, v) || v == u && false).ToList();
                if (a.special == "sleep") { Specials.Sleep(ctx, victims); yield return new WaitForSeconds(0.6f); yield break; }
                if (a.special == "turn_undead") { Specials.TurnUndead(ctx, victims); yield return new WaitForSeconds(0.6f); yield break; }
                if (a.special == "preserve_life") { Specials.PreserveLife(ctx, victims); yield return new WaitForSeconds(0.6f); yield break; }
                if (a.usesWeapon)
                {
                    foreach (var v in victims.Where(v => Hostile(u, v)).Take(3)) ResolveAttack(ctx, v, true);
                }
                else
                {
                    int dc = SaveDC(u, a);
                    var dmg = !string.IsNullOrEmpty(a.dmg) ? RollDamage(u, victims.FirstOrDefault() ?? u, a, false, ctx.slot, false) : null;
                    foreach (var v in victims)
                    {
                        if (a.onlyVs != null && !a.onlyVs.Contains(v.type)) continue;
                        if (!string.IsNullOrEmpty(a.heal)) { DoHeal(ctx, v); continue; }
                        bool failed = true;
                        if (a.hasSave)
                        {
                            var sr = RollSave(v, a.saveAb, dc, u, a, a.conds.Count > 0 ? a.conds[0].cond : (Cond?)null);
                            failed = !sr.success;
                            CombatLog.Add($"{Name(v)} {(failed ? "<color=#e87a6a>fails</color>" : "<color=#70e090>succeeds</color>")} a {a.saveAb} save ({sr.total} vs DC {dc}).");
                            if (!failed && v.actor) FloatingText.Show(v.actor.HeadPos + Vector3.up * 0.5f, "Saved", Theme.TextDim, 0.8f);
                        }
                        if (dmg != null && (failed || a.halfOnSave)) Apply(u, v, dmg, false, a, failed ? 1f : 0.5f);
                        if (!v.dead) ApplyConds(a, u, v, failed, dc);
                        if (failed && a.pushM != 0 && !v.dead) Push(u, v, a.pushM);
                    }
                }
                u.destructiveWrath = false;
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            // single / multi target
            for (int i = 0; i < ctx.targets.Count; i++)
            {
                var t = ctx.targets[i];
                if (t == null || t.dead && a.special != "revivify") continue;
                if (a.proj != ProjKind.None && t.actor != null)
                {
                    Vector3 from = u.actor.Chest + u.actor.transform.forward * 0.3f;
                    if (u.actor.rig != null) from = (anim == AnimAct.Bow ? u.actor.rig.socketHandL : u.actor.rig.socketHandR).position;
                    yield return FX.I.Projectile(from, t.actor.Chest, a.proj, a.color);
                }
                for (int h = 0; h < hits; h++)
                {
                    if (t.dead) break;
                    if (h > 0)
                    {
                        bool imp2 = false; u.actor.PlayAnim(anim, () => imp2 = true);
                        float g2 = 0; while (!imp2 && g2 < 3f) { g2 += Time.deltaTime; yield return null; }
                    }
                    if (!string.IsNullOrEmpty(a.heal) || a.special == "lay_on_hands" || a.special == "second_wind") { DoHeal(ctx, t); continue; }
                    if (a.IsAttack) ResolveAttack(ctx, t, false, i == 0 && h == 0);
                    else ResolveSaveOrEffect(ctx, t);
                }
                if (ctx.targets.Count > 1) yield return new WaitForSeconds(0.12f);
            }
            if (a.special == "ice_knife") Specials.IceKnifeBurst(ctx);
            if (a.special == "spiritual_weapon") { u.spiritualWeaponTurns = 10; if (!u.extraActions.Contains("spiritual_strike")) u.extraActions.Add("spiritual_strike"); }
            if (a.selfCond.HasValue) { u.AddCond(a.selfCond.Value, a.selfCondTurns, u); if (u.actor) FloatingText.Show(u.actor.HeadPos + Vector3.up * 0.5f, a.selfCond.Value.Nice(), Conditions.Get(a.selfCond.Value).color, 0.85f); if (a.concentration) u.concLinks.Add((u, a.selfCond.Value)); }
            if (a.tempHP > 0) { u.tempHP = Mathf.Max(u.tempHP, a.tempHP + (ctx.slot > 1 ? (ctx.slot - 1) * 5 : 0)); }
            if (a.special == "reckless") u.AddCond(Cond.Reckless, 1, u);
            u.destructiveWrath = false;
            yield return new WaitForSeconds(0.35f);
        }

        static void DoHeal(ActionContext ctx, Creature t)
        {
            var u = ctx.user; var a = ctx.a;
            int amt = 0;
            if (a.special == "lay_on_hands")
            {
                int pool = u.ResourceLeft("layonhands");
                amt = Mathf.Min(pool, t.MaxHPTotal - Mathf.Max(0, t.hp));
                if (amt <= 0) amt = Mathf.Min(pool, 1);
                u.SpendResource("layonhands", amt);
                Heal(u, t, amt, null);
                FX.Burst(t.actor.Chest, FxKind.Holy, 1f);
                return;
            }
            var d = DiceExpr.Parse(a.heal);
            if (!string.IsNullOrEmpty(a.upcast) && ctx.slot > a.spellLevel && a.spellLevel > 0) d.count += DiceExpr.Parse(a.upcast).count * (ctx.slot - a.spellLevel);
            amt = d.Roll();
            if (a.healAddMod) amt += u.SpellMod;
            if (a.special == "second_wind") amt += u.level;
            if (a.id == "healing_radiance") amt = d.Roll() + u.Mod(Ability.CHA);
            Heal(u, t, Mathf.Max(1, amt), a);
            if (t.actor) FX.Burst(t.actor.Chest, FxKind.Heal, 1f);
        }

        public static void ResolveAttack(ActionContext ctx, Creature t, bool cleave = false, bool first = true)
        {
            var u = ctx.user; var a = ctx.a;
            if (t.actor == null) return;
            u.attackedThisTurn = true;
            var w = a.usesWeapon ? WeaponFor(u, a) : null;
            if (a.special == "martial_arts" || a.id == "unarmed_strike" || (w != null && u.IsClass(ClassId.Monk) && (w.Has(WeaponProps.Monk) || w.family == "shortsword"))) u.usedMonkAttack = true;
            bool hit, crit; int total = 0;
            if (a.autoHit) { hit = true; crit = false; }
            else
            {
                var r = RollAttack(u, t, a);
                hit = r.hit; crit = r.crit;
                string adv = r.info.mode == RollMode.Advantage ? " (adv)" : r.info.mode == RollMode.Disadvantage ? " (dis)" : "";
                CombatLog.Add($"{Name(u)} attacks {Name(t)}: {r.d20.natural}{(r.d20.mode != RollMode.Normal ? "/" + r.d20.other : "")}{adv} {(r.d20.modifier >= 0 ? "+" : "")}{r.d20.modifier}{(r.parts.Count > 0 ? " " + string.Join(" ", r.parts) : "")} = {r.total} vs AC {r.info.ac} — " + (crit ? "<color=#ffd060><b>CRITICAL HIT</b></color>" : hit ? "<color=#e0d8c8>hit</color>" : "<color=#9a9a9a>miss</color>"));
                if (!hit && t.actor) { FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.3f, "Miss", Theme.TextDim, 0.95f); if (t.actor.anim is HumanoidAnimator ha && !ha.Busy) ha.Play(AnimAct.Dodge); }
            }
            if (!hit) { Audio.Sfx.Play(a.usesWeapon || a.fixedToHit != int.MinValue ? "swing_miss" : "spell_miss"); return; }
            if (crit) { CameraRig.I.Shake(0.12f); Audio.Sfx.Play("crit"); }
            var parts = RollDamage(u, t, a, crit, ctx.slot, first);
            total = Apply(u, t, parts, crit, a, cleave ? a.dmgMult : 1f);
            FX.I.Impact(t.actor.Chest, a, parts.Count > 0 ? parts[0].type : DamageType.None, crit);
            Audio.Sfx.Play(ImpactSound(a, parts));
            if (a.special == "drain" && total > 0) Heal(u, u, total / 2, null);
            if (a.special == "vampire_bite") { int nec = parts.Where(p => p.type == DamageType.Necrotic).Sum(p => p.amount); if (nec > 0) Heal(u, u, nec, null); }
            if (a.special == "strength_drain" && !t.dead) { int dr = Rng.D(4); t.strDrain += dr; t.AddCond(Cond.WeakenedStrength, -1); CombatLog.Add($"{Name(t)}'s Strength is drained by {dr}."); if (t.Score(Ability.STR) <= 1 && t.strDrain > 0) Kill(t, u, a); }
            if (a.special == "life_drain" && !t.dead) { var sr = RollSave(t, Ability.CON, 10, u, a); if (!sr.success) { t.maxHPPenalty += total; t.hp = Mathf.Min(t.hp, t.MaxHPTotal); CombatLog.Add($"{Name(t)}'s maximum hit points are reduced by {total}."); } }
            if (a.special == "eldritch_blast" && u.IsClass(ClassId.Warlock) && u.level >= 2 && !t.dead && t.size < SizeCat.Large) Push(u, t, 3f);
            if (t.dead) return;
            int dc = SaveDC(u, a);
            bool failed = true;
            if (a.hasSave && a.conds.Count > 0)
            {
                var sr = RollSave(t, a.saveAb, dc, u, a, a.conds[0].cond);
                failed = !sr.success;
                CombatLog.Add($"{Name(t)} {(failed ? "<color=#e87a6a>fails</color>" : "<color=#70e090>succeeds</color>")} a {a.saveAb} save ({sr.total} vs DC {dc}).");
            }
            else if (a.hasSave && a.pushM != 0)
            {
                failed = !RollSave(t, a.saveAb, dc, u, a).success;
            }
            ApplyConds(a, u, t, failed, dc);
            if (a.pushM != 0 && failed) Push(u, t, a.pushM);
            if (IsGrickOrMoundEngulf(a, t)) { }
        }

        static bool IsGrickOrMoundEngulf(ActionDef a, Creature t) => false;

        static string ImpactSound(ActionDef a, List<DmgPart> parts)
        {
            var t = parts.Count > 0 ? parts[0].type : DamageType.Bludgeoning;
            switch (t)
            {
                case DamageType.Fire: return "fire_hit";
                case DamageType.Cold: return "frost_hit";
                case DamageType.Lightning: return "lightning_hit";
                case DamageType.Radiant: return "radiant_hit";
                case DamageType.Necrotic: return "necrotic_hit";
                case DamageType.Force: return "force_hit";
                case DamageType.Thunder: return "thunder_hit";
                case DamageType.Slashing: return "slash_hit";
                case DamageType.Piercing: return "pierce_hit";
                default: return "blunt_hit";
            }
        }

        static void ResolveSaveOrEffect(ActionContext ctx, Creature t)
        {
            var u = ctx.user; var a = ctx.a;
            if (a.onlyVs != null && !a.onlyVs.Contains(t.type)) { FloatingText.Show(t.actor.HeadPos, "No effect", Theme.TextDim); return; }
            int dc = SaveDC(u, a);
            bool failed = true;
            if (a.hasSave)
            {
                var sr = RollSave(t, a.saveAb, dc, u, a, a.conds.Count > 0 ? a.conds[0].cond : (Cond?)null);
                failed = !sr.success;
                CombatLog.Add($"{Name(t)} {(failed ? "<color=#e87a6a>fails</color>" : "<color=#70e090>succeeds</color>")} a {a.saveAb} save ({sr.total} vs DC {dc}).");
                if (!failed && t.actor) FloatingText.Show(t.actor.HeadPos + Vector3.up * 0.5f, "Saved", Theme.TextDim, 0.8f);
            }
            if (!string.IsNullOrEmpty(a.dmg) && (failed || a.halfOnSave))
            {
                var parts = RollDamage(u, t, a, false, ctx.slot, false);
                Apply(u, t, parts, false, a, failed ? 1f : 0.5f);
                if (t.actor) FX.I.Impact(t.actor.Chest, a, a.dtype, false);
                Audio.Sfx.Play(ImpactSound(a, parts));
            }
            else if (t.actor && a.fx != FxKind.None) FX.Burst(t.actor.Chest, a.fx, 0.9f);
            if (!t.dead) ApplyConds(a, u, t, failed, dc);
            if (failed && a.pushM != 0 && !t.dead) Push(u, t, a.pushM);
            if (a.special == "aid") { t.maxHPBonus += 5; t.hp += 5; t.AddCond(Cond.Aided, -1); FloatingText.Show(t.actor.HeadPos, "+5 max HP", Theme.Gold); }
            if (a.special == "restoration") { foreach (var c in new[] { Cond.Poisoned, Cond.Blinded, Cond.Paralyzed }) t.RemoveCond(c); FloatingText.Show(t.actor.HeadPos, "Restored", Theme.Friendly); }
        }
    }
}
