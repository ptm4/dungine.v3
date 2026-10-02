using System.Collections.Generic;
using System.Linq;
using Dungine.Combat;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Dungine
{
    public struct PickResult
    {
        public Actor actor;
        public Interactable interact;
        public bool hasGround;
        public Vector3 ground;
    }

    /// <summary>Exploration input: selection, click-to-move with party following, talking, using objects, hotkeys.</summary>
    public class PartyController : MonoBehaviour
    {
        public static PartyController I;
        public PickResult hover;
        Actor hoverActor; Interactable hoverInteract;
        GroundDecal moveMarker; float markerT;
        float senseTimer;
        readonly RaycastHit[] hits = new RaycastHit[32];

        void Awake() { I = this; }

        public PickResult Pick()
        {
            var r = new PickResult();
            var cam = CameraRig.I?.cam; if (!cam) return r;
            if (UIRoot.PointerOverUI) return r;
            var ray = CameraRig.I.MouseRay();
            int n = Physics.RaycastNonAlloc(ray, hits, 250f, Layers.Mask(Layers.Actors, Layers.Interact), QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                var a = h.collider.GetComponentInParent<Actor>();
                if (a != null && h.distance < best) { best = h.distance; r.actor = a; r.interact = null; continue; }
                var it = h.collider.GetComponentInParent<Interactable>();
                if (it != null && it.Available && h.distance < best - 0.3f) { best = h.distance; r.interact = it; r.actor = null; }
            }
            if (Physics.Raycast(ray, out var gh, 400f, Layers.GroundMask | Layers.Mask(Layers.Walls), QueryTriggerInteraction.Ignore))
            {
                r.hasGround = true; r.ground = gh.point;
                if (NavMesh.SamplePosition(gh.point, out var nh, 1.5f, NavMesh.AllAreas)) r.ground = nh.position;
            }
            return r;
        }

        void Update()
        {
            var g = Game.I; if (g == null) return;
            var kb = Keyboard.current; var mouse = Mouse.current;
            if (kb == null || mouse == null) return;
            bool explore = g.mode == GameMode.Explore && !g.busy;
            bool combat = g.mode == GameMode.Combat;

            // global hotkeys
            if (!CameraRig.TypingInUI)
            {
                if (kb.escapeKey.wasPressedThisFrame) UIRoot.I.OnEscape();
                if (explore || combat)
                {
                    if (kb.iKey.wasPressedThisFrame) UIRoot.I.Toggle("inventory");
                    if (kb.cKey.wasPressedThisFrame) UIRoot.I.Toggle("character");
                    if (kb.jKey.wasPressedThisFrame || kb.lKey.wasPressedThisFrame) UIRoot.I.Toggle("journal");
                    if (kb.f1Key.wasPressedThisFrame) g.Select(0);
                    if (kb.f2Key.wasPressedThisFrame) g.Select(1);
                    if (kb.f3Key.wasPressedThisFrame) g.Select(2);
                    for (int k = 0; k < 10; k++)
                    {
                        var key = k == 9 ? kb.digit0Key : kb[Key.Digit1 + k];
                        if (key.wasPressedThisFrame) UIRoot.I.HotbarKey(k);
                    }
                }
                if (explore)
                {
                    if (kb.f5Key.wasPressedThisFrame) SaveSystem.QuickSave();
                    if (kb.f9Key.wasPressedThisFrame) SaveSystem.QuickLoad();
                }
                if (kb.tabKey.wasPressedThisFrame) UIRoot.I.SetShowLabels(true);
                if (kb.tabKey.wasReleasedThisFrame) UIRoot.I.SetShowLabels(false);
            }

            if (!explore) { ClearHover(); return; }
            if (Targeting.I.Active) { ClearHover(); return; }
            if (UIRoot.I.ModalOpen) { ClearHover(); return; }

            hover = Pick();
            UpdateHover();

            if (mouse.leftButton.wasPressedThisFrame && !UIRoot.PointerOverUI) OnLeftClick();
            if (mouse.rightButton.wasPressedThisFrame && !UIRoot.PointerOverUI && hover.actor != null) UIRoot.I.ShowContext(hover.actor);

            senseTimer -= Time.deltaTime;
            if (senseTimer <= 0) { senseTimer = 0.25f; Sense(); }
            if (moveMarker && moveMarker.gameObject.activeSelf) { markerT += Time.deltaTime; moveMarker.SetSize(Mathf.Lerp(1.4f, 0.4f, markerT / 0.6f)); if (markerT > 0.6f) moveMarker.gameObject.SetActive(false); }
        }

        void ClearHover()
        {
            if (hoverActor) hoverActor.SetHighlight(Color.black, 0);
            if (hoverInteract) hoverInteract.SetHover(false);
            hoverActor = null; hoverInteract = null;
            if (!Targeting.I.Active && Game.I.mode != GameMode.Combat) UIRoot.I?.SetHoverLabel(null, Color.white);
        }

        void UpdateHover()
        {
            if (hoverActor != hover.actor) { if (hoverActor) hoverActor.SetHighlight(Color.black, 0); hoverActor = hover.actor; }
            if (hoverInteract != hover.interact) { if (hoverInteract) hoverInteract.SetHover(false); hoverInteract = hover.interact; }
            if (hoverActor)
            {
                var a = hoverActor;
                Color col = a.c.faction == Faction.Hostile && a.c.Active ? Theme.Hostile : a.IsPC ? Theme.Gold : Theme.Neutral;
                a.SetHighlight(col * 0.35f, 1);
                string label = a.Name;
                if (!a.c.Active && a.lootable && !a.IsPC) label += a.looted ? " (searched)" : "\n<size=80%>Search</size>";
                else if (a.talkable) label += "\n<size=80%>Talk</size>";
                else if (a.c.faction == Faction.Hostile) label += $"\n<size=80%>{a.c.hp}/{a.c.MaxHPTotal} HP · Attack</size>";
                UIRoot.I.SetHoverLabel(label, col);
                UIRoot.I.SetCursor(a.talkable ? "talk" : a.c.faction == Faction.Hostile && a.c.Active ? "attack" : "default");
            }
            else if (hoverInteract)
            {
                hoverInteract.SetHover(true);
                UIRoot.I.SetHoverLabel($"{hoverInteract.label}\n<size=80%>{hoverInteract.Verb}</size>", Theme.Parchment);
                UIRoot.I.SetCursor("use");
            }
            else { UIRoot.I.SetHoverLabel(null, Color.white); UIRoot.I.SetCursor("default"); }
        }

        void OnLeftClick()
        {
            var g = Game.I; var sel = g.Selected;
            if (sel == null || sel.c.hp <= 0) { var alive = g.party.FirstOrDefault(c => c.Active); if (alive != null) g.SelectCreature(alive); sel = g.Selected; if (sel == null) return; }
            if (hover.actor != null && !hover.actor.IsPC)
            {
                var a = hover.actor;
                if (a.c.faction == Faction.Hostile && a.c.Active)
                {
                    CombatManager.I.StartCombat(a, sel);
                    return;
                }
                if (!a.c.Active && a.lootable) { Approach(sel, a.transform.position, 1.6f, () => UI.LootWindow.OpenCorpse(a, sel)); return; }
                if (a.talkable) { Approach(sel, a.transform.position, 2.2f, () => Dialogue.DialogueRunner.I.Begin(a.dialogue, a, sel)); return; }
                if (!string.IsNullOrEmpty(a.barkOnClick)) { UIRoot.I.Bark(a, a.barkOnClick); return; }
            }
            else if (hover.actor != null && hover.actor.IsPC)
            {
                var a = hover.actor;
                if (a.c.hp <= 0 && !a.c.dead) { Approach(sel, a.transform.position, 1.4f, () => { RulesEngine.Revive(a.c, 1); Toast.Show($"{sel.Name} helps {a.Name} up.", Theme.Friendly); }); return; }
                g.SelectCreature(a.c);
                return;
            }
            if (hover.interact != null)
            {
                var it = hover.interact;
                Approach(sel, it.UsePoint, it.useRange, () => { if (it && it.Available) { sel.Face(it.transform.position); sel.PlayAnim(AnimAct.Interact); it.Use(sel); } });
                return;
            }
            if (hover.hasGround) MoveParty(hover.ground);
        }

        public void Approach(Actor who, Vector3 p, float range, System.Action then)
        {
            Vector3 d = who.transform.position - p; d.y = 0;
            if (d.magnitude <= range + 0.2f) { who.Stop(); who.Face(p); then(); return; }
            Vector3 dest = p + d.normalized * Mathf.Max(0.3f, range * 0.8f);
            if (NavMesh.SamplePosition(dest, out var nh, 2f, NavMesh.AllAreas)) dest = nh.position;
            else if (NavMesh.SamplePosition(p, out var nh2, 2.5f, NavMesh.AllAreas)) dest = nh2.position;
            MoveParty(dest, who, () => { who.Face(p); then(); });
        }

        public void MoveParty(Vector3 dest, Actor leader = null, System.Action onArrive = null)
        {
            var g = Game.I;
            leader ??= g.Selected;
            if (leader == null) return;
            leader.SetAgentSpeed(4.2f);
            leader.MoveTo(dest, onArrive, 0.12f);
            ShowMarker(dest);
            // followers: slots behind the destination relative to travel direction
            Vector3 dir = dest - leader.transform.position; dir.y = 0;
            if (dir.sqrMagnitude < 0.01f) dir = leader.transform.forward;
            dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            int slot = 0;
            foreach (var a in g.PartyActors)
            {
                if (a == leader || a.c.hp <= 0) continue;
                Vector3 off = -dir * 1.3f + right * (slot == 0 ? -0.9f : 0.9f);
                if (slot >= 2) off = -dir * 2.4f;
                Vector3 p = dest + off;
                if (NavMesh.SamplePosition(p, out var nh, 2f, NavMesh.AllAreas)) p = nh.position; else p = dest;
                a.SetAgentSpeed(4.4f);
                a.MoveTo(p, null, 0.3f);
                slot++;
            }
        }

        void ShowMarker(Vector3 p)
        {
            if (!moveMarker) moveMarker = GroundDecal.Create("MoveMarker", ProcTex.Ring, new Color(1f, .85f, .5f, .9f), 1f, null, 8);
            moveMarker.transform.position = p;
            moveMarker.gameObject.SetActive(true);
            moveMarker.MarkDirty();
            markerT = 0;
        }

        /// <summary>Hostile sight checks and passive perception reveals.</summary>
        void Sense()
        {
            var g = Game.I; if (g.area == null) return;
            var party = g.PartyActors;
            foreach (var n in g.area.npcs)
            {
                if (!n || !n.c.Active || !n.hostileOnSight || n.c.faction != Faction.Hostile) continue;
                foreach (var p in party)
                {
                    if (!p || p.c.hp <= 0) continue;
                    float d = Vector3.Distance(p.transform.position, n.transform.position);
                    float range = n.sightRange;
                    if (p.c.Has(Cond.Invisible)) range *= 0.3f;
                    if (d < range && n.CanSee(p.transform.position))
                    {
                        CombatManager.I.StartCombat(n, null);
                        return;
                    }
                }
            }
            foreach (var it in g.area.interactables)
            {
                if (!it || !it.hidden) continue;
                foreach (var p in party)
                {
                    if (!p) continue;
                    if (Vector3.Distance(p.transform.position, it.transform.position) < 4.5f && p.c.PassivePerception >= it.perceptionDC)
                    {
                        it.Reveal();
                        UIRoot.I.Bark(p, "Wait — there's something here.");
                        break;
                    }
                }
            }
        }

        public void OnActorRebuilt(Actor a, bool selected)
        {
            a.SetRing(selected, Theme.Gold);
        }
    }
}
