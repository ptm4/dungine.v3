using System.Collections.Generic;
using System.Linq;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.Rules
{
    /// <summary>Every action, spell, feature and monster attack in the game.</summary>
    public static class ActionLibrary
    {
        public static readonly Dictionary<string, ActionDef> All = new Dictionary<string, ActionDef>();
        public static readonly Dictionary<ClassId, List<string>> ClassSpells = new Dictionary<ClassId, List<string>>();

        public static ActionDef Get(string id) => id != null && All.TryGetValue(id, out var a) ? a : null;

        static readonly Color cFire = new Color(1f, .5f, .2f), cFrost = new Color(.5f, .8f, 1f), cLight = new Color(.6f, .7f, 1f), cRad = new Color(1f, .9f, .5f), cNec = new Color(.55f, .9f, .5f),
            cForce = new Color(.75f, .55f, 1f), cAcid = new Color(.6f, 1f, .3f), cPsy = new Color(1f, .45f, .85f), cThunder = new Color(.7f, .75f, .95f), cHeal = new Color(.45f, 1f, .6f),
            cMartial = new Color(.9f, .75f, .55f), cBonus = new Color(1f, .6f, .25f), cHoly = new Color(1f, .85f, .45f), cNature = new Color(.55f, .85f, .4f);

        static ActionDef Add(ActionDef a) { All[a.id] = a; return a; }

        static ActionDef Spell(string id, string name, int lvl, School school, string desc, params ClassId[] classes)
        {
            var a = new ActionDef { id = id, name = name, spellLevel = lvl, school = school, desc = desc, anim = AnimAct.CastPoint };
            foreach (var c in classes)
            {
                if (!ClassSpells.TryGetValue(c, out var l)) ClassSpells[c] = l = new List<string>();
                l.Add(id);
            }
            return Add(a);
        }

        static ActionDef Feat(string id, string name, ActionCost cost, string desc, string icon, Color col)
            => Add(new ActionDef { id = id, name = name, cost = cost, desc = desc, icon = icon, color = col, target = TargetKind.Self, who = Who.Ally, anim = AnimAct.Roar });

        static CondApply C(Cond c, int t, bool rep = false) => new CondApply(c, t, rep);

        const ClassId Bar = ClassId.Barbarian, Brd = ClassId.Bard, Clr = ClassId.Cleric, Dru = ClassId.Druid, Ftr = ClassId.Fighter, Mnk = ClassId.Monk, Pal = ClassId.Paladin, Rng = ClassId.Ranger, Rog = ClassId.Rogue, Sor = ClassId.Sorcerer, Wlk = ClassId.Warlock, Wiz = ClassId.Wizard;

        static ActionLibrary()
        {
            // =============================== common ===============================
            Add(new ActionDef { id = "attack", name = "Main Hand Attack", desc = "Make an attack with your main-hand weapon.", icon = "sword", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, anim = AnimAct.Slash });
            Add(new ActionDef { id = "ranged_attack", name = "Ranged Attack", desc = "Loose a shot with your ranged weapon.", icon = "bow", color = cMartial, attack = AttackKind.RangedWeapon, usesWeapon = true, addMod = true, range = 18, anim = AnimAct.Bow, proj = ProjKind.Arrow });
            Add(new ActionDef { id = "offhand_attack", name = "Off-Hand Attack", desc = "Strike with the light weapon in your off hand. You don't add your ability modifier to the damage unless you have the Two-Weapon Fighting style.", cost = ActionCost.BonusAction, icon = "dagger", color = cBonus, attack = AttackKind.MeleeWeapon, usesWeapon = true, offHand = true, addMod = false, anim = AnimAct.Thrust });
            Add(new ActionDef { id = "unarmed_strike", name = "Unarmed Strike", desc = "Punch, kick or headbutt: 1 + Strength modifier bludgeoning damage.", icon = "fist", color = cMartial, attack = AttackKind.MeleeWeapon, dmg = "1", dtype = DamageType.Bludgeoning, addMod = true, anim = AnimAct.Punch, special = "unarmed" });
            Add(new ActionDef { id = "dash", name = "Dash", desc = "Double your remaining movement this turn.", icon = "dash", color = cMartial, target = TargetKind.Self, who = Who.Ally, special = "dash", anim = AnimAct.None });
            Add(new ActionDef { id = "disengage", name = "Disengage", desc = "Your movement doesn't provoke opportunity attacks for the rest of the turn.", icon = "wind", color = cMartial, target = TargetKind.Self, who = Who.Ally, selfCond = Cond.Disengaged, selfCondTurns = 1, anim = AnimAct.None });
            Add(new ActionDef { id = "dodge", name = "Dodge", desc = "Until your next turn, attacks against you have disadvantage and you make Dexterity saves with advantage.", icon = "shield", color = cMartial, target = TargetKind.Self, who = Who.Ally, selfCond = Cond.Dodging, selfCondTurns = 1, anim = AnimAct.Dodge });
            Add(new ActionDef { id = "hide", name = "Hide", desc = "Make a Stealth check against the highest passive Perception among your enemies. If you succeed you are hidden: attacks against you have disadvantage and your next attack has advantage.", cost = ActionCost.Action, icon = "eye", color = cMartial, target = TargetKind.Self, who = Who.Ally, special = "hide", anim = AnimAct.None });
            Add(new ActionDef { id = "shove", name = "Shove", desc = "Push a creature up to 3 metres away. Contested Athletics against its Athletics or Acrobatics.", cost = ActionCost.BonusAction, icon = "fist", color = cBonus, range = 1.5f, special = "shove", anim = AnimAct.Shove });
            Add(new ActionDef { id = "help", name = "Help", desc = "Help a downed ally back onto their feet with 1 hit point, or end the Asleep or Prone condition on an ally.", icon = "hand", color = cHeal, range = 1.5f, who = Who.DownedAlly, special = "help", anim = AnimAct.Interact });
            Add(new ActionDef { id = "end_turn", name = "End Turn", hidden = true, target = TargetKind.Self });

            // =============================== weapon actions ===============================
            Add(new ActionDef { id = "topple", name = "Topple", desc = "Attack with your weapon. On a hit the target must succeed on a Dexterity save or be knocked Prone.", icon = "boot", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.DEX, conds = { C(Cond.Prone, 1) }, anim = AnimAct.Overhead });
            Add(new ActionDef { id = "lacerate", name = "Lacerate", desc = "Rake the target open. On a hit it must succeed on a Constitution save or start Bleeding.", icon = "blood", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.CON, conds = { C(Cond.Bleeding, 2) }, anim = AnimAct.Slash });
            Add(new ActionDef { id = "pommel_strike", name = "Pommel Strike", desc = "Crack the target with your pommel: 1d4 + Strength bludgeoning. It must succeed on a Constitution save or be Dazed.", cost = ActionCost.BonusAction, icon = "fist", color = cBonus, attack = AttackKind.MeleeWeapon, dmg = "1d4", dtype = DamageType.Bludgeoning, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.CON, conds = { C(Cond.Dazed, 2) }, anim = AnimAct.Punch });
            Add(new ActionDef { id = "piercing_strike", name = "Piercing Strike", desc = "Drive the point deep. On a hit the target must succeed on a Constitution save or suffer a Gaping Wound.", icon = "dagger", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.CON, conds = { C(Cond.GapingWound, 2) }, anim = AnimAct.Thrust });
            Add(new ActionDef { id = "cleave", name = "Cleave", desc = "Swing in a wide arc, attacking up to three enemies in front of you for half damage.", icon = "axe", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, target = TargetKind.Cone, radius = 2.2f, coneAngle = 110, dmgMult = 0.5f, anim = AnimAct.Slash });
            Add(new ActionDef { id = "concussive_smash", name = "Concussive Smash", desc = "A ringing blow to the head. On a hit the target must succeed on a Wisdom save or be Dazed.", icon = "mace", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.WIS, conds = { C(Cond.Dazed, 2) }, anim = AnimAct.Overhead });
            Add(new ActionDef { id = "flourish", name = "Defensive Flourish", desc = "Attack, then fall into a guard: +2 AC until your next turn.", icon = "sword", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, selfCond = Cond.ShieldOfFaith, selfCondTurns = 1, anim = AnimAct.Thrust });
            Add(new ActionDef { id = "hamstring_shot", name = "Hamstring Shot", desc = "Shoot for the legs. On a hit the target must succeed on a Constitution save or have its movement slowed.", icon = "bow", color = cMartial, attack = AttackKind.RangedWeapon, usesWeapon = true, addMod = true, range = 18, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.CON, conds = { C(Cond.Chilled, 2) }, anim = AnimAct.Bow, proj = ProjKind.Arrow });
            Add(new ActionDef { id = "piercing_shot", name = "Piercing Shot", desc = "A bolt that tears through. On a hit the target must succeed on a Constitution save or suffer a Gaping Wound.", icon = "crossbow", color = cMartial, attack = AttackKind.RangedWeapon, usesWeapon = true, addMod = true, range = 18, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, hasSave = true, saveAb = Ability.CON, conds = { C(Cond.GapingWound, 2) }, anim = AnimAct.Crossbow, proj = ProjKind.Bolt });
            Add(new ActionDef { id = "mobile_shot", name = "Mobile Shot", desc = "Loose an arrow, then keep moving: this turn your movement doesn't provoke opportunity attacks.", icon = "bow", color = cMartial, attack = AttackKind.RangedWeapon, usesWeapon = true, addMod = true, range = 18, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, selfCond = Cond.Disengaged, selfCondTurns = 1, anim = AnimAct.Bow, proj = ProjKind.Arrow });
            Add(new ActionDef { id = "brace", name = "Brace", desc = "Steady your crossbow: roll damage twice and keep the higher result.", icon = "crossbow", color = cMartial, attack = AttackKind.RangedWeapon, usesWeapon = true, addMod = true, range = 18, weaponAction = true, recharge = Recharge.ShortRest, maxUses = 1, special = "brace", anim = AnimAct.Crossbow, proj = ProjKind.Bolt });

            // =============================== cantrips ===============================
            var s = Spell("fire_bolt", "Fire Bolt", 0, School.Evocation, "Hurl a mote of fire: ranged spell attack for 1d10 fire damage.", Sor, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d10"; s.dtype = DamageType.Fire; s.cantripScales = true; s.icon = "fire"; s.color = cFire; s.proj = ProjKind.Fire; s.fx = FxKind.Fire;
            s = Spell("ray_of_frost", "Ray of Frost", 0, School.Evocation, "A frigid beam: 1d8 cold damage and the target's movement is slowed until its next turn.", Sor, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d8"; s.dtype = DamageType.Cold; s.cantripScales = true; s.icon = "frost"; s.color = cFrost; s.proj = ProjKind.Frost; s.fx = FxKind.Frost; s.conds.Add(new CondApply(Cond.Chilled, 1) { onlyOnFail = false });
            s = Spell("shocking_grasp", "Shocking Grasp", 0, School.Evocation, "Lightning springs from your hand: melee spell attack for 1d8 lightning damage, and the target can't take reactions.", Sor, Wiz);
            s.attack = AttackKind.MeleeSpell; s.range = 1.5f; s.dmg = "1d8"; s.dtype = DamageType.Lightning; s.cantripScales = true; s.icon = "lightning"; s.color = cLight; s.fx = FxKind.Lightning; s.anim = AnimAct.CastTouch; s.conds.Add(new CondApply(Cond.Dazed, 1) { onlyOnFail = false });
            s = Spell("chill_touch", "Chill Touch", 0, School.Necromancy, "A ghostly skeletal hand grips the target: 1d8 necrotic damage, and it can't regain hit points until your next turn.", Sor, Wiz, Wlk);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d8"; s.dtype = DamageType.Necrotic; s.cantripScales = true; s.icon = "skull"; s.color = cNec; s.proj = ProjKind.Necrotic; s.fx = FxKind.Necrotic;
            s = Spell("acid_splash", "Acid Splash", 0, School.Conjuration, "Hurl a bubble of acid that bursts in a small area: Dexterity save or 1d6 acid damage.", Sor, Wiz);
            s.target = TargetKind.Point; s.radius = 1.5f; s.range = 18; s.hasSave = true; s.saveAb = Ability.DEX; s.dmg = "1d6"; s.dtype = DamageType.Acid; s.cantripScales = true; s.icon = "acid"; s.color = cAcid; s.proj = ProjKind.Acid; s.fx = FxKind.Acid;
            s = Spell("poison_spray", "Poison Spray", 0, School.Conjuration, "A puff of noxious gas: Constitution save or 1d12 poison damage.", Sor, Wiz, Dru, Wlk);
            s.range = 3; s.hasSave = true; s.saveAb = Ability.CON; s.dmg = "1d12"; s.dtype = DamageType.Poison; s.cantripScales = true; s.icon = "poison"; s.color = cAcid; s.fx = FxKind.Poison;
            s = Spell("sacred_flame", "Sacred Flame", 0, School.Evocation, "Radiance descends on the target: Dexterity save or 1d8 radiant damage.", Clr);
            s.range = 18; s.hasSave = true; s.saveAb = Ability.DEX; s.dmg = "1d8"; s.dtype = DamageType.Radiant; s.cantripScales = true; s.icon = "sun"; s.color = cRad; s.fx = FxKind.Radiant; s.anim = AnimAct.CastRaise;
            s = Spell("toll_the_dead", "Toll the Dead", 0, School.Necromancy, "A mournful bell tolls for the target: Wisdom save or 1d8 necrotic damage, or 1d12 if it is already wounded.", Clr, Wiz, Wlk);
            s.range = 18; s.hasSave = true; s.saveAb = Ability.WIS; s.dmg = "1d8"; s.dtype = DamageType.Necrotic; s.cantripScales = true; s.icon = "bell"; s.color = cNec; s.fx = FxKind.Necrotic; s.special = "toll";
            s = Spell("eldritch_blast", "Eldritch Blast", 0, School.Evocation, "A crackling beam of force: ranged spell attack for 1d10 force damage. At level 5 you fire two beams.", Wlk);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d10"; s.dtype = DamageType.Force; s.icon = "force"; s.color = cForce; s.proj = ProjKind.Force; s.fx = FxKind.Force; s.special = "eldritch_blast"; s.distinctTargets = false;
            s = Spell("vicious_mockery", "Vicious Mockery", 0, School.Enchantment, "An insult laced with magic: Wisdom save or 1d4 psychic damage and disadvantage on its next attack.", Brd);
            s.range = 18; s.hasSave = true; s.saveAb = Ability.WIS; s.dmg = "1d4"; s.dtype = DamageType.Psychic; s.cantripScales = true; s.icon = "music"; s.color = cPsy; s.fx = FxKind.Psychic; s.conds.Add(C(Cond.Reeling, 1)); s.anim = AnimAct.CastPoint;
            s = Spell("produce_flame", "Produce Flame", 0, School.Conjuration, "A flame flickers in your palm and flies at the target: ranged spell attack for 1d8 fire damage.", Dru);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d8"; s.dtype = DamageType.Fire; s.cantripScales = true; s.icon = "fire"; s.color = cFire; s.proj = ProjKind.Fire; s.fx = FxKind.Fire;
            s = Spell("thorn_whip", "Thorn Whip", 0, School.Transmutation, "A vine-whip lashes out: melee spell attack for 1d6 piercing damage that pulls the target 3m toward you.", Dru);
            s.attack = AttackKind.MeleeSpell; s.range = 9; s.dmg = "1d6"; s.dtype = DamageType.Piercing; s.cantripScales = true; s.icon = "leaf"; s.color = cNature; s.proj = ProjKind.Thorn; s.pushM = -3; s.fx = FxKind.Nature;
            s = Spell("guidance", "Guidance", 0, School.Divination, "Bless an ally's next ability check with an extra 1d4. Outside of combat, the whole party benefits in conversation.", Clr, Dru);
            s.target = TargetKind.Creature; s.who = Who.Ally; s.range = 1.5f; s.conds.Add(new CondApply(Cond.Guidance, 10) { onlyOnFail = false }); s.icon = "star"; s.color = cHoly; s.fx = FxKind.Buff; s.anim = AnimAct.Bless; s.concentration = true;
            s = Spell("blade_ward", "Blade Ward", 0, School.Abjuration, "Gain resistance to bludgeoning, piercing and slashing damage from weapon attacks until the end of your next turn.", Brd, Sor, Wlk, Wiz);
            s.target = TargetKind.Self; s.who = Who.Ally; s.selfCond = Cond.BladeWard; s.selfCondTurns = 2; s.icon = "shield"; s.color = cForce; s.fx = FxKind.Buff; s.anim = AnimAct.Bless;

            // =============================== level 1 ===============================
            s = Spell("magic_missile", "Magic Missile", 1, School.Evocation, "Three glowing darts that never miss, each dealing 1d4+1 force damage. One more dart per slot level above 1st.", Sor, Wiz);
            s.range = 18; s.autoHit = true; s.targets = 3; s.targetsPerUpcast = 1; s.distinctTargets = false; s.dmg = "1d4+1"; s.dtype = DamageType.Force; s.icon = "force"; s.color = cForce; s.proj = ProjKind.Force; s.fx = FxKind.Force;
            s = Spell("burning_hands", "Burning Hands", 1, School.Evocation, "A thin sheet of flame: every creature in a 5m cone makes a Dexterity save, taking 3d6 fire damage on a failure or half on a success.", Sor, Wiz);
            s.target = TargetKind.Cone; s.radius = 5f; s.coneAngle = 60; s.hasSave = true; s.saveAb = Ability.DEX; s.halfOnSave = true; s.dmg = "3d6"; s.dtype = DamageType.Fire; s.upcast = "1d6"; s.icon = "fire"; s.color = cFire; s.fx = FxKind.Fire; s.anim = AnimAct.CastTouch;
            s = Spell("thunderwave", "Thunderwave", 1, School.Evocation, "A wave of thunder sweeps out from you: Constitution save or 2d8 thunder damage and be pushed 3m. Half damage on a success.", Brd, Dru, Sor, Wiz);
            s.target = TargetKind.Cone; s.radius = 4f; s.coneAngle = 100; s.hasSave = true; s.saveAb = Ability.CON; s.halfOnSave = true; s.dmg = "2d8"; s.dtype = DamageType.Thunder; s.upcast = "1d8"; s.pushM = 3; s.icon = "thunder"; s.color = cThunder; s.fx = FxKind.Thunder; s.anim = AnimAct.Shove;
            s = Spell("chromatic_orb", "Chromatic Orb", 1, School.Evocation, "Hurl an orb of crackling energy: ranged spell attack for 3d8 lightning damage.", Sor, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "3d8"; s.dtype = DamageType.Lightning; s.upcast = "1d8"; s.icon = "lightning"; s.color = cLight; s.proj = ProjKind.Lightning; s.fx = FxKind.Lightning;
            s = Spell("ice_knife", "Ice Knife", 1, School.Conjuration, "A shard of ice strikes for 1d10 piercing, then bursts: everyone within 1.5m makes a Dexterity save or takes 2d6 cold.", Dru, Sor, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d10"; s.dtype = DamageType.Piercing; s.icon = "frost"; s.color = cFrost; s.proj = ProjKind.Frost; s.fx = FxKind.Frost; s.special = "ice_knife"; s.upcast = "1d6";
            s = Spell("sleep", "Sleep", 1, School.Enchantment, "Creatures within 6m of the target point fall asleep, lowest hit points first, up to 5d8 hit points in total. Undead and elves are unaffected.", Brd, Sor, Wiz);
            s.target = TargetKind.Point; s.radius = 6f; s.range = 18; s.icon = "sleep"; s.color = cPsy; s.fx = FxKind.Psychic; s.special = "sleep"; s.dmg = "5d8"; s.upcast = "2d8";
            s = Spell("shield", "Shield", 1, School.Abjuration, "Reaction: when you are hit by an attack, gain +5 AC until the start of your next turn, possibly turning the hit into a miss.", Sor, Wiz);
            s.cost = ActionCost.Reaction; s.target = TargetKind.Self; s.who = Who.Ally; s.selfCond = Cond.ShieldSpell; s.selfCondTurns = 1; s.icon = "shield"; s.color = cForce; s.fx = FxKind.Force; s.special = "shield_reaction";
            s = Spell("mage_armor", "Mage Armour", 1, School.Abjuration, "An unarmoured ally's base AC becomes 13 + Dexterity modifier until long rest.", Sor, Wiz);
            s.who = Who.Ally; s.range = 1.5f; s.conds.Add(new CondApply(Cond.MageArmor, -1) { onlyOnFail = false }); s.icon = "shield"; s.color = cForce; s.fx = FxKind.Buff; s.anim = AnimAct.CastTouch;
            s = Spell("false_life", "False Life", 1, School.Necromancy, "Gain 1d4+4 temporary hit points.", Sor, Wiz);
            s.target = TargetKind.Self; s.who = Who.Ally; s.special = "false_life"; s.icon = "skull"; s.color = cNec; s.fx = FxKind.Necrotic; s.anim = AnimAct.Bless;
            s = Spell("ray_of_sickness", "Ray of Sickness", 1, School.Necromancy, "A sickly green ray: ranged spell attack for 2d8 poison damage; the target must pass a Constitution save or be Poisoned.", Sor, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "2d8"; s.dtype = DamageType.Poison; s.upcast = "1d8"; s.hasSave = true; s.saveAb = Ability.CON; s.conds.Add(C(Cond.Poisoned, 2)); s.icon = "poison"; s.color = cAcid; s.proj = ProjKind.Poison; s.fx = FxKind.Poison;
            s = Spell("cure_wounds", "Cure Wounds", 1, School.Evocation, "Touch an ally to heal 1d8 + your spellcasting modifier hit points.", Brd, Clr, Dru, Pal, Rng);
            s.who = Who.Ally; s.range = 1.5f; s.heal = "1d8"; s.healAddMod = true; s.upcast = "1d8"; s.icon = "heal"; s.color = cHeal; s.fx = FxKind.Heal; s.anim = AnimAct.Heal;
            s = Spell("healing_word", "Healing Word", 1, School.Evocation, "Bonus action: a word of power heals an ally within 18m for 1d4 + your spellcasting modifier. Can revive a downed ally.", Brd, Clr, Dru);
            s.cost = ActionCost.BonusAction; s.who = Who.Ally; s.range = 18; s.heal = "1d4"; s.healAddMod = true; s.upcast = "1d4"; s.icon = "heal"; s.color = cHeal; s.fx = FxKind.Heal; s.anim = AnimAct.Bless;
            s = Spell("guiding_bolt", "Guiding Bolt", 1, School.Evocation, "A flash of light: ranged spell attack for 4d6 radiant damage, and the next attack against the target has advantage.", Clr);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "4d6"; s.dtype = DamageType.Radiant; s.upcast = "1d6"; s.conds.Add(new CondApply(Cond.FaerieFire, 1) { onlyOnFail = false }); s.icon = "sun"; s.color = cRad; s.proj = ProjKind.Radiant; s.fx = FxKind.Radiant;
            s = Spell("bless", "Bless", 1, School.Enchantment, "Up to three allies add 1d4 to attack rolls and saving throws.", Clr, Pal);
            s.who = Who.Ally; s.range = 9; s.targets = 3; s.targetsPerUpcast = 1; s.concentration = true; s.conds.Add(new CondApply(Cond.Blessed, 10) { onlyOnFail = false }); s.icon = "bless"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.Bless;
            s = Spell("bane", "Bane", 1, School.Enchantment, "Up to three enemies make a Charisma save or subtract 1d4 from attack rolls and saving throws.", Brd, Clr);
            s.range = 9; s.targets = 3; s.targetsPerUpcast = 1; s.concentration = true; s.hasSave = true; s.saveAb = Ability.CHA; s.conds.Add(C(Cond.Baned, 10)); s.icon = "curse"; s.color = cPsy; s.fx = FxKind.Debuff;
            s = Spell("inflict_wounds", "Inflict Wounds", 1, School.Necromancy, "Your touch rots flesh: melee spell attack for 3d10 necrotic damage.", Clr);
            s.attack = AttackKind.MeleeSpell; s.range = 1.5f; s.dmg = "3d10"; s.dtype = DamageType.Necrotic; s.upcast = "1d10"; s.icon = "skull"; s.color = cNec; s.fx = FxKind.Necrotic; s.anim = AnimAct.CastTouch;
            s = Spell("command", "Command: Grovel", 1, School.Enchantment, "Speak one word of divine command. The target makes a Wisdom save or falls Prone and loses its next turn.", Clr, Pal);
            s.range = 18; s.hasSave = true; s.saveAb = Ability.WIS; s.conds.Add(C(Cond.Prone, 1)); s.conds.Add(C(Cond.Incapacitated, 1)); s.icon = "hand"; s.color = cPsy; s.fx = FxKind.Psychic; s.targetsPerUpcast = 1;
            s = Spell("shield_of_faith", "Shield of Faith", 1, School.Abjuration, "Bonus action: a shimmering field grants an ally +2 AC.", Clr, Pal);
            s.cost = ActionCost.BonusAction; s.who = Who.Ally; s.range = 18; s.concentration = true; s.conds.Add(new CondApply(Cond.ShieldOfFaith, 10) { onlyOnFail = false }); s.icon = "shield"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.Bless;
            s = Spell("protection_evil", "Protection from Evil and Good", 1, School.Abjuration, "An ally is protected against undead, fiends, fey and aberrations: they attack it with disadvantage and it can't be charmed or frightened by them.", Clr, Pal, Wlk, Wiz);
            s.who = Who.Ally; s.range = 1.5f; s.concentration = true; s.conds.Add(new CondApply(Cond.ProtectionEvil, 10) { onlyOnFail = false }); s.icon = "holy"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.CastTouch;
            s = Spell("faerie_fire", "Faerie Fire", 1, School.Evocation, "Outline creatures in a 3m radius with violet light: on a failed Dexterity save, attacks against them have advantage.", Brd, Dru);
            s.target = TargetKind.Point; s.radius = 3f; s.range = 18; s.hasSave = true; s.saveAb = Ability.DEX; s.concentration = true; s.conds.Add(C(Cond.FaerieFire, 10)); s.icon = "star"; s.color = cPsy; s.fx = FxKind.Psychic;
            s = Spell("entangle", "Entangle", 1, School.Conjuration, "Grasping weeds erupt in a 4m radius. Creatures there make a Strength save or become Ensnared.", Dru);
            s.target = TargetKind.Point; s.radius = 4f; s.range = 18; s.hasSave = true; s.saveAb = Ability.STR; s.concentration = true; s.conds.Add(C(Cond.Ensnared, 3, true)); s.icon = "leaf"; s.color = cNature; s.fx = FxKind.Nature; s.anim = AnimAct.CastRaise;
            s = Spell("hex", "Hex", 1, School.Enchantment, "Bonus action: curse a creature. Your attacks deal an extra 1d6 necrotic damage to it.", Wlk);
            s.cost = ActionCost.BonusAction; s.range = 18; s.concentration = true; s.conds.Add(new CondApply(Cond.Hexed, 10) { onlyOnFail = false }); s.icon = "curse"; s.color = cNec; s.fx = FxKind.Shadow;
            s = Spell("hunters_mark", "Hunter's Mark", 1, School.Divination, "Bonus action: mark a quarry. Your weapon attacks deal an extra 1d6 damage to it.", Rng);
            s.cost = ActionCost.BonusAction; s.range = 18; s.concentration = true; s.conds.Add(new CondApply(Cond.HuntersMark, 10) { onlyOnFail = false }); s.icon = "mark"; s.color = cNature; s.fx = FxKind.Debuff;
            s = Spell("hellish_rebuke", "Hellish Rebuke", 1, School.Evocation, "Reaction: when damaged by a creature, engulf it in flames: Dexterity save or 2d10 fire damage, half on a success.", Wlk);
            s.cost = ActionCost.Reaction; s.range = 18; s.hasSave = true; s.saveAb = Ability.DEX; s.halfOnSave = true; s.dmg = "2d10"; s.dtype = DamageType.Fire; s.upcast = "1d10"; s.icon = "fire"; s.color = cFire; s.fx = FxKind.Fire; s.special = "rebuke_reaction";
            s = Spell("armor_of_agathys", "Armour of Agathys", 1, School.Abjuration, "Spectral frost cloaks you: gain 5 temporary hit points, and melee attackers take 5 cold damage while they last.", Wlk);
            s.target = TargetKind.Self; s.who = Who.Ally; s.tempHP = 5; s.selfCond = Cond.ArmorOfAgathys; s.selfCondTurns = -1; s.icon = "frost"; s.color = cFrost; s.fx = FxKind.Frost; s.anim = AnimAct.Bless;
            s = Spell("dissonant_whispers", "Dissonant Whispers", 1, School.Enchantment, "A melody only the target hears: Wisdom save or 3d6 psychic damage and it becomes Frightened.", Brd);
            s.range = 18; s.hasSave = true; s.saveAb = Ability.WIS; s.halfOnSave = true; s.dmg = "3d6"; s.dtype = DamageType.Psychic; s.upcast = "1d6"; s.conds.Add(C(Cond.Frightened, 1)); s.icon = "music"; s.color = cPsy; s.fx = FxKind.Psychic;
            s = Spell("hideous_laughter", "Hideous Laughter", 1, School.Enchantment, "The target makes a Wisdom save or collapses in helpless laughter, Prone and Incapacitated. It repeats the save each turn.", Brd, Wiz);
            s.range = 9; s.hasSave = true; s.saveAb = Ability.WIS; s.concentration = true; s.conds.Add(C(Cond.Laughing, 10, true)); s.icon = "laugh"; s.color = cPsy; s.fx = FxKind.Psychic;
            s = Spell("divine_favor", "Divine Favour", 1, School.Evocation, "Bonus action: your weapon attacks deal an extra 1d4 radiant damage.", Pal);
            s.cost = ActionCost.BonusAction; s.target = TargetKind.Self; s.who = Who.Ally; s.concentration = true; s.selfCond = Cond.DivineFavor; s.selfCondTurns = 10; s.icon = "sun"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.Bless;
            s = Spell("witch_bolt", "Witch Bolt", 1, School.Evocation, "A crackling arc of blue energy: ranged spell attack for 1d12 lightning damage.", Sor, Wlk, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.dmg = "1d12"; s.dtype = DamageType.Lightning; s.upcast = "1d12"; s.icon = "lightning"; s.color = cLight; s.proj = ProjKind.Lightning; s.fx = FxKind.Lightning;

            // =============================== level 2 ===============================
            s = Spell("scorching_ray", "Scorching Ray", 2, School.Evocation, "Three rays of fire, each a ranged spell attack for 2d6 fire damage. One more ray per slot level above 2nd.", Sor, Wiz);
            s.attack = AttackKind.RangedSpell; s.range = 18; s.targets = 3; s.targetsPerUpcast = 1; s.distinctTargets = false; s.dmg = "2d6"; s.dtype = DamageType.Fire; s.icon = "fire"; s.color = cFire; s.proj = ProjKind.Fire; s.fx = FxKind.Fire;
            s = Spell("misty_step", "Misty Step", 2, School.Conjuration, "Bonus action: vanish in silver mist and reappear at a point you can see within 18m.", Sor, Wlk, Wiz);
            s.cost = ActionCost.BonusAction; s.target = TargetKind.Point; s.radius = 0; s.range = 18; s.who = Who.Ally; s.special = "teleport"; s.icon = "teleport"; s.color = cForce; s.fx = FxKind.Smoke; s.anim = AnimAct.None;
            s = Spell("hold_person", "Hold Person", 2, School.Enchantment, "A humanoid makes a Wisdom save or is Paralyzed. It repeats the save at the end of each of its turns.", Brd, Clr, Dru, Sor, Wlk, Wiz);
            s.range = 18; s.hasSave = true; s.saveAb = Ability.WIS; s.concentration = true; s.conds.Add(C(Cond.Paralyzed, 10, true)); s.icon = "chain"; s.color = cPsy; s.fx = FxKind.Psychic; s.onlyVs = new[] { CreatureType.Humanoid }; s.targetsPerUpcast = 1;
            s = Spell("spiritual_weapon", "Spiritual Weapon", 2, School.Evocation, "Bonus action: a floating spectral weapon strikes a target within 18m for 1d8 + your spellcasting modifier force damage. For the next ten turns you can make it strike again as a bonus action.", Clr);
            s.cost = ActionCost.BonusAction; s.attack = AttackKind.MeleeSpell; s.range = 18; s.dmg = "1d8"; s.dtype = DamageType.Force; s.addMod = true; s.icon = "sword"; s.color = cHoly; s.fx = FxKind.Force; s.special = "spiritual_weapon"; s.anim = AnimAct.CastPoint;
            Add(new ActionDef { id = "spiritual_strike", name = "Spiritual Weapon: Strike", desc = "Your spiritual weapon strikes again: melee spell attack for 1d8 + spellcasting modifier force damage.", cost = ActionCost.BonusAction, attack = AttackKind.MeleeSpell, range = 18, dmg = "1d8", dtype = DamageType.Force, addMod = true, icon = "sword", color = cHoly, fx = FxKind.Force, anim = AnimAct.CastPoint, special = "spell_mod" });
            s = Spell("moonbeam", "Moonbeam", 2, School.Evocation, "A silvery beam of moonlight: creatures within 1.5m make a Constitution save, taking 2d10 radiant on a failure or half on a success.", Dru);
            s.target = TargetKind.Point; s.radius = 1.8f; s.range = 36; s.hasSave = true; s.saveAb = Ability.CON; s.halfOnSave = true; s.dmg = "2d10"; s.dtype = DamageType.Radiant; s.upcast = "1d10"; s.icon = "moon"; s.color = cRad; s.fx = FxKind.Radiant; s.anim = AnimAct.CastRaise;
            s = Spell("shatter", "Shatter", 2, School.Evocation, "A painfully loud ringing: creatures within 3m make a Constitution save, taking 3d8 thunder damage or half on a success.", Brd, Sor, Wlk, Wiz);
            s.target = TargetKind.Point; s.radius = 3f; s.range = 18; s.hasSave = true; s.saveAb = Ability.CON; s.halfOnSave = true; s.dmg = "3d8"; s.dtype = DamageType.Thunder; s.upcast = "1d8"; s.icon = "thunder"; s.color = cThunder; s.fx = FxKind.Thunder;
            s = Spell("cloud_of_daggers", "Cloud of Daggers", 2, School.Conjuration, "Spinning blades fill a small area: 4d4 slashing damage to creatures there, no save.", Brd, Sor, Wlk, Wiz);
            s.target = TargetKind.Point; s.radius = 1.5f; s.range = 18; s.dmg = "4d4"; s.dtype = DamageType.Slashing; s.upcast = "2d4"; s.autoHit = true; s.icon = "dagger"; s.color = cForce; s.fx = FxKind.Slash;
            s = Spell("aid", "Aid", 2, School.Abjuration, "Up to three allies increase their maximum and current hit points by 5.", Clr, Pal);
            s.who = Who.Ally; s.range = 9; s.targets = 3; s.special = "aid"; s.icon = "heart"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.Bless;
            s = Spell("prayer_of_healing", "Prayer of Healing", 2, School.Evocation, "Out of combat only: every ally within 9m regains 2d8 + your spellcasting modifier hit points.", Clr);
            s.target = TargetKind.Aura; s.who = Who.Ally; s.radius = 9; s.heal = "2d8"; s.healAddMod = true; s.upcast = "1d8"; s.outOfCombatOnly = true; s.icon = "heal"; s.color = cHeal; s.fx = FxKind.Heal; s.anim = AnimAct.Bless;
            s = Spell("lesser_restoration", "Lesser Restoration", 2, School.Abjuration, "Cure an ally of disease, poison, blindness or paralysis.", Brd, Clr, Dru, Pal, Rng);
            s.who = Who.Ally; s.range = 1.5f; s.special = "restoration"; s.icon = "heal"; s.color = cHeal; s.fx = FxKind.Heal; s.anim = AnimAct.CastTouch; s.cost = ActionCost.BonusAction;
            s = Spell("invisibility", "Invisibility", 2, School.Illusion, "An ally turns invisible until they attack or cast a spell.", Brd, Sor, Wlk, Wiz);
            s.who = Who.Ally; s.range = 1.5f; s.concentration = true; s.conds.Add(new CondApply(Cond.Invisible, 10) { onlyOnFail = false }); s.icon = "eye"; s.color = cForce; s.fx = FxKind.Smoke; s.anim = AnimAct.CastTouch;
            s = Spell("web", "Web", 2, School.Conjuration, "Sticky webbing fills a 4m radius: creatures there make a Dexterity save or become Webbed.", Sor, Wiz);
            s.target = TargetKind.Point; s.radius = 4f; s.range = 18; s.hasSave = true; s.saveAb = Ability.DEX; s.concentration = true; s.conds.Add(C(Cond.Webbed, 3, true)); s.icon = "web"; s.color = new Color(.85f, .85f, .85f); s.fx = FxKind.Smoke;
            s = Spell("darkness", "Darkness", 2, School.Evocation, "Magical darkness in a 4m radius: creatures inside are Blinded while they remain.", Sor, Wlk, Wiz);
            s.target = TargetKind.Point; s.radius = 4f; s.range = 18; s.concentration = true; s.autoHit = true; s.conds.Add(new CondApply(Cond.Blinded, 2) { onlyOnFail = false }); s.icon = "moon"; s.color = new Color(.4f, .3f, .55f); s.fx = FxKind.Shadow;

            // =============================== level 3 ===============================
            s = Spell("fireball", "Fireball", 3, School.Evocation, "A bright streak blossoms into an explosion: creatures within 6m make a Dexterity save, taking 8d6 fire damage or half on a success.", Sor, Wiz);
            s.target = TargetKind.Point; s.radius = 6f; s.range = 18; s.hasSave = true; s.saveAb = Ability.DEX; s.halfOnSave = true; s.dmg = "8d6"; s.dtype = DamageType.Fire; s.upcast = "1d6"; s.icon = "fire"; s.color = cFire; s.proj = ProjKind.Fire; s.fx = FxKind.Fire;
            s = Spell("lightning_bolt", "Lightning Bolt", 3, School.Evocation, "A 30m line of lightning: Dexterity save, 8d6 lightning damage or half on a success.", Sor, Wiz);
            s.target = TargetKind.Line; s.radius = 30f; s.lineWidth = 1.5f; s.hasSave = true; s.saveAb = Ability.DEX; s.halfOnSave = true; s.dmg = "8d6"; s.dtype = DamageType.Lightning; s.upcast = "1d6"; s.icon = "lightning"; s.color = cLight; s.fx = FxKind.Lightning;
            s = Spell("spirit_guardians", "Spirit Guardians", 3, School.Conjuration, "Spirits wheel around you for ten turns. Enemies that start their turn within 4.5m make a Wisdom save, taking 3d8 radiant damage or half on a success.", Clr);
            s.target = TargetKind.Self; s.who = Who.Ally; s.concentration = true; s.selfCond = Cond.SpiritGuardians; s.selfCondTurns = 10; s.dmg = "3d8"; s.dtype = DamageType.Radiant; s.upcast = "1d8"; s.icon = "holy"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.CastRaise; s.special = "spirit_guardians";
            s = Spell("mass_healing_word", "Mass Healing Word", 3, School.Evocation, "Bonus action: up to six allies within 18m regain 1d4 + your spellcasting modifier hit points.", Clr);
            s.cost = ActionCost.BonusAction; s.target = TargetKind.Aura; s.who = Who.Ally; s.radius = 18; s.heal = "1d4"; s.healAddMod = true; s.upcast = "1d4"; s.icon = "heal"; s.color = cHeal; s.fx = FxKind.Heal; s.anim = AnimAct.Bless;
            s = Spell("revivify", "Revivify", 3, School.Necromancy, "Touch a companion who has died within the last minute: they return to life with 1 hit point.", Clr, Pal);
            s.who = Who.DeadAlly; s.range = 1.5f; s.special = "revivify"; s.icon = "heart"; s.color = cHoly; s.fx = FxKind.Holy; s.anim = AnimAct.Heal;
            s = Spell("haste", "Haste", 3, School.Transmutation, "An ally gains +2 AC, double movement and an extra action each turn. When the spell ends they are Lethargic for a turn.", Sor, Wiz);
            s.who = Who.Ally; s.range = 9; s.concentration = true; s.conds.Add(new CondApply(Cond.Hasted, 10) { onlyOnFail = false }); s.icon = "dash"; s.color = cForce; s.fx = FxKind.Buff; s.anim = AnimAct.CastPoint;
            s = Spell("vampiric_touch", "Vampiric Touch", 3, School.Necromancy, "Melee spell attack for 3d6 necrotic damage; you regain half the damage dealt.", Wlk, Wiz);
            s.attack = AttackKind.MeleeSpell; s.range = 1.5f; s.dmg = "3d6"; s.dtype = DamageType.Necrotic; s.upcast = "1d6"; s.special = "drain"; s.icon = "fang"; s.color = cNec; s.fx = FxKind.Necrotic; s.anim = AnimAct.CastTouch;
            s = Spell("fear", "Fear", 3, School.Illusion, "Project a phantasmal image of dread in a 9m cone: Wisdom save or be Frightened.", Brd, Sor, Wlk, Wiz);
            s.target = TargetKind.Cone; s.radius = 9f; s.coneAngle = 60; s.hasSave = true; s.saveAb = Ability.WIS; s.concentration = true; s.conds.Add(C(Cond.Frightened, 3, true)); s.icon = "skull"; s.color = cPsy; s.fx = FxKind.Psychic;
            s = Spell("call_lightning", "Call Lightning", 3, School.Conjuration, "A storm cloud gathers and a bolt strikes: creatures within 1.5m make a Dexterity save, 3d10 lightning or half.", Dru);
            s.target = TargetKind.Point; s.radius = 1.8f; s.range = 36; s.hasSave = true; s.saveAb = Ability.DEX; s.halfOnSave = true; s.dmg = "3d10"; s.dtype = DamageType.Lightning; s.upcast = "1d10"; s.icon = "lightning"; s.color = cLight; s.fx = FxKind.Lightning; s.anim = AnimAct.CastRaise;
            s = Spell("daylight", "Daylight", 3, School.Evocation, "A sphere of true daylight bursts from a point. Undead within 6m make a Constitution save or take 4d6 radiant damage and are Blinded.", Clr, Dru, Pal, Rng, Sor);
            s.target = TargetKind.Point; s.radius = 6f; s.range = 18; s.hasSave = true; s.saveAb = Ability.CON; s.dmg = "4d6"; s.dtype = DamageType.Radiant; s.conds.Add(C(Cond.Blinded, 1)); s.onlyVs = new[] { CreatureType.Undead }; s.icon = "sun"; s.color = cRad; s.fx = FxKind.Radiant; s.anim = AnimAct.CastRaise;

            // =============================== class features ===============================
            var f = Feat("rage", "Rage", ActionCost.BonusAction, "Enter a primal fury: +2 melee damage, resistance to physical damage and advantage on Strength checks for ten turns.", "rage", new Color(.95f, .3f, .25f));
            f.resource = "rage"; f.selfCond = Cond.Raging; f.selfCondTurns = 10; f.fx = FxKind.Blood;
            f = Add(new ActionDef { id = "reckless_attack", name = "Reckless Attack", desc = "Throw caution aside: attack with advantage, but attacks against you have advantage until your next turn.", icon = "axe", color = new Color(.95f, .35f, .25f), attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, special = "reckless", anim = AnimAct.Overhead });
            f = Add(new ActionDef { id = "frenzy_attack", name = "Frenzied Strike", desc = "While Frenzied, make an extra melee attack as a bonus action.", cost = ActionCost.BonusAction, icon = "axe", color = cBonus, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, special = "frenzy", anim = AnimAct.Slash });
            f = Feat("bardic_inspiration", "Bardic Inspiration", ActionCost.BonusAction, "Inspire an ally within 18m: they can add a d6 to their next attack roll, ability check or saving throw.", "music", cHoly);
            f.target = TargetKind.Creature; f.who = Who.Ally; f.range = 18; f.resource = "inspiration"; f.conds.Add(new CondApply(Cond.Inspired, 10) { onlyOnFail = false }); f.fx = FxKind.Buff; f.anim = AnimAct.Cheer;
            f = Feat("turn_undead", "Turn Undead", ActionCost.Action, "Present your holy symbol: undead within 9m make a Wisdom save or are Turned, fleeing from you for three turns. At level 5, weak undead are destroyed outright.", "holy", cHoly);
            f.target = TargetKind.Aura; f.who = Who.Enemy; f.radius = 9; f.resource = "channel"; f.hasSave = true; f.saveAb = Ability.WIS; f.conds.Add(C(Cond.Turned, 3)); f.onlyVs = new[] { CreatureType.Undead }; f.special = "turn_undead"; f.fx = FxKind.Holy; f.anim = AnimAct.CastRaise;
            f = Feat("radiance_of_dawn", "Radiance of the Dawn", ActionCost.Action, "Channel Divinity: sunlight bursts from you. Enemies within 9m make a Constitution save, taking 2d10 + cleric level radiant damage, half on a success.", "sun", cRad);
            f.target = TargetKind.Aura; f.who = Who.Enemy; f.radius = 9; f.resource = "channel"; f.hasSave = true; f.saveAb = Ability.CON; f.halfOnSave = true; f.dmg = "2d10"; f.dtype = DamageType.Radiant; f.special = "radiance"; f.fx = FxKind.Radiant; f.anim = AnimAct.CastRaise;
            f = Feat("preserve_life", "Preserve Life", ActionCost.Action, "Channel Divinity: allies within 9m regain hit points equal to five times your cleric level, divided among them, up to half their maximum.", "heart", cHeal);
            f.target = TargetKind.Aura; f.who = Who.Ally; f.radius = 9; f.resource = "channel"; f.special = "preserve_life"; f.fx = FxKind.Heal; f.anim = AnimAct.Bless;
            f = Feat("destructive_wrath", "Destructive Wrath", ActionCost.Free, "Channel Divinity: your next thunder or lightning spell deals maximum damage.", "lightning", cLight);
            f.resource = "channel"; f.special = "destructive_wrath"; f.fx = FxKind.Lightning; f.anim = AnimAct.None;
            f = Add(new ActionDef { id = "war_priest", name = "War Priest Strike", desc = "Bonus action: make a weapon attack. Uses equal to your Wisdom modifier per long rest.", cost = ActionCost.BonusAction, icon = "mace", color = cBonus, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, resource = "warpriest", anim = AnimAct.Slash });
            f = Feat("wild_shape_wolf", "Wild Shape: Wolf", ActionCost.Action, "Transform into a wolf with its own hit points. Pack tactics and a tripping bite. You revert when its hit points reach 0.", "wolf", cNature);
            f.resource = "wildshape"; f.special = "wildshape:wolf"; f.fx = FxKind.Nature; f.outOfCombatOnly = false;
            f = Feat("wild_shape_bear", "Wild Shape: Bear", ActionCost.Action, "Transform into a brown bear: tough, strong and savage.", "bear", cNature);
            f.resource = "wildshape"; f.special = "wildshape:bear"; f.fx = FxKind.Nature;
            f = Feat("revert_form", "Revert Form", ActionCost.BonusAction, "Return to your normal form.", "leaf", cNature);
            f.special = "revert"; f.fx = FxKind.Nature;
            f = Feat("second_wind", "Second Wind", ActionCost.BonusAction, "Draw on your stamina to regain 1d10 + fighter level hit points.", "heart", cHeal);
            f.recharge = Recharge.ShortRest; f.maxUses = 1; f.heal = "1d10"; f.special = "second_wind"; f.fx = FxKind.Heal;
            f = Feat("action_surge", "Action Surge", ActionCost.Free, "Push beyond your limits: gain an additional action this turn.", "surge", new Color(1f, .8f, .3f));
            f.recharge = Recharge.ShortRest; f.maxUses = 1; f.special = "action_surge"; f.fx = FxKind.Buff;
            Add(new ActionDef { id = "trip_attack", name = "Trip Attack", desc = "Superiority manoeuvre: attack and add a d8 to damage. The target must succeed on a Strength save or fall Prone.", icon = "boot", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, resource = "superiority", dmg2 = "1d8", hasSave = true, saveAb = Ability.STR, conds = { C(Cond.Prone, 1) }, anim = AnimAct.Overhead });
            Add(new ActionDef { id = "menacing_attack", name = "Menacing Attack", desc = "Superiority manoeuvre: attack and add a d8 to damage. The target must succeed on a Wisdom save or be Frightened.", icon = "skull", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, resource = "superiority", dmg2 = "1d8", hasSave = true, saveAb = Ability.WIS, conds = { C(Cond.Frightened, 2) }, anim = AnimAct.Slash });
            Add(new ActionDef { id = "precision_attack", name = "Precision Attack", desc = "Superiority manoeuvre: attack with a d8 added to the attack roll.", icon = "eye", color = cMartial, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, resource = "superiority", special = "precision", anim = AnimAct.Thrust });
            Add(new ActionDef { id = "flurry", name = "Flurry of Blows", desc = "Spend 1 ki point: make two unarmed strikes as a bonus action.", cost = ActionCost.BonusAction, icon = "fist", color = cBonus, attack = AttackKind.MeleeWeapon, dmg = "1d4", dtype = DamageType.Bludgeoning, addMod = true, resource = "ki", multi = 2, special = "martial_arts", anim = AnimAct.Punch });
            Add(new ActionDef { id = "flurry_topple", name = "Flurry of Blows: Topple", desc = "Open Hand: two unarmed strikes; each hit forces a Dexterity save or the target falls Prone.", cost = ActionCost.BonusAction, icon = "boot", color = cBonus, attack = AttackKind.MeleeWeapon, dmg = "1d4", dtype = DamageType.Bludgeoning, addMod = true, resource = "ki", multi = 2, special = "martial_arts", hasSave = true, saveAb = Ability.DEX, conds = { C(Cond.Prone, 1) }, anim = AnimAct.Punch });
            Add(new ActionDef { id = "flurry_push", name = "Flurry of Blows: Push", desc = "Open Hand: two unarmed strikes; each hit forces a Strength save or pushes the target 5m.", cost = ActionCost.BonusAction, icon = "wind", color = cBonus, attack = AttackKind.MeleeWeapon, dmg = "1d4", dtype = DamageType.Bludgeoning, addMod = true, resource = "ki", multi = 2, special = "martial_arts", hasSave = true, saveAb = Ability.STR, pushM = 5, anim = AnimAct.Punch });
            Add(new ActionDef { id = "martial_bonus", name = "Martial Arts: Bonus Strike", desc = "After attacking with an unarmed strike or monk weapon, make an unarmed strike as a bonus action.", cost = ActionCost.BonusAction, icon = "fist", color = cBonus, attack = AttackKind.MeleeWeapon, dmg = "1d4", dtype = DamageType.Bludgeoning, addMod = true, special = "martial_arts", anim = AnimAct.Punch });
            f = Feat("patient_defense", "Patient Defence", ActionCost.BonusAction, "Spend 1 ki point to Dodge as a bonus action.", "shield", cBonus);
            f.resource = "ki"; f.selfCond = Cond.Dodging; f.selfCondTurns = 1; f.fx = FxKind.Buff; f.anim = AnimAct.Dodge;
            f = Feat("step_of_the_wind", "Step of the Wind", ActionCost.BonusAction, "Spend 1 ki point to Dash and Disengage as a bonus action.", "wind", cBonus);
            f.resource = "ki"; f.special = "step_wind"; f.fx = FxKind.Smoke; f.anim = AnimAct.None;
            Add(new ActionDef { id = "stunning_strike", name = "Stunning Strike", desc = "Spend 1 ki point: make an unarmed strike. On a hit the target must succeed on a Constitution save or be Stunned.", icon = "star", color = cMartial, attack = AttackKind.MeleeWeapon, dmg = "1d6", dtype = DamageType.Bludgeoning, addMod = true, resource = "ki", special = "martial_arts", hasSave = true, saveAb = Ability.CON, conds = { C(Cond.Stunned, 1) }, anim = AnimAct.Punch });
            f = Feat("lay_on_hands", "Lay on Hands", ActionCost.Action, "Touch an ally to restore hit points from your healing pool, which holds five points per paladin level.", "hand", cHeal);
            f.target = TargetKind.Creature; f.who = Who.Ally; f.range = 1.5f; f.special = "lay_on_hands"; f.fx = FxKind.Holy; f.anim = AnimAct.Heal;
            Add(new ActionDef { id = "divine_smite", name = "Divine Smite", desc = "Attack and pour holy power into the blow, expending a spell slot: +2d8 radiant damage (+1d8 per slot level above 1st, +1d8 against undead and fiends).", icon = "smite", color = cHoly, attack = AttackKind.MeleeWeapon, usesWeapon = true, addMod = true, special = "smite", fx = FxKind.Holy, anim = AnimAct.Overhead });
            f = Feat("sacred_weapon", "Sacred Weapon", ActionCost.Action, "Channel Divinity: imbue your weapon with light. Add your Charisma modifier to weapon attack rolls for ten turns.", "sword", cHoly);
            f.resource = "channel"; f.selfCond = Cond.SacredWeapon; f.selfCondTurns = 10; f.fx = FxKind.Holy;
            f = Feat("vow_of_enmity", "Vow of Enmity", ActionCost.BonusAction, "Channel Divinity: swear an oath against a creature within 3m. You have advantage on attacks against it for ten turns.", "mark", new Color(.9f, .3f, .3f));
            f.target = TargetKind.Creature; f.who = Who.Enemy; f.range = 3; f.resource = "channel"; f.conds.Add(new CondApply(Cond.VowOfEnmity, 10) { onlyOnFail = false }); f.fx = FxKind.Debuff;
            f = Feat("healing_radiance", "Healing Radiance", ActionCost.Action, "Channel Divinity (Ancients): allies within 9m regain 1d6 + Charisma modifier hit points.", "leaf", cHeal);
            f.target = TargetKind.Aura; f.who = Who.Ally; f.radius = 9; f.resource = "channel"; f.heal = "1d6"; f.healAddMod = true; f.fx = FxKind.Nature; f.anim = AnimAct.Bless;
            f = Feat("cunning_dash", "Cunning Action: Dash", ActionCost.BonusAction, "Dash as a bonus action.", "dash", cBonus);
            f.special = "dash"; f.anim = AnimAct.None;
            f = Feat("cunning_disengage", "Cunning Action: Disengage", ActionCost.BonusAction, "Disengage as a bonus action.", "wind", cBonus);
            f.selfCond = Cond.Disengaged; f.selfCondTurns = 1; f.anim = AnimAct.None;
            f = Feat("cunning_hide", "Cunning Action: Hide", ActionCost.BonusAction, "Hide as a bonus action.", "eye", cBonus);
            f.special = "hide"; f.anim = AnimAct.None;
            f = Feat("quickened_spell", "Metamagic: Quickened Spell", ActionCost.Free, "Spend 2 sorcery points: your next spell with a casting time of an action is cast as a bonus action instead.", "surge", cForce);
            f.resource = "sorcery"; f.resourceCost = 2; f.special = "quicken"; f.anim = AnimAct.None;
            f = Feat("twinned_spell", "Metamagic: Twinned Spell", ActionCost.Free, "Spend 1 sorcery point: your next single-target spell targets a second creature.", "surge", cForce);
            f.resource = "sorcery"; f.special = "twin"; f.anim = AnimAct.None;
            f = Feat("create_slot", "Create Spell Slot", ActionCost.BonusAction, "Spend 2 sorcery points to create a 1st-level spell slot.", "star", cForce);
            f.resource = "sorcery"; f.resourceCost = 2; f.special = "create_slot"; f.anim = AnimAct.None;
            f = Feat("fey_presence", "Fey Presence", ActionCost.Action, "Archfey: creatures in a 3m radius around you make a Wisdom save or are Frightened for a turn.", "star", cPsy);
            f.target = TargetKind.Aura; f.who = Who.Enemy; f.radius = 3; f.recharge = Recharge.ShortRest; f.maxUses = 1; f.hasSave = true; f.saveAb = Ability.WIS; f.conds.Add(C(Cond.Frightened, 1)); f.fx = FxKind.Psychic;
            f = Feat("pact_weapon", "Bind Pact Weapon", ActionCost.BonusAction, "Pact of the Blade: bond with your weapon. Attack with Charisma and treat it as magical.", "sword", cForce);
            f.special = "pact_weapon"; f.fx = FxKind.Shadow; f.anim = AnimAct.Draw;

            // =============================== racial ===============================
            f = Add(new ActionDef { id = "breath_weapon", name = "Breath Weapon", desc = "Exhale destructive energy in a 5m cone: Dexterity save, 2d6 damage of your ancestry's type (3d6 at level 5), half on a success.", target = TargetKind.Cone, radius = 5, coneAngle = 60, hasSave = true, saveAb = Ability.DEX, halfOnSave = true, dmg = "2d6", dtype = DamageType.Fire, recharge = Recharge.ShortRest, maxUses = 1, icon = "breath", color = cFire, fx = FxKind.Fire, anim = AnimAct.Breath, special = "breath" });
            f = Feat("enlarge", "Duergar Magic: Enlarge", ActionCost.Action, "Grow to Large size: +1d4 weapon damage and advantage on Strength checks.", "surge", new Color(.7f, .6f, .5f));
            f.recharge = Recharge.LongRest; f.maxUses = 1; f.selfCond = Cond.Enlarged; f.selfCondTurns = 10; f.fx = FxKind.Buff;
            f = Feat("duergar_invisibility", "Duergar Magic: Invisibility", ActionCost.Action, "Become invisible until you attack.", "eye", new Color(.7f, .6f, .5f));
            f.recharge = Recharge.LongRest; f.maxUses = 1; f.selfCond = Cond.Invisible; f.selfCondTurns = 10; f.fx = FxKind.Smoke;
            f = Feat("hidden_step", "Hidden Step", ActionCost.BonusAction, "Firbolg: turn invisible until the start of your next turn, or until you attack or cast a spell.", "eye", new Color(.45f, .7f, .55f));
            f.recharge = Recharge.ShortRest; f.maxUses = 1; f.selfCond = Cond.Invisible; f.selfCondTurns = 1; f.fx = FxKind.Smoke;
            f = Feat("githyanki_leap", "Enhanced Leap", ActionCost.BonusAction, "Githyanki Psionics: leap up to 9m through the air.", "wind", cForce);
            f.target = TargetKind.Point; f.range = 9; f.recharge = Recharge.LongRest; f.maxUses = 1; f.special = "teleport"; f.fx = FxKind.Force;

            // =============================== items ===============================
            Add(new ActionDef { id = "use_potion_healing", name = "Drink Potion of Healing", desc = "Regain 2d4+2 hit points.", cost = ActionCost.BonusAction, target = TargetKind.Creature, who = Who.Ally, range = 1.5f, heal = "2d4+2", icon = "potion", color = cHeal, fx = FxKind.Heal, anim = AnimAct.Drink, special = "consume" });
            Add(new ActionDef { id = "use_potion_greater", name = "Drink Potion of Greater Healing", desc = "Regain 4d4+4 hit points.", cost = ActionCost.BonusAction, target = TargetKind.Creature, who = Who.Ally, range = 1.5f, heal = "4d4+4", icon = "potion", color = cHeal, fx = FxKind.Heal, anim = AnimAct.Drink, special = "consume" });
            Add(new ActionDef { id = "use_elixir_vigilance", name = "Drink Elixir of Vigilance", desc = "+5 to initiative until your next long rest.", cost = ActionCost.BonusAction, target = TargetKind.Self, who = Who.Ally, icon = "potion", color = cForce, fx = FxKind.Buff, anim = AnimAct.Drink, special = "consume;vigilance" });
            Add(new ActionDef { id = "throw_holy_water", name = "Throw Holy Water", desc = "Undead and fiends within 1.5m of the impact take 2d6 radiant damage.", target = TargetKind.Point, radius = 1.5f, range = 9, autoHit = true, dmg = "2d6", dtype = DamageType.Radiant, onlyVs = new[] { CreatureType.Undead, CreatureType.Fiend }, icon = "flask", color = cHoly, fx = FxKind.Holy, anim = AnimAct.Throw, proj = ProjKind.Flask, special = "consume" });
            Add(new ActionDef { id = "throw_alchemist_fire", name = "Throw Alchemist's Fire", desc = "Creatures within 1.5m make a Dexterity save or take 1d4 fire damage and start Burning.", target = TargetKind.Point, radius = 1.5f, range = 9, hasSave = true, saveAb = Ability.DEX, dmg = "1d4", dtype = DamageType.Fire, conds = { C(Cond.Burning, 3) }, icon = "flask", color = cFire, fx = FxKind.Fire, anim = AnimAct.Throw, proj = ProjKind.Flask, special = "consume" });
            Add(new ActionDef { id = "revivify_scroll", name = "Read Scroll of Revivify", desc = "Return a dead companion to life with 1 hit point.", target = TargetKind.Creature, who = Who.DeadAlly, range = 1.5f, icon = "scroll", color = cHoly, fx = FxKind.Holy, anim = AnimAct.Heal, special = "revivify;consume" });
            Add(new ActionDef { id = "bless_scroll", name = "Read Scroll of Bless", desc = "Up to three allies add 1d4 to attack rolls and saving throws.", target = TargetKind.Creature, who = Who.Ally, range = 9, targets = 3, concentration = true, conds = { new CondApply(Cond.Blessed, 10) { onlyOnFail = false } }, icon = "scroll", color = cHoly, fx = FxKind.Holy, anim = AnimAct.Bless, special = "consume" });
            Add(new ActionDef { id = "magic_missile_scroll", name = "Read Scroll of Magic Missile", desc = "Three darts of force, 1d4+1 each.", range = 18, autoHit = true, targets = 3, distinctTargets = false, dmg = "1d4+1", dtype = DamageType.Force, icon = "scroll", color = cForce, proj = ProjKind.Force, fx = FxKind.Force, anim = AnimAct.CastPoint, special = "consume" });
            Add(new ActionDef { id = "eat_food", name = "Eat", desc = "Restore 1 hit point.", target = TargetKind.Self, who = Who.Ally, heal = "1", icon = "food", color = cHeal, fx = FxKind.None, anim = AnimAct.Drink, outOfCombatOnly = true, special = "consume" });

            // =============================== monster attacks ===============================
            M("bite_wolf", "Bite", "2d4+2", DamageType.Piercing, 4, "fang", AnimAct.Bite, c: C(Cond.Prone, 1), saveAb: Ability.STR, dc: 11, desc: "A snapping bite that can drag the target to the ground.");
            M("bite_dire_wolf", "Bite", "2d6+3", DamageType.Piercing, 5, "fang", AnimAct.Bite, c: C(Cond.Prone, 1), saveAb: Ability.STR, dc: 13);
            M("bites_swarm", "Bites", "2d4", DamageType.Piercing, 4, "fang", AnimAct.Bite);
            M("slam_zombie", "Slam", "1d6+1", DamageType.Bludgeoning, 3, "fist", AnimAct.Claw);
            M("claws_ghoul", "Claws", "2d4+2", DamageType.Slashing, 4, "claw", AnimAct.Claw, c: C(Cond.Paralyzed, 2, true), saveAb: Ability.CON, dc: 10, desc: "Filthy claws whose touch can paralyse the living (elves are immune).");
            M("bite_ghoul", "Bite", "2d6+2", DamageType.Piercing, 2, "fang", AnimAct.Bite);
            M("shortsword_skeleton", "Shortsword", "1d6+2", DamageType.Piercing, 4, "sword", AnimAct.Thrust);
            M("shortbow_skeleton", "Shortbow", "1d6+2", DamageType.Piercing, 4, "bow", AnimAct.Bow, range: 18, ranged: true);
            M("drain_shadow", "Strength Drain", "2d6+2", DamageType.Necrotic, 4, "skull", AnimAct.Claw, special: "strength_drain", desc: "A chill touch that saps the victim's strength.");
            M("life_drain_specter", "Life Drain", "3d6", DamageType.Necrotic, 4, "skull", AnimAct.Claw, special: "life_drain", desc: "Its touch withers the soul; the target's maximum hit points are reduced by the damage taken.");
            M("slam_armor", "Slam", "1d6+2", DamageType.Bludgeoning, 4, "fist", AnimAct.Punch, multi: 2);
            M("broom_attack", "Broomstick", "1d4", DamageType.Bludgeoning, 5, "staff", AnimAct.Slash, multi: 1);
            M("tentacles_grick", "Tentacles", "2d6+2", DamageType.Slashing, 4, "claw", AnimAct.Claw);
            M("dagger_cultist", "Ritual Dagger", "1d4+1", DamageType.Piercing, 3, "dagger", AnimAct.Thrust, multi: 1);
            M("claws_ghast", "Claws", "2d6+3", DamageType.Slashing, 5, "claw", AnimAct.Claw, c: C(Cond.Paralyzed, 2, true), saveAb: Ability.CON, dc: 10);
            M("slam_mound", "Slam", "1d8+3", DamageType.Bludgeoning, 6, "fist", AnimAct.Claw, multi: 2, desc: "Two crushing blows of rotting vegetation. Lorghoth can engulf a creature it hits with both.");
            M("claws_spawn", "Claws", "2d6+2", DamageType.Slashing, 5, "claw", AnimAct.Claw);
            M("bite_spawn", "Bite", "1d6+3", DamageType.Piercing, 6, "fang", AnimAct.Bite, special: "vampire_bite", desc: "A draining bite. The vampire regains hit points equal to the necrotic damage dealt.", dmg2: "1d4", t2: DamageType.Necrotic);
            M("claw_bear", "Claws", "2d6+4", DamageType.Slashing, 6, "claw", AnimAct.Claw, multi: 2);
            M("bite_wildwolf", "Bite", "2d4+2", DamageType.Piercing, 4, "fang", AnimAct.Bite, c: C(Cond.Prone, 1), saveAb: Ability.STR, dc: 11);
            M("bite_bat", "Bite", "1d6+2", DamageType.Piercing, 4, "fang", AnimAct.Bite);
            M("claw_strahd", "Unarmed Strike", "1d8+4", DamageType.Slashing, 9, "claw", AnimAct.Claw, multi: 2, dmg2: "3d6", t2: DamageType.Necrotic);
            M("scythe_mist", "Grave Scythe", "1d10+3", DamageType.Slashing, 5, "sword", AnimAct.Slash);
            M("bite_rat", "Bite", "1d4+2", DamageType.Piercing, 4, "fang", AnimAct.Bite, c: C(Cond.Poisoned, 2), saveAb: Ability.CON, dc: 10);
            M("mace_guard", "Mace", "1d6+1", DamageType.Bludgeoning, 3, "mace", AnimAct.Slash);
            // Lorghoth / animated armour specials
            var eng = Add(new ActionDef { id = "engulf_mound", name = "Engulf", desc = "Lorghoth pulls a Prone or Restrained creature into its rotting mass: Strength save or Restrained and 2d8+4 bludgeoning.", range = 1.5f, hasSave = true, saveAb = Ability.STR, fixedDC = 14, dmg = "2d8+4", dtype = DamageType.Bludgeoning, conds = { C(Cond.Restrained, 2, true) }, icon = "chain", color = cNature, anim = AnimAct.Claw });
            eng.fixedToHit = 0; eng.recharge = Recharge.ShortRest; eng.maxUses = 2; eng.fixedDC = 13; eng.dmg = "2d6+3";
            var wail = Add(new ActionDef { id = "wail_banshee", name = "Dread Wail", desc = "A shriek of grief: creatures within 9m make a Wisdom save or are Frightened.", target = TargetKind.Aura, radius = 9, hasSave = true, saveAb = Ability.WIS, fixedDC = 12, conds = { C(Cond.Frightened, 2) }, recharge = Recharge.ShortRest, maxUses = 1, icon = "skull", color = cPsy, fx = FxKind.Psychic, anim = AnimAct.Roar });
            wail.fixedToHit = 0;
            var charm = Add(new ActionDef { id = "strahd_charm", name = "Charm", desc = "Strahd fixes a creature with his gaze: Wisdom save or be Charmed.", range = 9, hasSave = true, saveAb = Ability.WIS, fixedDC = 17, conds = { C(Cond.Charmed, 3) }, icon = "eye", color = cPsy, fx = FxKind.Psychic, anim = AnimAct.CastPoint });
            charm.fixedToHit = 0;
        }

        static ActionDef M(string id, string name, string dmg, DamageType t, int toHit, string icon, AnimAct anim, CondApply c = null, Ability saveAb = Ability.STR, int dc = 0, float range = 1.5f, bool ranged = false, int multi = 1, string special = null, string desc = null, string dmg2 = null, DamageType t2 = DamageType.None)
        {
            var a = new ActionDef
            {
                id = id, name = name, dmg = dmg, dtype = t, fixedToHit = toHit, icon = icon, anim = anim, range = range, multi = multi, special = special,
                attack = ranged ? AttackKind.RangedWeapon : AttackKind.MeleeWeapon, desc = desc ?? $"Melee attack: +{toHit} to hit, {dmg} {t} damage.", dmg2 = dmg2, dtype2 = t2,
                proj = ranged ? ProjKind.Arrow : ProjKind.None, color = new Color(.85f, .4f, .35f)
            };
            if (c != null) { a.conds.Add(c); a.hasSave = dc > 0; a.saveAb = saveAb; a.fixedDC = dc; if (dc == 0) c.onlyOnFail = false; }
            return Add(a);
        }

        public static List<string> SpellsFor(ClassId c, int maxLevel)
        {
            if (!ClassSpells.TryGetValue(c, out var l)) return new List<string>();
            return l.Where(id => All[id].spellLevel <= maxLevel).OrderBy(id => All[id].spellLevel).ThenBy(id => All[id].name).ToList();
        }
    }
}
