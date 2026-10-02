using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Rules
{
    /// <summary>Static description of a condition's mechanical effects.</summary>
    public class CondDef
    {
        public Cond id;
        public string desc;
        public bool incapacitated, skipTurn, noMove, noReactions;
        public bool attackersAdv, attackersDis, attackDis, attackAdv, meleeAutoCrit;
        public bool dexSaveFail, strSaveFail, abilityCheckDis;
        public int acBonus, attackBonus, dmgBonus;
        public string bonusDie;        // added to attack rolls and saves (Bless, Bane is negative)
        public bool bonusDieNegative;
        public string dot; public DamageType dotType; // damage at turn start
        public bool endsOnDamage;      // Sleep
        public float speedMult = 1f;
        public bool positive;
        public Color color = Color.white;
    }

    public class CondInstance
    {
        public Cond id;
        public int turns;              // remaining rounds (-1 = until removed)
        public Creature source;
        public int dc; public Ability saveAb; public bool saveEachTurn;
        public string tag;             // e.g. concentration spell id
        public int value;              // generic payload (temp hp amount, damage bonus...)
    }

    public static class Conditions
    {
        public static readonly Dictionary<Cond, CondDef> Defs = new Dictionary<Cond, CondDef>();

        static void D(CondDef d) => Defs[d.id] = d;

        static Conditions()
        {
            var red = new Color(.85f, .3f, .3f); var green = new Color(.4f, .8f, .45f); var gold = new Color(.9f, .75f, .35f); var blue = new Color(.4f, .6f, .95f); var purple = new Color(.7f, .45f, .9f);
            D(new CondDef { id = Cond.Prone, desc = "Knocked to the ground. Melee attacks against it have advantage, ranged attacks have disadvantage. Standing up costs half its movement.", attackDis = true, color = red });
            D(new CondDef { id = Cond.Frightened, desc = "Has disadvantage on attack rolls and ability checks while the source of its fear is in sight, and cannot move closer to it.", attackDis = true, abilityCheckDis = true, color = purple });
            D(new CondDef { id = Cond.Poisoned, desc = "Has disadvantage on attack rolls and ability checks.", attackDis = true, abilityCheckDis = true, color = green });
            D(new CondDef { id = Cond.Paralyzed, desc = "Cannot move, act or react. Automatically fails Strength and Dexterity saves. Attacks against it have advantage; melee hits are critical.", incapacitated = true, skipTurn = true, noMove = true, attackersAdv = true, meleeAutoCrit = true, dexSaveFail = true, strSaveFail = true, color = purple });
            D(new CondDef { id = Cond.Restrained, desc = "Cannot move. Attacks against it have advantage and its own attacks have disadvantage.", noMove = true, attackersAdv = true, attackDis = true, color = green });
            D(new CondDef { id = Cond.Blinded, desc = "Cannot see. Its attacks have disadvantage; attacks against it have advantage.", attackDis = true, attackersAdv = true, color = new Color(.4f, .4f, .4f) });
            D(new CondDef { id = Cond.Charmed, desc = "Cannot attack the charmer.", color = purple });
            D(new CondDef { id = Cond.Stunned, desc = "Cannot move, act or react. Fails Strength and Dexterity saves; attacks against it have advantage.", incapacitated = true, skipTurn = true, noMove = true, attackersAdv = true, dexSaveFail = true, strSaveFail = true, color = gold });
            D(new CondDef { id = Cond.Incapacitated, desc = "Cannot take actions or reactions.", incapacitated = true, skipTurn = true, color = gold });
            D(new CondDef { id = Cond.Burning, desc = "Takes 1d4 fire damage at the start of each turn.", dot = "1d4", dotType = DamageType.Fire, color = new Color(1f, .5f, .15f) });
            D(new CondDef { id = Cond.Bleeding, desc = "Takes 1d4 piercing damage at the start of each turn.", dot = "1d4", dotType = DamageType.Piercing, color = red });
            D(new CondDef { id = Cond.Blessed, desc = "Adds 1d4 to attack rolls and saving throws.", bonusDie = "1d4", positive = true, color = gold });
            D(new CondDef { id = Cond.Baned, desc = "Subtracts 1d4 from attack rolls and saving throws.", bonusDie = "1d4", bonusDieNegative = true, color = purple });
            D(new CondDef { id = Cond.Hexed, desc = "The caster deals an extra 1d6 necrotic damage to it with each hit.", color = purple });
            D(new CondDef { id = Cond.HuntersMark, desc = "The ranger deals an extra 1d6 damage to it with each weapon hit.", color = green });
            D(new CondDef { id = Cond.Turned, desc = "Turned by divine power: must spend its turns fleeing and cannot take reactions.", noReactions = true, color = gold });
            D(new CondDef { id = Cond.Dodging, desc = "Attacks against it have disadvantage; it has advantage on Dexterity saves.", attackersDis = true, positive = true, color = blue });
            D(new CondDef { id = Cond.Raging, desc = "+2 melee damage, resistance to bludgeoning, piercing and slashing damage, advantage on Strength checks and saves.", dmgBonus = 2, positive = true, color = red });
            D(new CondDef { id = Cond.Hasted, desc = "+2 AC, doubled speed and an extra action each turn.", acBonus = 2, speedMult = 2f, positive = true, color = blue });
            D(new CondDef { id = Cond.Lethargic, desc = "Exhausted by haste: cannot move or act.", incapacitated = true, skipTurn = true, noMove = true, color = new Color(.5f, .5f, .5f) });
            D(new CondDef { id = Cond.ShieldSpell, desc = "+5 AC until the start of its next turn.", acBonus = 5, positive = true, color = blue });
            D(new CondDef { id = Cond.MageArmor, desc = "Base AC becomes 13 + Dexterity modifier.", positive = true, color = blue });
            D(new CondDef { id = Cond.ShieldOfFaith, desc = "+2 AC.", acBonus = 2, positive = true, color = gold });
            D(new CondDef { id = Cond.FaerieFire, desc = "Outlined in light: attacks against it have advantage.", attackersAdv = true, color = purple });
            D(new CondDef { id = Cond.Asleep, desc = "Unconscious until damaged or shaken awake.", incapacitated = true, skipTurn = true, noMove = true, attackersAdv = true, meleeAutoCrit = true, endsOnDamage = true, color = blue });
            D(new CondDef { id = Cond.Laughing, desc = "Prone and incapacitated with laughter; repeats the save whenever it takes damage.", incapacitated = true, skipTurn = true, noMove = true, attackersAdv = true, color = gold });
            D(new CondDef { id = Cond.Disengaged, desc = "Moving does not provoke opportunity attacks this turn.", positive = true, color = blue });
            D(new CondDef { id = Cond.Invisible, desc = "Attacks against it have disadvantage; its attacks have advantage.", attackersDis = true, attackAdv = true, positive = true, color = blue });
            D(new CondDef { id = Cond.ProtectionEvil, desc = "Aberrations, celestials, elementals, fey, fiends and undead have disadvantage attacking it.", positive = true, color = gold });
            D(new CondDef { id = Cond.Guidance, desc = "Adds 1d4 to its next ability check.", positive = true, color = gold });
            D(new CondDef { id = Cond.Resistance, desc = "Adds 1d4 to its next saving throw.", positive = true, color = gold });
            D(new CondDef { id = Cond.BladeWard, desc = "Resistant to bludgeoning, piercing and slashing damage from weapon attacks.", positive = true, color = blue });
            D(new CondDef { id = Cond.Reeling, desc = "Dazed by a blow: attack rolls have disadvantage.", attackDis = true, color = gold });
            D(new CondDef { id = Cond.GapingWound, desc = "Takes extra damage from every weapon hit.", color = red });
            D(new CondDef { id = Cond.Dazed, desc = "Cannot take reactions and has disadvantage on Wisdom saves.", noReactions = true, color = gold });
            D(new CondDef { id = Cond.Enlarged, desc = "Grown to Large size: +1d4 weapon damage, advantage on Strength checks.", positive = true, color = gold });
            D(new CondDef { id = Cond.Inspired, desc = "Can add a Bardic Inspiration die to its next attack roll, check or save.", positive = true, color = gold });
            D(new CondDef { id = Cond.Downed, desc = "Unconscious at 0 hit points and making death saving throws. Any damage counts as a failed save; a melee hit is a critical.", incapacitated = true, skipTurn = true, noMove = true, attackersAdv = true, meleeAutoCrit = true, color = red });
            D(new CondDef { id = Cond.Stabilized, desc = "Unconscious but no longer dying.", incapacitated = true, skipTurn = true, noMove = true, color = red });
            D(new CondDef { id = Cond.Dead, desc = "Dead.", incapacitated = true, skipTurn = true, noMove = true, color = new Color(.3f, .3f, .3f) });
            D(new CondDef { id = Cond.Concentrating, desc = "Maintaining a spell. Taking damage forces a Constitution save to keep it.", positive = true, color = blue });
            D(new CondDef { id = Cond.Wildshape, desc = "Transformed into a beast.", positive = true, color = green });
            D(new CondDef { id = Cond.SpiritGuardians, desc = "Spirits circle it, harming enemies who start their turn nearby.", positive = true, color = gold });
            D(new CondDef { id = Cond.Aided, desc = "Hit point maximum increased.", positive = true, color = gold });
            D(new CondDef { id = Cond.ArmorOfAgathys, desc = "Protected by spectral frost: attackers who hit it in melee take cold damage while the temporary hit points last.", positive = true, color = blue });
            D(new CondDef { id = Cond.WeakenedStrength, desc = "Its Strength has been drained by shadow. Returns after a long rest.", color = purple });
            D(new CondDef { id = Cond.Reckless, desc = "Attacking recklessly: advantage on melee attacks, but attacks against it have advantage too.", attackersAdv = true, color = red });
            D(new CondDef { id = Cond.Frenzied, desc = "Frenzied: can attack as a bonus action.", positive = true, color = red });
            D(new CondDef { id = Cond.SacredWeapon, desc = "Adds its Charisma modifier to weapon attack rolls.", positive = true, color = gold });
            D(new CondDef { id = Cond.VowOfEnmity, desc = "Its sworn enemy: the paladin has advantage attacking it.", color = red });
            D(new CondDef { id = Cond.Chilled, desc = "Speed reduced by 3m.", speedMult = 0.7f, color = blue });
            D(new CondDef { id = Cond.Silenced, desc = "Cannot cast spells with a verbal component.", color = purple });
            D(new CondDef { id = Cond.Frozen, desc = "Frozen solid: cannot move and attacks against it have advantage.", noMove = true, attackersAdv = true, color = blue });
            D(new CondDef { id = Cond.Wet, desc = "Drenched: resistant to fire, vulnerable to cold and lightning.", color = blue });
            D(new CondDef { id = Cond.BlessedByChoice, desc = "Blessed.", positive = true, color = gold });
            D(new CondDef { id = Cond.Heroism, desc = "Immune to fear and gains temporary hit points each turn.", positive = true, color = gold });
            D(new CondDef { id = Cond.Sanctuary, desc = "Cannot be targeted by enemy attacks until it attacks.", positive = true, color = gold });
            D(new CondDef { id = Cond.Webbed, desc = "Stuck in webbing: restrained.", noMove = true, attackersAdv = true, attackDis = true, color = new Color(.8f, .8f, .8f) });
            D(new CondDef { id = Cond.Ensnared, desc = "Caught by thorny vines: restrained and takes piercing damage each turn.", noMove = true, attackersAdv = true, attackDis = true, dot = "1d6", dotType = DamageType.Piercing, color = green });
            D(new CondDef { id = Cond.DivineFavor, desc = "Weapon attacks deal an extra 1d4 radiant damage.", positive = true, color = gold });
            D(new CondDef { id = Cond.Darkvision, desc = "Can see in the dark.", positive = true, color = blue });
        }

        public static CondDef Get(Cond c) => Defs.TryGetValue(c, out var d) ? d : new CondDef { id = c, desc = c.Nice() };
    }
}
