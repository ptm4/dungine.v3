using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Rules
{
    public enum TargetKind { Self, Creature, Point, Cone, Line, Aura, Weapon }
    public enum Who { Enemy, Ally, Any, DownedAlly, DeadAlly }
    public enum AttackKind { None, MeleeWeapon, RangedWeapon, MeleeSpell, RangedSpell }
    public enum Recharge { None, Turn, ShortRest, LongRest }
    public enum ProjKind { None, Arrow, Bolt, Fire, Frost, Lightning, Radiant, Necrotic, Force, Acid, Poison, Psychic, Thorn, Flask }
    public enum FxKind { None, Slash, Fire, Frost, Lightning, Radiant, Necrotic, Force, Acid, Poison, Psychic, Thunder, Heal, Buff, Debuff, Holy, Shadow, Nature, Blood, Smoke }

    public class CondApply
    {
        public Cond cond;
        public int turns = 2;
        public bool saveEachTurn;
        public bool onlyOnFail = true;
        public CondApply(Cond c, int t, bool repeat = false) { cond = c; turns = t; saveEachTurn = repeat; }
    }

    /// <summary>
    /// One thing a creature can do: weapon attacks, weapon actions, spells, class features, items and monster attacks.
    /// Most behaviour is data; special cases hook in through <see cref="special"/>.
    /// </summary>
    public class ActionDef
    {
        public string id, name, desc;
        public string icon = "sword";
        public Color color = new Color(.8f, .75f, .6f);
        public ActionCost cost = ActionCost.Action;
        public int spellLevel = -1;                 // -1 = not a spell, 0 = cantrip
        public School school;
        public bool concentration;
        public int durationTurns;

        public TargetKind target = TargetKind.Creature;
        public Who who = Who.Enemy;
        public float range = 1.5f;                   // metres
        public float radius;                        // aoe radius / cone length / line length
        public float coneAngle = 60f;
        public float lineWidth = 1.5f;
        public int targets = 1;                      // number of separate creature picks (Bless = 3, Magic Missile darts)
        public int targetsPerUpcast;
        public bool distinctTargets = true;

        public AttackKind attack;
        public bool usesWeapon;                      // damage from equipped weapon
        public bool offHand;
        public bool autoHit;                         // Magic Missile
        public bool hasSave; public Ability saveAb; public bool halfOnSave;
        public string dmg; public DamageType dtype; public bool addMod;
        public string dmg2; public DamageType dtype2;
        public string heal; public bool healAddMod;
        public string upcast;                        // extra dice per slot above base
        public bool cantripScales;                   // x2 dice at level 5
        public float dmgMult = 1f;                   // cleave etc.
        public int pushM;                            // push on hit / failed save, metres
        public List<CondApply> conds = new List<CondApply>();
        public Cond? selfCond; public int selfCondTurns;
        public int tempHP;

        public Recharge recharge;
        public int maxUses;                          // 0 = uses a shared resource or unlimited
        public string resource;                      // "rage", "ki", "channel", "sorcery", "superiority", "inspiration", "layonhands", "wildshape"
        public int resourceCost = 1;
        public bool weaponAction;                    // BG3 weapon action (short rest)

        public int fixedToHit = int.MinValue;        // monsters
        public int fixedDC;                          // monsters
        public int multi = 1;                        // monster multiattack count

        public string special;                       // hook id for bespoke behaviour
        public ProjKind proj;
        public FxKind fx;
        public Visual.AnimAct anim = Visual.AnimAct.Slash;
        public bool outOfCombatOnly, combatOnly;
        public bool ritual;
        public bool hidden;                          // not shown on hotbar
        public string grantedBy;                     // UI note
        public CreatureType[] onlyVs;                // holy water etc.

        public bool IsSpell => spellLevel >= 0;
        public bool IsAttack => attack != AttackKind.None;
        public bool IsAoE => target == TargetKind.Point && radius > 0 || target == TargetKind.Cone || target == TargetKind.Line || target == TargetKind.Aura;
        public bool IsHeal => !string.IsNullOrEmpty(heal);

        public ActionDef Clone() => (ActionDef)MemberwiseClone();

        public string CostLabel()
        {
            switch (cost)
            {
                case ActionCost.BonusAction: return "Bonus Action";
                case ActionCost.Reaction: return "Reaction";
                case ActionCost.Free: return "Free";
                default: return "Action";
            }
        }

        public string LevelLabel() => spellLevel < 0 ? "" : spellLevel == 0 ? "Cantrip" : $"Level {spellLevel} {school}";
    }
}
