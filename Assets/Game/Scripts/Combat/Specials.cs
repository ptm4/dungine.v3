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
    /// <summary>Actions whose behaviour isn't pure data.</summary>
    public static class Specials
    {
        static string Name(Creature c) => RulesEngine.Name(c);

        public static bool TryHandle(ActionContext ctx, bool inCombat, out IEnumerator routine)
        {
            routine = null;
            var u = ctx.user; var a = ctx.a;
            string sp = a.special;
            if (sp.Contains("vigilance")) { u.vigilance = true; FloatingText.Show(u.actor.HeadPos, "Vigilant", Theme.Magic); }
            switch (sp.Split(';')[0])
            {
                case "dash":
                    u.moveLeft += u.Speed; FloatingText.Show(u.actor.HeadPos, "Dash", Theme.MoveYellow); Audio.Sfx.Play("dash"); return true;
                case "step_wind":
                    u.moveLeft += u.Speed; u.AddCond(Cond.Disengaged, 1, u); FloatingText.Show(u.actor.HeadPos, "Step of the Wind", Theme.MoveYellow); FX.Burst(u.actor.Chest, FxKind.Smoke, 0.8f); return true;
                case "hide":
                    routine = Hide(ctx); return true;
                case "shove":
                    routine = Shove(ctx); return true;
                case "help":
                    routine = Help(ctx); return true;
                case "teleport":
                    routine = Teleport(ctx); return true;
                case "false_life":
                    {
                        int thp = DiceExpr.Parse("1d4+4").Roll() + (ctx.slot > 1 ? (ctx.slot - 1) * 5 : 0);
                        u.tempHP = Mathf.Max(u.tempHP, thp); u.actor.PlayAnim(AnimAct.Bless);
                        FloatingText.Show(u.actor.HeadPos, $"+{thp} temp HP", Theme.Magic); FX.Burst(u.actor.Chest, FxKind.Necrotic, 1f); return true;
                    }
                case "action_surge":
                    if (!u.hasAction) u.hasAction = true; else u.actionsExtra++;
                    FloatingText.Show(u.actor.HeadPos, "Action Surge!", Theme.Gold, 1.2f); FX.Burst(u.actor.Chest, FxKind.Buff, 1.2f); Audio.Sfx.Play("buff"); u.actor.PlayAnim(AnimAct.Roar); return true;
                case "quicken":
                    u.pendingMeta = "quicken"; FloatingText.Show(u.actor.HeadPos, "Quickened", Theme.Magic); return true;
                case "twin":
                    u.pendingMeta = "twin"; FloatingText.Show(u.actor.HeadPos, "Twinned", Theme.Magic); return true;
                case "create_slot":
                    if (u.slotsUsed[1] > 0) u.slotsUsed[1]--; else u.slotsMax[1]++;
                    FloatingText.Show(u.actor.HeadPos, "+1 spell slot", Theme.Magic); return true;
                case "destructive_wrath":
                    u.destructiveWrath = true; FloatingText.Show(u.actor.HeadPos, "Destructive Wrath", Theme.Magic); FX.Burst(u.actor.Chest, FxKind.Lightning, 1f); return true;
                case "pact_weapon":
                    u.pactWeapon = true; FloatingText.Show(u.actor.HeadPos, "Pact Weapon bound", Theme.Magic); FX.Burst(u.actor.Chest, FxKind.Shadow, 1f); return true;
                case "revivify":
                    routine = Revivify(ctx); return true;
                case "wildshape:wolf":
                case "wildshape:bear":
                    routine = Wildshape.Transform(u, sp.Split(';')[0].EndsWith("wolf") ? "wildshape_wolf" : "wildshape_bear"); return true;
                case "revert":
                    Wildshape.Revert(u); return true;
                case "consume":
                    if (a.target == TargetKind.Self || a.IsHeal || a.id.StartsWith("use_")) return false; // fall through to generic handling
                    return false;
            }
            return false;
        }

        static IEnumerator Hide(ActionContext ctx)
        {
            var u = ctx.user;
            int best = 0;
            foreach (var e in CombatManager.I.Hostiles(u)) best = Mathf.Max(best, e.PassivePerception);
            if (!CombatManager.I.Active) best = 12;
            bool adv = u.Subrace != null && u.Subrace.stealthAdv;
            var r = D20.Roll(u.SkillBonus(Skill.Stealth), adv ? RollMode.Advantage : RollMode.Normal, u.Race != null && u.Race.lucky);
            bool ok = r.total >= best;
            CombatLog.Add($"{Name(u)} tries to hide: Stealth {r.total} vs {best} — {(ok ? "hidden" : "spotted")}.");
            if (ok) { u.AddCond(Cond.Invisible, 2, u); FloatingText.Show(u.actor.HeadPos, "Hidden", Theme.Magic); FX.Burst(u.actor.Chest, FxKind.Smoke, 0.7f); }
            else FloatingText.Show(u.actor.HeadPos, "Spotted", Theme.Failure);
            yield return new WaitForSeconds(0.4f);
        }

        static IEnumerator Shove(ActionContext ctx)
        {
            var u = ctx.user; var t = ctx.targets.FirstOrDefault();
            if (t == null) yield break;
            bool done = false; u.actor.PlayAnim(AnimAct.Shove, () => done = true);
            while (!done) yield return null;
            if (t.size > u.size + 1) { FloatingText.Show(t.actor.HeadPos, "Too large", Theme.TextDim); yield break; }
            var ra = D20.Roll(u.SkillBonus(Skill.Athletics), RollMode.Normal);
            int defBonus = Mathf.Max(t.SkillBonus(Skill.Athletics), t.SkillBonus(Skill.Acrobatics));
            var rd = D20.Roll(defBonus, RollMode.Normal);
            bool ok = ra.total >= rd.total;
            CombatLog.Add($"{Name(u)} shoves {Name(t)}: {ra.total} vs {rd.total} — {(ok ? "pushed back" : "resisted")}.");
            if (ok) { RulesEngine.Push(u, t, 3f); Audio.Sfx.Play("blunt_hit"); }
            else FloatingText.Show(t.actor.HeadPos, "Resisted", Theme.TextDim);
            yield return new WaitForSeconds(0.3f);
        }

        static IEnumerator Help(ActionContext ctx)
        {
            var u = ctx.user; var t = ctx.targets.FirstOrDefault();
            if (t == null) yield break;
            bool done = false; u.actor.PlayAnim(AnimAct.Interact, () => done = true);
            while (!done) yield return null;
            if (t.hp <= 0 && !t.dead) { RulesEngine.Revive(t, 1); CombatLog.Add($"{Name(u)} helps {Name(t)} back to their feet."); FloatingText.Show(t.actor.HeadPos, "Helped up", Theme.Friendly); }
            else { t.RemoveCond(Cond.Asleep); t.RemoveCond(Cond.Prone); FloatingText.Show(t.actor.HeadPos, "Helped", Theme.Friendly); }
            yield return new WaitForSeconds(0.3f);
        }

        static IEnumerator Teleport(ActionContext ctx)
        {
            var u = ctx.user;
            Vector3 dest = ctx.point;
            if (NavMesh.SamplePosition(dest, out var hit, 2f, NavMesh.AllAreas)) dest = hit.position; else yield break;
            FX.Burst(u.actor.Chest, FxKind.Smoke, 1.2f);
            Audio.Sfx.Play("teleport");
            foreach (var r in u.actor.renderers) if (r) r.enabled = false;
            yield return new WaitForSeconds(0.25f);
            u.actor.Warp(dest);
            FX.Burst(dest + Vector3.up * 1f, FxKind.Smoke, 1.2f);
            yield return new WaitForSeconds(0.15f);
            foreach (var r in u.actor.renderers) if (r) r.enabled = true;
        }

        static IEnumerator Revivify(ActionContext ctx)
        {
            var u = ctx.user; var t = ctx.targets.FirstOrDefault();
            if (t == null) yield break;
            bool done = false; u.actor.PlayAnim(AnimAct.Heal, () => done = true);
            while (!done) yield return null;
            if (t.dead || t.hp <= 0)
            {
                RulesEngine.Revive(t, 1);
                CombatLog.Add($"{Name(t)} is brought back to life!");
                FX.Burst(t.actor.Chest, FxKind.Holy, 1.6f);
                Audio.Sfx.Play("revive");
            }
            yield return new WaitForSeconds(0.4f);
        }

        public static void Sleep(ActionContext ctx, List<Creature> victims)
        {
            var u = ctx.user;
            int pool = DiceExpr.Parse("5d8").Roll() + (ctx.slot > 1 ? DiceExpr.Parse($"{(ctx.slot - 1) * 2}d8").Roll() : 0);
            CombatLog.Add($"Sleep affects up to {pool} hit points of creatures.");
            foreach (var v in victims.Where(v => !v.dead && v.hp > 0 && v != u).OrderBy(v => v.hp))
            {
                if (v.type == CreatureType.Undead || v.type == CreatureType.Construct || v.condImmune.Contains(Cond.Asleep) || (v.Race != null && v.Race.feyAncestry)) continue;
                if (v.hp > pool) break;
                pool -= v.hp;
                v.AddCond(Cond.Asleep, 2, u);
                FloatingText.Show(v.actor.HeadPos + Vector3.up * 0.4f, "Asleep", Theme.Magic);
                CombatLog.Add($"{Name(v)} falls asleep.");
            }
        }

        public static void TurnUndead(ActionContext ctx, List<Creature> victims)
        {
            var u = ctx.user; int dc = RulesEngine.SaveDC(u, ctx.a);
            foreach (var v in victims)
            {
                if (v.type != CreatureType.Undead || !RulesEngine.Hostile(u, v) || v.dead) continue;
                var sr = RulesEngine.RollSave(v, Ability.WIS, dc, u, ctx.a);
                if (sr.success) { FloatingText.Show(v.actor.HeadPos, "Resisted", Theme.TextDim); continue; }
                if (u.level >= 5 && v.xpValue <= 100 && v.mdef != null)
                {
                    CombatLog.Add($"{Name(v)} is destroyed by holy light!");
                    FX.Burst(v.actor.Chest, FxKind.Radiant, 1.5f);
                    RulesEngine.Kill(v, u, ctx.a);
                    continue;
                }
                v.AddCond(Cond.Turned, 3, u);
                FloatingText.Show(v.actor.HeadPos + Vector3.up * 0.4f, "Turned", Theme.Gold);
                CombatLog.Add($"{Name(v)} is turned and flees!");
            }
        }

        public static void PreserveLife(ActionContext ctx, List<Creature> allies)
        {
            var u = ctx.user;
            int pool = 5 * u.level;
            foreach (var t in allies.Where(t => !t.dead && t.hp < t.MaxHPTotal / 2).OrderBy(t => t.hp))
            {
                int cap = t.MaxHPTotal / 2 - Mathf.Max(0, t.hp);
                int give = Mathf.Min(cap, pool);
                if (give <= 0) continue;
                pool -= give;
                RulesEngine.Heal(u, t, give, null);
                FX.Burst(t.actor.Chest, FxKind.Heal, 1f);
                if (pool <= 0) break;
            }
        }

        public static void IceKnifeBurst(ActionContext ctx)
        {
            var u = ctx.user; var t = ctx.targets.FirstOrDefault();
            if (t == null || t.actor == null) return;
            Vector3 p = t.actor.transform.position;
            FX.Burst(p + Vector3.up * 0.5f, FxKind.Frost, 1.5f);
            int dc = RulesEngine.SaveDC(u, ctx.a);
            int dice = 2 + Mathf.Max(0, ctx.slot - 1);
            foreach (var v in CombatManager.I.All.Where(c => !c.dead && c.actor && RulesEngine.FlatDist(c.actor.transform.position, p) <= 1.6f))
            {
                var sr = RulesEngine.RollSave(v, Ability.DEX, dc, u, ctx.a);
                if (!sr.success) RulesEngine.DealDamage(u, v, DiceExpr.Parse($"{dice}d6").Roll(), DamageType.Cold, false, ctx.a);
            }
        }
    }

    /// <summary>Druid Wild Shape: swap the creature's physical stats and body for a beast's.</summary>
    public static class Wildshape
    {
        public static IEnumerator Transform(Creature u, string beast)
        {
            var md = Monsters.Get(beast);
            if (md == null || u.actor == null) yield break;
            FX.Burst(u.actor.Chest, FxKind.Nature, 1.6f);
            Audio.Sfx.Play("wildshape");
            yield return new WaitForSeconds(0.3f);
            var backup = new Creature { abil = (int[])u.abil.Clone(), hp = u.hp, maxHP = u.maxHP, baseAC = u.baseAC, baseSpeed = u.baseSpeed, mdef = u.mdef, size = u.size, type = u.type, tempHP = u.tempHP };
            u.wildshapeBackup = backup;
            u.wildForm = beast;
            u.mdef = md;
            u.abil[0] = md.abil[0]; u.abil[1] = md.abil[1]; u.abil[2] = md.abil[2];
            int hp = md.hp + (u.IsSub("moon") ? u.level * 3 : 0);
            u.maxHP = hp; u.hp = hp; u.baseAC = md.ac + (u.IsSub("moon") ? 1 : 0); u.baseSpeed = md.speedM; u.size = md.size; u.type = CreatureType.Beast;
            RulesEngine.EndConcentration(u);
            var old = u.actor; var pos = old.transform.position; var rot = old.transform.rotation;
            bool sel = Game.I.Selected == old;
            var parent = old.transform.parent;
            Object.Destroy(old.gameObject);
            var na = ActorFactory.Spawn(u, pos, rot);
            na.transform.SetParent(parent, true);
            na.SetRing(sel, Theme.Gold);
            FX.Burst(na.Chest, FxKind.Nature, 1.4f);
            FloatingText.Show(na.HeadPos, md.name, Theme.Friendly);
            CombatManager.I.RefreshActor(u);
            Game.I.NotifyPartyChanged();
        }

        public static void Revert(Creature u)
        {
            var b = u.wildshapeBackup; if (b == null) return;
            u.wildshapeBackup = null; u.wildForm = null;
            u.abil = b.abil; u.maxHP = b.maxHP; u.hp = Mathf.Max(1, b.hp); u.baseAC = b.baseAC; u.baseSpeed = b.baseSpeed; u.mdef = b.mdef; u.size = b.size; u.type = b.type;
            if (u.actor == null) return;
            var old = u.actor; var pos = old.transform.position; var rot = old.transform.rotation;
            bool sel = Game.I.Selected == old;
            var parent = old.transform.parent;
            FX.Burst(old.Chest, FxKind.Nature, 1.4f);
            Object.Destroy(old.gameObject);
            var na = ActorFactory.Spawn(u, pos, rot, 512);
            na.transform.SetParent(parent, true);
            na.SetRing(sel, Theme.Gold);
            if (CombatManager.I.Active) na.anim.SetCombat(true);
            CombatLog.Add($"{RulesEngine.Name(u)} returns to their normal form.");
            CombatManager.I.RefreshActor(u);
            PortraitRenderer.I?.Invalidate(u);
            Game.I.NotifyPartyChanged();
        }
    }
}
