using System;
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
    /// <summary>Interactive target selection with previews for every TargetKind.</summary>
    public class Targeting : MonoBehaviour
    {
        public static Targeting I;
        public bool Active => action != null;
        public ActionDef action;
        public Creature user;
        public ItemStack item;
        public int slotOverride;
        readonly List<Creature> picked = new List<Creature>();
        Action<ActionContext> onConfirm; Action onCancel;
        GroundDecal rangeRing, aoe, shape;
        readonly HashSet<Actor> highlighted = new HashSet<Actor>();
        bool inCombat;
        float armedAt;

        void Awake() { I = this; }

        public int NeededTargets
        {
            get
            {
                if (action == null) return 1;
                int n = action.targets;
                int slot = slotOverride > 0 ? slotOverride : RulesEngine.SlotLevelFor(user, action);
                if (action.targetsPerUpcast > 0 && slot > action.spellLevel && action.spellLevel > 0) n += action.targetsPerUpcast * (slot - action.spellLevel);
                if (action.special == "eldritch_blast" && user.level >= 5) n = 2;
                if (user.pendingMeta == "twin" && action.targets == 1 && action.target == TargetKind.Creature && action.IsSpell) n = 2;
                return n;
            }
        }

        public void Begin(Creature u, ActionDef a, bool combat, Action<ActionContext> confirm, Action cancel = null, ItemStack it = null)
        {
            Cancel(false);
            user = u; action = a; onConfirm = confirm; onCancel = cancel; item = it; inCombat = combat;
            picked.Clear(); slotOverride = 0; armedAt = Time.unscaledTime;
            if (a.target == TargetKind.Self || (a.target == TargetKind.Aura && !a.IsAoE))
            {
                Confirm(new List<Creature> { u }, u.actor.transform.position);
                return;
            }
            if (a.target == TargetKind.Aura)
            {
                var victims = CollectArea(a, u, u.actor.transform.position);
                Confirm(victims, u.actor.transform.position);
                return;
            }
            float range = RulesEngine.Range(u, a);
            if (range > 1.6f && a.target != TargetKind.Cone && a.target != TargetKind.Line)
            {
                rangeRing = GroundDecal.Create("RangeRing", ProcTex.Ring, new Color(.9f, .8f, .5f, .35f), (range + u.actor.radius) * 2f, null, 24);
                rangeRing.transform.position = u.actor.transform.position;
            }
            if (a.target == TargetKind.Point && a.radius > 0) aoe = GroundDecal.Create("AoE", Templates.Disc, WithA(a.color, 0.55f), a.radius * 2f, null, 16);
            if (a.target == TargetKind.Cone) shape = GroundDecal.Create("Cone", Templates.Cone(a.coneAngle), WithA(a.color, 0.6f), a.radius * 2f, null, 18);
            if (a.target == TargetKind.Line) shape = GroundDecal.Create("Line", Templates.Line(a.lineWidth / Mathf.Max(1, a.radius)), WithA(a.color, 0.6f), a.radius * 2f, null, 24);
            UIRoot.I.SetTargetingHint($"{a.name}: " + HintFor(a) + "   <color=#9a9a9a>[Right-click to cancel]</color>");
        }

        static Color WithA(Color c, float a) => new Color(c.r, c.g, c.b, a);

        string HintFor(ActionDef a)
        {
            switch (a.target)
            {
                case TargetKind.Point: return a.special == "teleport" ? "choose a destination" : "choose where to aim";
                case TargetKind.Cone: case TargetKind.Line: return "aim the area";
                default:
                    int n = NeededTargets;
                    string who = a.who == Who.Ally ? "an ally" : a.who == Who.DownedAlly ? "a downed ally" : a.who == Who.DeadAlly ? "a fallen companion" : a.who == Who.Any ? "a creature" : "an enemy";
                    return n > 1 ? $"choose {n} targets ({picked.Count}/{n}) — Enter to finish early" : "choose " + who;
            }
        }

        public void Cancel(bool notify = true)
        {
            if (rangeRing) Destroy(rangeRing.gameObject);
            if (aoe) Destroy(aoe.gameObject);
            if (shape) Destroy(shape.gameObject);
            foreach (var h in highlighted) if (h) h.SetHighlight(Color.black, 0);
            highlighted.Clear();
            bool was = action != null;
            action = null; user = null; picked.Clear();
            UIRoot.I?.SetTargetingHint(null);
            UIRoot.I?.SetHoverLabel(null, Color.white);
            if (was && notify) onCancel?.Invoke();
        }

        void Confirm(List<Creature> targets, Vector3 point)
        {
            var ctx = new ActionContext { a = action, user = user, point = point, item = item, free = !inCombat };
            ctx.targets.AddRange(targets);
            if (slotOverride > 0) ctx.slot = slotOverride;
            var cb = onConfirm;
            Cancel(false);
            cb?.Invoke(ctx);
        }

        public bool ValidTarget(Creature t)
        {
            if (t == null || action == null) return false;
            switch (action.who)
            {
                case Who.Enemy: if (t.dead || t.hp <= 0 && !t.isPC) return false; if (!RulesEngine.Hostile(user, t) && !(t.faction == Faction.Neutral && t != user)) return false; break;
                case Who.Ally: if (t.dead || RulesEngine.Hostile(user, t)) return false; if (t.hp <= 0 && !action.IsHeal && action.special != "help") return false; break;
                case Who.DownedAlly: if (t.dead || t.hp > 0 && !t.Has(Cond.Asleep) && !t.Has(Cond.Prone)) return false; if (RulesEngine.Hostile(user, t)) return false; break;
                case Who.DeadAlly: if (!t.isPC || !(t.dead || t.hp <= 0)) return false; break;
                case Who.Any: if (t.dead) return false; break;
            }
            if (action.distinctTargets && picked.Contains(t)) return false;
            if (action.onlyVs != null && !action.onlyVs.Contains(t.type)) return false;
            return true;
        }

        void Update()
        {
            if (!Active) return;
            if (user == null || user.actor == null) { Cancel(); return; }
            var mouse = Mouse.current; var kb = Keyboard.current;
            if (mouse.rightButton.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame) { Cancel(); return; }
            if (rangeRing) rangeRing.transform.position = user.actor.transform.position;
            var pick = PartyController.I.Pick();
            bool click = mouse.leftButton.wasPressedThisFrame && !UIRoot.PointerOverUI && Time.unscaledTime - armedAt > 0.1f;
            float range = RulesEngine.Range(user, action);
            foreach (var h in highlighted) if (h) h.SetHighlight(Color.black, 0);
            highlighted.Clear();

            switch (action.target)
            {
                case TargetKind.Creature:
                case TargetKind.Weapon:
                    {
                        var t = pick.actor?.c;
                        if (t != null && ValidTarget(t))
                        {
                            float d = RulesEngine.Dist(user, t);
                            bool inRange = d <= range + 0.3f;
                            string label = pick.actor.Name;
                            Color col = RulesEngine.Hostile(user, t) ? Theme.Hostile : Theme.Friendly;
                            pick.actor.SetHighlight(col * 0.4f, 1); highlighted.Add(pick.actor);
                            if (action.IsAttack && !action.autoHit)
                            {
                                var info = RulesEngine.AttackPreview(user, t, action);
                                label += $"\n<size=120%><b>{Mathf.RoundToInt(info.chance * 100)}%</b></size> to hit";
                                if (info.adv.Count > 0) label += $"\n<color=#8fd88f>Advantage: {string.Join(", ", info.adv.Distinct())}</color>";
                                if (info.dis.Count > 0) label += $"\n<color=#e89a8a>Disadvantage: {string.Join(", ", info.dis.Distinct())}</color>";
                            }
                            else if (action.hasSave) label += $"\n{action.saveAb} save DC {RulesEngine.SaveDC(user, action)}";
                            if (!inRange) label += $"\n<color=#e8c070>Out of range ({d:F1}m / {range:F1}m) — will move closer</color>";
                            if (NeededTargets > 1) label += $"\nTargets {picked.Count}/{NeededTargets}";
                            UIRoot.I.SetHoverLabel(label, col);
                            if (click)
                            {
                                picked.Add(t);
                                Audio.Sfx.Play("select");
                                if (picked.Count >= NeededTargets) { Confirm(new List<Creature>(picked), t.actor.transform.position); return; }
                                UIRoot.I.SetTargetingHint($"{action.name}: " + HintFor(action));
                            }
                        }
                        else UIRoot.I.SetHoverLabel(t != null ? "<color=#9a9a9a>Invalid target</color>" : null, Theme.TextDim);
                        if (kb.enterKey.wasPressedThisFrame && picked.Count > 0) { Confirm(new List<Creature>(picked), picked[0].actor.transform.position); return; }
                        break;
                    }
                case TargetKind.Point:
                    {
                        if (!pick.hasGround) break;
                        Vector3 p = pick.ground;
                        Vector3 from = user.actor.transform.position;
                        Vector3 dv = p - from; dv.y = 0;
                        if (dv.magnitude > range) { p = from + dv.normalized * range; p.y = pick.ground.y; if (NavMesh.SamplePosition(p, out var nh, 3f, NavMesh.AllAreas)) p = nh.position; }
                        if (aoe) { aoe.transform.position = p; }
                        var victims = action.radius > 0 ? CollectArea(action, user, p) : new List<Creature>();
                        foreach (var v in victims) { var col = RulesEngine.Hostile(user, v) ? Theme.Hostile : Theme.Friendly; v.actor.SetHighlight(col * 0.45f, 1); highlighted.Add(v.actor); }
                        string lbl = action.radius > 0 ? $"{victims.Count} creature{(victims.Count == 1 ? "" : "s")} in the area" : "";
                        if (victims.Any(v => !RulesEngine.Hostile(user, v)) && !(user.IsSub("evocation") && action.IsSpell)) lbl += "\n<color=#e8c070>Allies will be hit!</color>";
                        UIRoot.I.SetHoverLabel(lbl, Theme.Parchment);
                        if (click) { Confirm(victims, p); return; }
                        break;
                    }
                case TargetKind.Cone:
                case TargetKind.Line:
                    {
                        if (!pick.hasGround) break;
                        Vector3 from = user.actor.transform.position;
                        Vector3 dir = pick.ground - from; dir.y = 0;
                        if (dir.sqrMagnitude < 0.01f) dir = user.actor.transform.forward;
                        dir.Normalize();
                        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                        if (shape) { shape.transform.position = from; shape.yaw = yaw; }
                        var victims = CollectShape(action, user, from, dir);
                        foreach (var v in victims) { var col = RulesEngine.Hostile(user, v) ? Theme.Hostile : Theme.Friendly; v.actor.SetHighlight(col * 0.45f, 1); highlighted.Add(v.actor); }
                        UIRoot.I.SetHoverLabel($"{victims.Count} creature{(victims.Count == 1 ? "" : "s")} affected", Theme.Parchment);
                        if (click) { user.actor.Face(from + dir, true); Confirm(victims, from + dir * action.radius); return; }
                        break;
                    }
            }
        }

        // ---------------------------------------------------------------- area queries
        public static IEnumerable<Creature> Candidates()
        {
            var g = Game.I;
            if (CombatManager.I.Active) return CombatManager.I.All.Where(c => c.actor);
            var list = new List<Creature>(g.party.Where(c => c.actor));
            if (g.area != null) list.AddRange(g.area.npcs.Where(a => a).Select(a => a.c));
            return list;
        }

        public static List<Creature> CollectArea(ActionDef a, Creature u, Vector3 p)
        {
            float r = a.radius;
            var res = new List<Creature>();
            foreach (var c in Candidates())
            {
                if (c.dead || c.actor == null) continue;
                if (a.target == TargetKind.Aura && c == u && a.who == Who.Enemy) continue;
                if (a.who == Who.Ally && RulesEngine.Hostile(u, c)) continue;
                if (a.who == Who.Enemy && a.target == TargetKind.Aura && !RulesEngine.Hostile(u, c)) continue;
                float d = RulesEngine.FlatDist(c.actor.transform.position, p) - c.actor.radius * 0.5f;
                if (d <= r) res.Add(c);
            }
            return res;
        }

        public static List<Creature> CollectShape(ActionDef a, Creature u, Vector3 from, Vector3 dir)
        {
            var res = new List<Creature>();
            foreach (var c in Candidates())
            {
                if (c == u || c.dead || c.actor == null) continue;
                Vector3 d = c.actor.transform.position - from; d.y = 0;
                float dist = d.magnitude;
                if (a.target == TargetKind.Cone)
                {
                    if (dist > a.radius + c.actor.radius * 0.5f || dist < 0.05f) continue;
                    float ang = Vector3.Angle(dir, d);
                    if (ang <= a.coneAngle * 0.5f + Mathf.Rad2Deg * Mathf.Atan2(c.actor.radius, dist)) res.Add(c);
                }
                else
                {
                    float along = Vector3.Dot(d, dir);
                    if (along < 0 || along > a.radius) continue;
                    float side = Vector3.Cross(dir, d).magnitude;
                    if (side <= a.lineWidth * 0.5f + c.actor.radius * 0.5f) res.Add(c);
                }
            }
            if (a.usesWeapon) res = res.Where(c => RulesEngine.Hostile(u, c)).ToList();
            return res;
        }
    }

    /// <summary>Procedural textures for AoE templates.</summary>
    public static class Templates
    {
        static Texture2D _disc; static readonly Dictionary<int, Texture2D> cones = new Dictionary<int, Texture2D>(); static readonly Dictionary<int, Texture2D> lines = new Dictionary<int, Texture2D>();

        public static Texture2D Disc
        {
            get
            {
                if (_disc) return _disc;
                _disc = ProcTex.Radial(256, d =>
                {
                    float edge = Mathf.Exp(-Mathf.Pow((d - 0.965f) / 0.022f, 2));
                    float fill = d < 0.98f ? 0.16f + 0.1f * MathX.Smoothstep(0.5f, 0.98f, d) : 0;
                    float rings = d < 0.95f ? Mathf.Max(0, Mathf.Sin(d * 40f)) * 0.04f : 0;
                    return Mathf.Clamp01(edge + fill + rings);
                });
                _disc.name = "AoEDisc";
                return _disc;
            }
        }

        public static Texture2D Cone(float angle)
        {
            int key = Mathf.RoundToInt(angle);
            if (cones.TryGetValue(key, out var t) && t) return t;
            int s = 256; t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Cone" + key };
            var px = new Color32[s * s];
            float half = angle * 0.5f * Mathf.Deg2Rad;
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Atan2(u, v);
                    float inside = (r <= 1f && Mathf.Abs(a) <= half && v > -0.01f) ? 1 : 0;
                    float edgeA = Mathf.Exp(-Mathf.Pow((Mathf.Abs(a) - half) * r * 60f, 2)) * (r <= 1 ? 1 : 0) * (v > 0 ? 1 : 0);
                    float edgeR = Mathf.Exp(-Mathf.Pow((r - 0.985f) / 0.018f, 2)) * (Mathf.Abs(a) <= half ? 1 : 0);
                    float alpha = Mathf.Clamp01(inside * (0.14f + 0.12f * r) + edgeA * 0.9f + edgeR);
                    px[y * s + x] = new Color(1, 1, 1, alpha);
                }
            t.SetPixels32(px); t.Apply(true, true);
            cones[key] = t;
            return t;
        }

        public static Texture2D Line(float widthFrac)
        {
            int key = Mathf.RoundToInt(widthFrac * 1000);
            if (lines.TryGetValue(key, out var t) && t) return t;
            int s = 256; t = new Texture2D(s, s, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "Line" + key };
            var px = new Color32[s * s];
            float hw = Mathf.Max(0.02f, widthFrac * 0.5f);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                {
                    float u = (x + .5f) / s * 2 - 1, v = (y + .5f) / s * 2 - 1;
                    bool inside = Mathf.Abs(u) <= hw && v >= 0;
                    float edge = v >= 0 ? Mathf.Exp(-Mathf.Pow((Mathf.Abs(u) - hw) / 0.01f, 2)) : 0;
                    float alpha = Mathf.Clamp01((inside ? 0.25f : 0) + edge);
                    px[y * s + x] = new Color(1, 1, 1, alpha);
                }
            t.SetPixels32(px); t.Apply(true, true);
            lines[key] = t;
            return t;
        }
    }
}
