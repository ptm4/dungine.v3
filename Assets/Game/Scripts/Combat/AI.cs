using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine.Combat
{
    /// <summary>Decision making for monsters and hostile NPCs.</summary>
    public static class AI
    {
        class Plan { public ActionDef a; public Creature t; public float score; public Vector3 point; }

        public static IEnumerator TakeTurn(Creature u)
        {
            var cm = CombatManager.I;
            if (u.actor == null) yield break;
            // Turned / frightened: run away from the source
            var turned = u.Get(Cond.Turned);
            if (turned != null && turned.source != null && turned.source.actor != null)
            {
                yield return FleeFrom(u, turned.source.actor.transform.position);
                yield break;
            }
            int safety = 6;
            while (safety-- > 0 && u.Active && cm.Active)
            {
                var plan = Choose(u);
                if (plan == null)
                {
                    // nothing in reach: close in on the nearest enemy
                    var near = cm.Hostiles(u).Where(h => h.actor && !h.dead).OrderBy(h => RulesEngine.Dist(u, h)).FirstOrDefault();
                    if (near == null || u.moveLeft < 0.3f) break;
                    if (!u.hasAction && !u.hasBonus) break;
                    yield return MoveToward(u, near, 1.2f);
                    if (Choose(u) == null) break;
                    continue;
                }
                float range = RulesEngine.Range(u, plan.a);
                if (plan.t != null && plan.t != u && RulesEngine.Dist(u, plan.t) > range + 0.2f)
                {
                    yield return MoveToward(u, plan.t, range * 0.8f);
                    if (!u.Active) yield break;
                    if (RulesEngine.Dist(u, plan.t) > range + 0.35f) { if (u.moveLeft < 0.3f) break; else continue; }
                }
                var ctx = new ActionContext { a = plan.a, user = u, point = plan.point };
                if (plan.t != null) ctx.targets.Add(plan.t);
                if (plan.a.targets > 1 && plan.t != null)
                {
                    var extra = cm.Hostiles(u).Where(h => h != plan.t && RulesEngine.Dist(u, h) <= range).Take(plan.a.targets - 1);
                    ctx.targets.AddRange(extra);
                    while (ctx.targets.Count < plan.a.targets && !plan.a.distinctTargets) ctx.targets.Add(plan.t);
                }
                yield return cm.Perform(ctx);
                yield return new WaitForSeconds(0.25f);
                if (!u.hasAction && !u.hasBonus) break;
            }
            // ranged creatures back off after acting if an enemy is adjacent
            if (u.Active && u.moveLeft > 1.5f && u.AvailableActions().Any(a => a.attack == AttackKind.RangedWeapon || a.attack == AttackKind.RangedSpell) && cm.HostileWithin(u, 1.6f) && !u.AvailableActions().Any(a => a.attack == AttackKind.MeleeWeapon))
            {
                var h = cm.Hostiles(u).OrderBy(x => RulesEngine.Dist(u, x)).First();
                yield return FleeFrom(u, h.actor.transform.position, 4f);
            }
        }

        static Plan Choose(Creature u)
        {
            var cm = CombatManager.I;
            var enemies = cm.Hostiles(u).Where(h => h.actor != null).ToList();
            if (enemies.Count == 0) return null;
            Plan best = null;
            foreach (var a in u.AvailableActions())
            {
                if (a.id == "dash") continue;
                if (a.cost == ActionCost.Reaction) continue;   // reactions fire on their own triggers
                if (!RulesEngine.CanUse(u, a, true, out _)) continue;
                float range = RulesEngine.Range(u, a);
                if (a.IsAoE)
                {
                    foreach (var e in enemies)
                    {
                        if (e.dead) continue;
                        float dd = RulesEngine.Dist(u, e);
                        if (dd > range + u.moveLeft) continue;
                        var victims = a.target == TargetKind.Aura ? Targeting.CollectArea(a, u, u.actor.transform.position) : Targeting.CollectArea(a, u, e.actor.transform.position);
                        float s = victims.Sum(v => RulesEngine.Hostile(u, v) ? 12f * Mathf.Min(1f, TypeMul(u, a, v) + 0.2f) : -10f) + victims.Where(v => RulesEngine.Hostile(u, v)).Sum(v => Avg(a) * TypeMul(u, a, v)) * 0.6f;
                        if (a.target == TargetKind.Aura && victims.Count(v => RulesEngine.Hostile(u, v)) == 0) s = -1;
                        if (best == null || s > best.score) best = new Plan { a = a, t = a.target == TargetKind.Aura ? u : null, point = e.actor.transform.position, score = s };
                    }
                    continue;
                }
                if (a.who == Who.Ally || a.target == TargetKind.Self)
                {
                    if (a.IsHeal)
                    {
                        var hurt = cm.All.Where(c => c.Active && RulesEngine.Allied(u, c) && c.hp < c.MaxHPTotal * 0.4f && (a.target != TargetKind.Self || c == u)).OrderBy(c => c.hp).FirstOrDefault();
                        if (hurt != null) { float s = 25; if (best == null || s > best.score) best = new Plan { a = a, t = hurt, score = s }; }
                    }
                    else if (a.selfCond.HasValue && !u.Has(a.selfCond.Value) && a.id != "disengage" && a.id != "dodge" && a.id != "hide")
                    {
                        float s = 9; if (best == null || s > best.score) best = new Plan { a = a, t = u, score = s };
                    }
                    continue;
                }
                foreach (var e in enemies)
                {
                    if (e.dead) continue;
                    if (a.onlyVs != null && !a.onlyVs.Contains(e.type)) continue;
                    float d = RulesEngine.Dist(u, e);
                    float reachable = range + u.moveLeft;
                    if (d > reachable + 0.2f) continue;
                    float chance = 1f;
                    if (a.IsAttack && !a.autoHit) chance = RulesEngine.AttackPreview(u, e, a).chance;
                    else if (a.hasSave) chance = 1f - D20.Chance(e.SaveBonus(a.saveAb), RulesEngine.SaveDC(u, a), RollMode.Normal, false);
                    float s = chance * Avg(a) * Mathf.Max(1, a.multi) * TypeMul(u, a, e);
                    if (a.conds.Count > 0) s += chance * 6f;
                    if (e.hp <= 0) s *= 0.15f;                   // downed PCs are low priority
                    if (e.hp > 0 && e.hp <= Avg(a)) s += 6f;       // finish off
                    if (!string.IsNullOrEmpty(e.concSpell)) s += 3f;
                    s += (20 - e.AC) * 0.3f;
                    if (d > range) s -= 1.5f;                      // needs to move
                    if (a.cost == ActionCost.BonusAction && u.hasAction && !a.IsAttack) s -= 2;
                    if (e.Has(Cond.Sanctuary)) s = -1;
                    if (a.id == "shove") s = -1f;   // shoving is a tactic for players; the AI wastes turns with it
                    if (u.Has(Cond.Frightened) && d > range) s -= 10;
                    if (best == null || s > best.score) best = new Plan { a = a, t = e, score = s };
                }
            }
            return best != null && best.score > 0 ? best : null;
        }

        /// <summary>How much of the damage actually lands given the target's immunities, resistances and vulnerabilities.</summary>
        static float TypeMul(Creature u, ActionDef a, Creature e)
        {
            if (string.IsNullOrEmpty(a.dmg) && !a.usesWeapon) return 1f;
            DamageType t = a.dtype;
            if (a.usesWeapon) { var w = RulesEngine.WeaponFor(u, a); if (w != null) t = w.dtype; }
            if (e.immune.Contains(t)) return 0f;
            float m = 1f;
            if (e.Resists(t)) m *= 0.5f;
            if (e.vuln.Contains(t)) m *= 2f;
            return m;
        }

        static float Avg(ActionDef a)
        {
            if (!string.IsNullOrEmpty(a.dmg)) return DiceExpr.Parse(a.dmg).Average + (string.IsNullOrEmpty(a.dmg2) ? 0 : DiceExpr.Parse(a.dmg2).Average);
            if (a.usesWeapon) return 6;
            return 3;
        }

        static IEnumerator MoveToward(Creature u, Creature t, float stopAt)
        {
            if (u.moveLeft <= 0.2f || t.actor == null || u.actor == null) yield break;
            if (u.conds.Any(c => Conditions.Get(c.id).noMove)) yield break;
            Vector3 tp = t.actor.transform.position;
            Vector3 dir = u.actor.transform.position - tp; dir.y = 0;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir.Normalize();
            Vector3 dest = tp + dir * Mathf.Max(0.6f, stopAt + t.actor.radius * 0.4f);
            if (NavMesh.SamplePosition(dest, out var nh, 2f, NavMesh.AllAreas)) dest = nh.position;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(u.actor.transform.position, dest, NavMesh.AllAreas, path) || path.corners.Length < 2) yield break;
            yield return CombatManager.I.MoveRoutine(u, path.corners, u.moveLeft, true);
        }

        static IEnumerator FleeFrom(Creature u, Vector3 from, float dist = 12f)
        {
            if (u.actor == null) yield break;
            Vector3 dir = u.actor.transform.position - from; dir.y = 0; dir.Normalize();
            Vector3 dest = u.actor.transform.position + dir * Mathf.Min(dist, u.moveLeft);
            if (!NavMesh.SamplePosition(dest, out var nh, 3f, NavMesh.AllAreas)) yield break;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(u.actor.transform.position, nh.position, NavMesh.AllAreas, path)) yield break;
            u.AddCond(Cond.Disengaged, 1, u);
            yield return CombatManager.I.MoveRoutine(u, path.corners, u.moveLeft, true);
        }
    }
}
