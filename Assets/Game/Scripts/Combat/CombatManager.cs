using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Dungine.Combat
{
    /// <summary>Turn-based combat: initiative, turns, movement with opportunity attacks, player input, victory/defeat.</summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager I;
        public bool Active;
        public readonly List<Creature> All = new List<Creature>();
        public List<Creature> order = new List<Creature>();
        public int turnIdx;
        public int Round;
        public Creature Current;
        public bool waitingForPlayer;
        public bool busy;
        bool endTurnRequested;
        LineRenderer pathLine;
        GroundDecal pathEnd;
        readonly HashSet<Creature> fled = new HashSet<Creature>();
        public System.Action OnTurnChanged;
        public string encounterName;
        public System.Action onVictory;

        void Awake()
        {
            I = this;
            gameObject.AddComponent<Targeting>();
        }

        public bool InCombat(Creature c) => Active && All.Contains(c);
        public IEnumerable<Creature> Hostiles(Creature u) => All.Where(c => c.Active && RulesEngine.Hostile(u, c));
        public bool HostileWithin(Creature u, float r) => Hostiles(u).Any(h => RulesEngine.Dist(u, h) <= r && !h.Incapacitated);
        public bool AllyAdjacent(Creature u, Creature t) => All.Any(c => c != u && c != t && c.Active && !c.Incapacitated && RulesEngine.Allied(c, u) && RulesEngine.Dist(c, t) <= 1.6f);

        public void RefreshActor(Creature c) { }

        // ---------------------------------------------------------------- start / end
        /// <summary>Dev: the AI plays the party's turns too (automated combat testing).</summary>
        public static bool DevAutoPCs;

        public void StartCombat(Actor trigger, Actor initiator)
        {
            if (Active) { if (trigger != null && !All.Contains(trigger.c)) Join(trigger.c); return; }
            var g = Game.I;
            if (g.mode != GameMode.Explore && g.mode != GameMode.Dialogue && g.mode != GameMode.Cutscene) return;
            All.Clear(); fled.Clear();
            foreach (var p in g.party) if (p.actor && !p.dead) All.Add(p);
            var enemies = new HashSet<Creature>();
            if (trigger != null)
            {
                if (trigger.c.faction == Faction.Neutral) trigger.c.faction = Faction.Hostile;
                enemies.Add(trigger.c);
                if (!string.IsNullOrEmpty(trigger.encounterId) && g.area.encounters.TryGetValue(trigger.encounterId, out var grp))
                    foreach (var a in grp) if (a && a.c.Active) enemies.Add(a.c);
            }
            // nearby hostiles join
            foreach (var n in g.area.npcs)
            {
                if (!n || !n.c.Active || n.c.faction != Faction.Hostile) continue;
                // others join only if they can see what's happening (walls keep neighbouring rooms out of it)
                if (trigger != null && RulesEngine.FlatDist(n.transform.position, trigger.transform.position) < 16f && n.CanSee(trigger.transform.position)) enemies.Add(n.c);
                foreach (var p in g.PartyActors) if (RulesEngine.FlatDist(n.transform.position, p.transform.position) < 10f && n.CanSee(p.transform.position)) enemies.Add(n.c);
            }
            foreach (var n in g.area.npcs) if (n && n.c.Active && n.c.faction == Faction.Ally) All.Add(n.c);
            All.AddRange(enemies);
            if (!enemies.Any()) return;
            encounterName = trigger?.encounterId;
            Begin();
        }

        public void StartScripted(IEnumerable<Actor> enemies, System.Action victory = null)
        {
            if (Active) return;
            var g = Game.I;
            All.Clear(); fled.Clear();
            foreach (var p in g.party) if (p.actor && !p.dead) All.Add(p);
            foreach (var e in enemies) if (e && e.c.Active) { if (e.c.faction == Faction.Neutral) e.c.faction = Faction.Hostile; All.Add(e.c); }
            foreach (var n in g.area.npcs) if (n && n.c.Active && n.c.faction == Faction.Ally && !All.Contains(n.c)) All.Add(n.c);
            onVictory = victory;
            Begin();
        }

        void Join(Creature c)
        {
            All.Add(c);
            ResetCombatState(c);
            c.initiative = D20.Roll(c.InitiativeBonus, RollMode.Normal).total;
            order.Add(c);
            c.actor?.Stop();
            c.actor?.anim?.SetCombat(true);
            OnTurnChanged?.Invoke();
        }

        void Begin()
        {
            var g = Game.I;
            Active = true;
            g.mode = GameMode.Combat;
            Round = 1;
            foreach (var c in All)
            {
                ResetCombatState(c);
                c.actedThisCombat = false;
                var roll = D20.Roll(c.InitiativeBonus, RollMode.Normal);
                c.initiative = roll.total;
                c.actor?.Stop();
                c.actor?.anim?.SetCombat(true);
                if (c.actor) c.actor.SetRing(true, c.isPC ? Theme.Gold * 0.9f : c.faction == Faction.Hostile ? Theme.Hostile : c.faction == Faction.Ally ? Theme.Friendly : Theme.Neutral);
            }
            order = All.OrderByDescending(c => c.initiative).ThenByDescending(c => c.Score(Ability.DEX)).ThenBy(c => c.isPC ? 0 : 1).ToList();
            turnIdx = 0;
            CombatLog.Add("<color=#e6c67a><b>Combat begins!</b></color> Initiative: " + string.Join(", ", order.Select(c => $"{c.name} {c.initiative}")));
            Audio.AudioSys.I.PlayMusic("combat");
            Audio.Sfx.Play("combat_start");
            UIRoot.I.ShowCombatBanner("Combat");
            OnTurnChanged?.Invoke();
            Game.I.NotifyPartyChanged();
            StartCoroutine(Loop());
        }

        void ResetCombatState(Creature c)
        {
            c.hasAction = c.hasBonus = c.hasReaction = true;
            c.moveLeft = c.Speed;
            c.attacksLeft = 0; c.actionsExtra = 0;
            c.sneakUsed = c.colossusUsed = c.frenzyUsed = c.attackedThisTurn = c.usedMonkAttack = false;
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(0.8f);
            while (Active)
            {
                if (order.Count == 0) break;
                if (turnIdx >= order.Count) { turnIdx = 0; Round++; CombatLog.Add($"<color=#8a8a8a>— Round {Round} —</color>"); }
                Current = order[turnIdx];
                if (Current.dead || fled.Contains(Current) || Current.actor == null || !All.Contains(Current)) { turnIdx++; continue; }
                OnTurnChanged?.Invoke();
                yield return TakeTurn(Current);
                if (!Active) yield break;
                if (CheckEnd()) yield break;
                turnIdx++;
            }
        }

        IEnumerator TakeTurn(Creature c)
        {
            bool skip = StartOfTurn(c);
            Game.I.NotifyPartyChanged();
            if (CheckEnd()) yield break;
            if (c.dead) yield break;
            if (c.actor) CameraRig.I.Focus(c.actor.transform.position);
            if (c.isPC) Game.I.SelectCreature(c);
            if (!skip && c.Active)
            {
                if (c.isPC && c.faction == Faction.Party && !c.Has(Cond.Turned) && !c.Has(Cond.Charmed) && !DevAutoPCs)
                {
                    waitingForPlayer = true; endTurnRequested = false;
                    UIRoot.I.ShowYourTurn(c);
                    Audio.Sfx.Play("your_turn");
                    while (!endTurnRequested && Active && c.Active && !c.dead) { yield return null; }
                    waitingForPlayer = false;
                    ClearPath();
                }
                else
                {
                    CameraRig.I.followT = c.actor ? c.actor.transform : null;
                    yield return new WaitForSeconds(0.35f);
                    yield return AI.TakeTurn(c);
                    CameraRig.I.followT = null;
                }
            }
            else if (skip && c.hp > 0)
            {
                CombatLog.Add($"{RulesEngine.Name(c)} can't act this turn.");
                yield return new WaitForSeconds(0.6f);
            }
            c.actedThisCombat = true;
            EndOfTurn(c);
        }

        /// <summary>Returns true if the creature skips its turn.</summary>
        bool StartOfTurn(Creature c)
        {
            ResetCombatState(c);
            if (c.Has(Cond.Hasted)) c.actionsExtra = 1;
            bool skip = c.conds.Any(x => Conditions.Get(x.id).skipTurn) || c.hp <= 0;
            // death saves
            if (c.isPC && c.hp <= 0 && !c.dead && !c.Has(Cond.Stabilized))
            {
                var r = D20.Roll(0, RollMode.Normal, c.Race != null && c.Race.lucky);
                if (r.natural == 20) { RulesEngine.Revive(c, 1); CombatLog.Add($"{RulesEngine.Name(c)} rolls a natural 20 and gets back up!"); FloatingText.Show(c.actor.HeadPos, "Back up!", Theme.Gold); return false; }
                if (r.natural == 1) c.deathFail += 2; else if (r.natural >= 10) c.deathSucc++; else c.deathFail++;
                CombatLog.Add($"{RulesEngine.Name(c)} death saving throw: {r.natural} — {c.deathSucc} successes, {c.deathFail} failures.");
                FloatingText.Show(c.actor.HeadPos, r.natural >= 10 ? "Death save ✓" : "Death save ✗", r.natural >= 10 ? Theme.Friendly : Theme.Failure);
                if (c.deathFail >= 3) RulesEngine.Kill(c, null, null);
                else if (c.deathSucc >= 3) { c.AddCond(Cond.Stabilized, -1); CombatLog.Add($"{RulesEngine.Name(c)} is stable."); }
                return true;
            }
            // durations tick at start of turn (after checking skip)
            foreach (var x in c.conds.ToList())
            {
                if (x.turns > 0) { x.turns--; if (x.turns == 0) { c.conds.Remove(x); if (x.id == Cond.Hasted) c.AddCond(Cond.Lethargic, 1); if (x.id == Cond.Concentrating) RulesEngine.EndConcentration(c); } }
            }
            // stand up
            if (c.Has(Cond.Prone) && !c.Incapacitated) { c.RemoveCond(Cond.Prone); c.moveLeft *= 0.5f; CombatLog.Add($"{RulesEngine.Name(c)} stands up."); }
            // damage over time
            foreach (var x in c.conds.ToList())
            {
                var cd = Conditions.Get(x.id);
                if (!string.IsNullOrEmpty(cd.dot)) { int d = DiceExpr.Parse(cd.dot).Roll(); CombatLog.Add($"{RulesEngine.Name(c)} suffers from {x.id.Nice()}."); RulesEngine.DealDamage(x.source, c, d, cd.dotType, false, null); FX.Burst(c.actor.Chest, cd.dotType == DamageType.Fire ? FxKind.Fire : FxKind.Blood, 0.6f); }
            }
            // spirit guardians
            foreach (var g in All.Where(o => o.Active && o.Has(Cond.SpiritGuardians) && RulesEngine.Hostile(o, c)))
            {
                if (RulesEngine.Dist(g, c) <= 4.5f)
                {
                    var sg = ActionLibrary.Get("spirit_guardians");
                    var sr = RulesEngine.RollSave(c, Ability.WIS, g.SpellDC, g, sg);
                    int dmg = DiceExpr.Parse("3d8").Roll();
                    FX.Burst(c.actor.Chest, FxKind.Holy, 0.8f);
                    RulesEngine.DealDamage(g, c, sr.success ? dmg / 2 : dmg, DamageType.Radiant, false, sg);
                }
            }
            // regeneration
            if (c.noRegenThisRound) { c.noRegenThisRound = false; if (c.mdef != null && c.mdef.regeneration > 0 && c.hp > 0) CombatLog.Add($"{RulesEngine.Name(c)} cannot regenerate — the radiant wound won't close."); }
            else if (c.mdef != null && c.mdef.regeneration > 0 && c.hp > 0 && c.hp < c.MaxHPTotal) { int h = Mathf.Min(c.mdef.regeneration, c.MaxHPTotal - c.hp); c.hp += h; FloatingText.Show(c.actor.HeadPos, $"+{h}", Theme.Friendly); CombatLog.Add($"{RulesEngine.Name(c)} regenerates {h} hit points."); }
            if (c.spiritualWeaponTurns > 0) { c.spiritualWeaponTurns--; if (c.spiritualWeaponTurns == 0) c.extraActions.Remove("spiritual_strike"); }
            if (c.dead || c.hp <= 0) return true;
            return c.conds.Any(x => Conditions.Get(x.id).skipTurn);
        }

        void EndOfTurn(Creature c)
        {
            foreach (var x in c.conds.ToList())
            {
                if (!x.saveEachTurn || x.dc <= 0) continue;
                var sr = RulesEngine.RollSave(c, x.saveAb, x.dc, x.source, null, x.id);
                if (sr.success)
                {
                    c.conds.Remove(x);
                    if (x.id == Cond.Laughing) c.RemoveCond(Cond.Prone);
                    CombatLog.Add($"{RulesEngine.Name(c)} shakes off {x.id.Nice()}.");
                    if (c.actor) FloatingText.Show(c.actor.HeadPos, x.id.Nice() + " ended", Theme.TextDim, 0.8f);
                }
            }
            c.pendingMeta = null;
            Game.I.NotifyPartyChanged();
        }

        public void EndTurn()
        {
            if (!waitingForPlayer || busy) return;
            Targeting.I.Cancel();
            endTurnRequested = true;
            Audio.Sfx.Play("end_turn");
        }

        public bool CheckEnd()
        {
            if (!Active) return true;
            var g = Game.I;
            bool partyAlive = g.party.Any(c => c.Active);
            bool enemies = All.Any(c => c.Active && c.faction == Faction.Hostile && !fled.Contains(c));
            if (!partyAlive) { End(false); return true; }
            if (!enemies) { End(true); return true; }
            return false;
        }

        void End(bool victory)
        {
            Active = false;
            waitingForPlayer = false;
            Targeting.I.Cancel();
            ClearPath();
            StopAllCoroutines();
            var g = Game.I;
            foreach (var c in All)
            {
                if (c.actor) { c.actor.anim?.SetCombat(false); c.actor.SetRing(c.isPC && c == g.SelectedC, Theme.Gold); if (!c.isPC) c.actor.SetRing(false, Color.clear); }
                c.conds.RemoveAll(x => x.id == Cond.Disengaged || x.id == Cond.Dodging || x.id == Cond.Reckless || x.id == Cond.ShieldSpell);
            }
            if (victory)
            {
                int xp = All.Where(c => !c.isPC && c.dead && c.faction == Faction.Hostile).Sum(c => c.xpValue);
                foreach (var p in g.party) if (p.hp <= 0 && !p.dead) { RulesEngine.Revive(p, 1); CombatLog.Add($"{RulesEngine.Name(p)} regains consciousness."); }
                foreach (var p in g.party) if (p.wildshapeBackup != null) Wildshape.Revert(p);
                g.mode = GameMode.Explore;
                UIRoot.I.ShowCombatBanner("Victory");
                Audio.AudioSys.I.PlayMusic(g.area?.def.Music ?? "explore");
                Audio.Sfx.Play("victory");
                if (xp > 0) g.GiveXP(xp, "combat");
                var cb = onVictory; onVictory = null;
                cb?.Invoke();
                SaveSystem.Autosave();
            }
            else g.CheckGameOver();
            Current = null;
            All.Clear(); order.Clear();
            OnTurnChanged?.Invoke();
            g.NotifyPartyChanged();
        }

        public void OnDeath(Creature c)
        {
            if (!Active) return;
            if (c == Current && !c.isPC) { }
            OnTurnChanged?.Invoke();
        }

        public void Flee(Creature c) { fled.Add(c); }

        // ---------------------------------------------------------------- player input during their turn
        void Update()
        {
            if (!Active || !waitingForPlayer || busy || Current == null || Current.actor == null) { if (!busy) ClearPath(); return; }
            if (Targeting.I.Active || UIRoot.I.ModalOpen) { ClearPath(); return; }
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (kb.spaceKey.wasPressedThisFrame && !CameraRig.TypingInUI) { EndTurn(); return; }
            var pick = PartyController.I.Pick();
            var hoverA = pick.actor;
            foreach (var c in All) if (c.actor && c.actor != hoverA) c.actor.SetHighlight(Color.black, 0);
            if (hoverA != null && hoverA.c != Current)
            {
                ClearPath();
                var t = hoverA.c;
                if (RulesEngine.Hostile(Current, t) && t.Active)
                {
                    var atk = DefaultAttack(Current, t);
                    hoverA.SetHighlight(Theme.Hostile * 0.4f, 1);
                    if (atk != null)
                    {
                        bool can = RulesEngine.CanUse(Current, atk, true, out var why);
                        var info = RulesEngine.AttackPreview(Current, t, atk);
                        float d = RulesEngine.Dist(Current, t), range = RulesEngine.Range(Current, atk);
                        string lbl = $"{t.name}  {t.hp}/{t.MaxHPTotal} HP  AC {t.AC}\n{atk.name}: <b>{Mathf.RoundToInt(info.chance * 100)}%</b>";
                        if (!can) lbl += $"\n<color=#e89a8a>{why}</color>";
                        else if (d > range + 0.3f) { float need = d - range; lbl += need > Current.moveLeft ? $"\n<color=#e89a8a>Too far ({need:F1}m, {Current.moveLeft:F1}m movement left)</color>" : $"\n<color=#e8c070>Moves {need:F1}m first</color>"; }
                        foreach (var c in t.conds.Take(4)) lbl += $"\n<color={Theme.Hex(Conditions.Get(c.id).color)}>{c.id.Nice()}</color>";
                        UIRoot.I.SetHoverLabel(lbl, Theme.Hostile);
                        if (mouse.leftButton.wasPressedThisFrame && !UIRoot.PointerOverUI && can)
                        {
                            var ctx = new ActionContext { a = atk, user = Current };
                            ctx.targets.Add(t);
                            StartCoroutine(Perform(ctx));
                        }
                    }
                    return;
                }
                if (t.isPC && t.hp <= 0 && !t.dead)
                {
                    var help = ActionLibrary.Get("help");
                    UIRoot.I.SetHoverLabel($"{t.name} is down\nHelp them up (Action)", Theme.Friendly);
                    if (mouse.leftButton.wasPressedThisFrame && !UIRoot.PointerOverUI && RulesEngine.CanUse(Current, help, true, out _))
                    {
                        var ctx = new ActionContext { a = help, user = Current }; ctx.targets.Add(t);
                        StartCoroutine(Perform(ctx));
                    }
                    return;
                }
                UIRoot.I.SetHoverLabel($"{t.name}  {Mathf.Max(0, t.hp)}/{t.MaxHPTotal} HP", Theme.Friendly);
                return;
            }
            UIRoot.I.SetHoverLabel(null, Color.white);
            if (pick.hasGround && !UIRoot.PointerOverUI)
            {
                var path = new NavMeshPath();
                var from = Current.actor.transform.position;
                if (NavMesh.CalculatePath(from, pick.ground, NavMesh.AllAreas, path) && path.corners.Length > 1)
                {
                    float len = PathLength(path.corners);
                    ShowPath(path.corners, Current.moveLeft);
                    UIRoot.I.SetCursorText($"{Mathf.Min(len, Current.moveLeft):F1}m" + (len > Current.moveLeft + 0.05f ? $" <color=#e89a8a>({len:F1}m)</color>" : ""));
                    if (mouse.leftButton.wasPressedThisFrame && Current.moveLeft > 0.2f && !Current.conds.Any(c => Conditions.Get(c.id).noMove))
                        StartCoroutine(MoveRoutine(Current, path.corners, Current.moveLeft));
                }
                else ClearPath();
            }
        }

        public static ActionDef DefaultAttack(Creature u, Creature t)
        {
            var acts = u.AvailableActions();
            float d = RulesEngine.Dist(u, t);
            var melee = acts.FirstOrDefault(a => a.id == "attack" || a.id == "unarmed_strike");
            var ranged = acts.FirstOrDefault(a => a.id == "ranged_attack");
            if (melee != null && melee.id == "attack" && u.MainWeapon != null && u.MainWeapon.IsRanged) return melee.Clone().WithRanged();
            if (ranged != null && (d > 2.5f || melee == null) && RulesEngine.CanUse(u, ranged, true, out _)) return ranged;
            return melee ?? ranged ?? acts.FirstOrDefault(a => a.IsAttack);
        }

        public void UseAction(ActionDef a, ItemStack item = null)
        {
            if (!Active || !waitingForPlayer || busy) return;
            if (!RulesEngine.CanUse(Current, a, true, out var why)) { Toast.Show(why, Theme.Failure); Audio.Sfx.Play("error"); return; }
            Targeting.I.Begin(Current, a, true, ctx => StartCoroutine(Perform(ctx)), null, item);
        }

        public IEnumerator Perform(ActionContext ctx)
        {
            busy = true;
            ClearPath();
            var u = ctx.user; var a = ctx.a;
            // close the distance for single-target actions
            if (ctx.targets.Count > 0 && ctx.targets[0] != u && (a.target == TargetKind.Creature || a.target == TargetKind.Weapon) && ctx.targets[0].actor != null)
            {
                float range = RulesEngine.Range(u, a);
                float d = RulesEngine.Dist(u, ctx.targets[0]);
                if (d > range + 0.3f)
                {
                    var path = new NavMeshPath();
                    Vector3 tp = ctx.targets[0].actor.transform.position;
                    Vector3 dir = (u.actor.transform.position - tp); dir.y = 0; dir.Normalize();
                    Vector3 dest = tp + dir * Mathf.Max(0.5f, range * 0.75f + ctx.targets[0].actor.radius * 0.5f);
                    if (NavMesh.SamplePosition(dest, out var nh, 1.5f, NavMesh.AllAreas)) dest = nh.position;
                    if (NavMesh.CalculatePath(u.actor.transform.position, dest, NavMesh.AllAreas, path) && PathLength(path.corners) <= u.moveLeft + 0.1f)
                        yield return MoveRoutine(u, path.corners, u.moveLeft, true);
                    if (u.dead || u.hp <= 0) { busy = false; yield break; }
                    d = RulesEngine.Dist(u, ctx.targets[0]);
                    if (d > range + 0.4f) { Toast.Show("Out of range.", Theme.Failure); busy = false; yield break; }
                }
            }
            yield return RulesEngine.Execute(ctx, true);
            if (!u.hasAction && u.actionsExtra > 0 && u.Has(Cond.Hasted)) { u.hasAction = true; u.actionsExtra--; }
            Game.I.NotifyPartyChanged();
            busy = false;
            CheckEnd();
        }

        // ---------------------------------------------------------------- movement with opportunity attacks
        public static float PathLength(Vector3[] c) { float l = 0; for (int i = 1; i < c.Length; i++) l += Vector3.Distance(c[i - 1], c[i]); return l; }

        public IEnumerator MoveRoutine(Creature c, Vector3[] corners, float budget, bool nested = false)
        {
            if (!nested) busy = true;
            ClearPath();
            var a = c.actor;
            if (a.agent && a.agent.enabled) { a.agent.isStopped = true; a.agent.ResetPath(); a.agent.updatePosition = false; }
            a.manualMove = true;
            float speed = 4.2f;
            float moved = 0;
            var reach = new Dictionary<Creature, bool>();
            foreach (var h in Hostiles(c)) reach[h] = RulesEngine.Dist(c, h) <= ReachOf(h);
            for (int i = 1; i < corners.Length && moved < budget - 0.01f; i++)
            {
                Vector3 target = corners[i];
                while (true)
                {
                    Vector3 pos = a.transform.position;
                    Vector3 to = target - pos; to.y = 0;
                    float dist = to.magnitude;
                    if (dist < 0.02f) break;
                    float step = Mathf.Min(speed * Time.deltaTime, dist, budget - moved);
                    if (step <= 0.0001f) break;
                    Vector3 np = pos + to / dist * step;
                    if (NavMesh.SamplePosition(np, out var nh, 0.6f, NavMesh.AllAreas)) np.y = nh.position.y;
                    a.transform.position = np;
                    a.transform.rotation = Quaternion.RotateTowards(a.transform.rotation, Quaternion.LookRotation(to), 720 * Time.deltaTime);
                    if (a.agent && a.agent.enabled) a.agent.nextPosition = np;
                    moved += step;
                    c.moveLeft = Mathf.Max(0, c.moveLeft - step);
                    // opportunity attacks
                    if (!c.Has(Cond.Disengaged))
                    {
                        foreach (var h in Hostiles(c).ToList())
                        {
                            bool now = RulesEngine.Dist(c, h) <= ReachOf(h);
                            if (reach.TryGetValue(h, out var was) && was && !now && h.hasReaction && !h.Incapacitated && h.actor)
                            {
                                reach[h] = false;
                                yield return OpportunityAttack(h, c);
                                if (c.hp <= 0 || c.dead) goto done;
                            }
                            reach[h] = now;
                        }
                    }
                    yield return null;
                }
            }
            done:
            a.manualMove = false;
            if (a && a.agent && a.agent.enabled) { a.agent.Warp(a.transform.position); a.agent.updatePosition = true; }
            if (!nested) busy = false;
            Game.I.NotifyPartyChanged();
        }

        static float ReachOf(Creature h)
        {
            var atk = h.AvailableActions().FirstOrDefault(x => x.attack == AttackKind.MeleeWeapon);
            return atk != null ? RulesEngine.Range(h, atk) : 1.5f;
        }

        IEnumerator OpportunityAttack(Creature h, Creature target)
        {
            var atk = h.AvailableActions().FirstOrDefault(x => x.attack == AttackKind.MeleeWeapon && (x.cost == ActionCost.Action));
            if (atk == null) yield break;
            h.hasReaction = false;
            CombatLog.Add($"{RulesEngine.Name(h)} makes an <color=#e8c070>opportunity attack</color>!");
            FloatingText.Show(h.actor.HeadPos + Vector3.up * 0.4f, "Opportunity Attack", Theme.BonusOrange, 0.85f);
            h.actor.Face(target.actor.transform.position, true);
            bool imp = false; h.actor.PlayAnim(atk.anim, () => imp = true);
            float t = 0; while (!imp && t < 2f) { t += Time.deltaTime; yield return null; }
            var ctx = new ActionContext { a = atk, user = h };
            RulesEngine.ResolveAttack(ctx, target);
            yield return new WaitForSeconds(0.25f);
        }

        // ---------------------------------------------------------------- path preview
        void ShowPath(Vector3[] corners, float budget)
        {
            if (!pathLine)
            {
                var go = new GameObject("PathLine");
                pathLine = go.AddComponent<LineRenderer>();
                pathLine.widthMultiplier = 0.08f;
                pathLine.material = MatLib.FX(ProcTex.White, Color.white, true, 0f, 1.2f);
                pathLine.numCornerVertices = 3;
                pathLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pathEnd = GroundDecal.Create("PathEnd", ProcTex.Ring, Color.white, 0.8f, null, 8);
            }
            pathLine.gameObject.SetActive(true); pathEnd.gameObject.SetActive(true);
            var pts = new List<Vector3>();
            float acc = 0; bool over = false; Vector3 last = corners[0];
            pts.Add(corners[0] + Vector3.up * 0.08f);
            for (int i = 1; i < corners.Length; i++)
            {
                float seg = Vector3.Distance(corners[i - 1], corners[i]);
                if (!over && acc + seg > budget) { over = true; }
                acc += seg;
                pts.Add(corners[i] + Vector3.up * 0.08f);
                last = corners[i];
            }
            pathLine.positionCount = pts.Count;
            pathLine.SetPositions(pts.ToArray());
            Color col = acc <= budget + 0.05f ? new Color(.5f, 1f, .6f, .9f) : new Color(1f, .45f, .35f, .9f);
            pathLine.startColor = pathLine.endColor = col;
            pathLine.material.SetColor("_BaseColor", col);
            pathEnd.transform.position = last; pathEnd.SetColor(col); pathEnd.MarkDirty();
        }

        void ClearPath()
        {
            if (pathLine) pathLine.gameObject.SetActive(false);
            if (pathEnd) pathEnd.gameObject.SetActive(false);
            UIRoot.I?.SetCursorText(null);
        }
    }

    public static class ActionDefExt
    {
        public static ActionDef WithRanged(this ActionDef a)
        {
            a.attack = AttackKind.RangedWeapon; a.anim = AnimAct.Bow; a.proj = ProjKind.Arrow; a.range = 18;
            return a;
        }
    }
}
