using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Dungine.Dialogue
{
    public class DOption
    {
        public string text, next, tag, id;
        public Func<bool> cond;
        public Action effect;
        public Skill? skill; public Ability? ability; public int dc; public string success, failure; public bool adv;
        public bool once, end;
        public BackgroundId? inspires;
    }

    public class DNode
    {
        public string id, speaker, text, next;
        public string speakerActor;       // area actor id to frame (null = the NPC being spoken to, "pc" = the party speaker)
        public Func<string> dynText;
        public readonly List<DOption> options = new List<DOption>();
        public Action onEnter;
        public bool end;
        public AnimAct gesture;
    }

    public class DialogueDef
    {
        public string id;
        public readonly Dictionary<string, DNode> nodes = new Dictionary<string, DNode>();
        public Func<string> start = () => "start";
    }

    /// <summary>Fluent builder for dialogue content.</summary>
    public class DB
    {
        public readonly DialogueDef def;
        DNode cur;
        DOption lastOpt;
        public DB(string id) { def = new DialogueDef { id = id }; Dialogues.All[id] = def; }
        public DB Start(Func<string> f) { def.start = f; return this; }
        public DB Node(string id, string speaker, string text) { cur = new DNode { id = id, speaker = speaker, text = text }; def.nodes[id] = cur; lastOpt = null; return this; }
        public DB Dyn(Func<string> t) { cur.dynText = t; return this; }
        public DB Actor(string actorId) { cur.speakerActor = actorId; return this; }
        public DB Enter(Action a) { cur.onEnter += a; return this; }
        public DB Then(string next) { cur.next = next; return this; }
        public DB End() { cur.end = true; return this; }
        public DB Gesture(AnimAct a) { cur.gesture = a; return this; }
        public DB Opt(string text, string next = null, Action effect = null)
        {
            lastOpt = new DOption { text = text, next = next, effect = effect, end = next == null };
            cur.options.Add(lastOpt); return this;
        }
        public DB If(Func<bool> c) { lastOpt.cond = c; return this; }
        public DB Once(string id = null) { lastOpt.once = true; lastOpt.id = id ?? def.id + "." + cur.id + "." + cur.options.Count; return this; }
        public DB Tag(string t) { lastOpt.tag = t; return this; }
        public DB Do(Action a) { if (lastOpt == null) cur.onEnter += a; else lastOpt.effect += a; return this; }
        public DB Inspire(BackgroundId b) { lastOpt.inspires = b; return this; }
        public DB Check(Skill s, int dc, string ok, string fail, bool adv = false) { lastOpt.skill = s; lastOpt.dc = dc; lastOpt.success = ok; lastOpt.failure = fail; lastOpt.adv = adv; lastOpt.end = false; return this; }
        public DB CheckAb(Ability a, int dc, string ok, string fail) { lastOpt.ability = a; lastOpt.dc = dc; lastOpt.success = ok; lastOpt.failure = fail; lastOpt.end = false; return this; }
    }

    public static class Dialogues
    {
        public static readonly Dictionary<string, DialogueDef> All = new Dictionary<string, DialogueDef>();
        public static Game G => Game.I;
        public static bool F(string k) => Game.I.Flag(k);
        public static bool Has(RaceId r) => Game.I.party.Any(c => c.Active && c.sheet.race == r);
        public static bool Has(ClassId c) => Game.I.party.Any(p => p.Active && p.sheet.cls == c);
        public static bool HasBg(BackgroundId b) => Game.I.party.Any(p => p.Active && p.sheet.background == b);
    }

    /// <summary>Plays dialogues: cinematic camera, typewriter text, options, skill checks.</summary>
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner I;
        public bool Active;
        DialogueDef def; Actor npc; public Actor speakerPC;
        VisualElement layer, box, optsBox, topBar, botBar; Label speakerLbl, textLbl;
        bool skipType; string chosen;
        GameMode prevMode;
        Action onFinish;

        void Awake() { I = this; }

        // dev/automation: inspect the current node and pick options from the console
        public string DevNode; public List<string> DevOptions = new List<string>(); public int DevPick = -1; public static bool DevFast;
        public string DevState() => Active ? $"[{def?.id}:{DevNode}] " + string.Join(" | ", DevOptions.Select((o, i) => i + ": " + o)) + " >> " + textLbl?.text : "inactive";

        public void Begin(string id, Actor npcActor, Actor pc, Action finished = null)
        {
            if (Active || !Dialogues.All.TryGetValue(id, out var d)) { if (!Dialogues.All.ContainsKey(id ?? "")) Debug.LogWarning("Missing dialogue " + id); return; }
            def = d; npc = npcActor; speakerPC = pc ?? Game.I.Selected; onFinish = finished;
            StartCoroutine(Run());
        }

        public void ForceEnd() { if (!Active) return; StopAllCoroutines(); Cleanup(); }

        void BuildUI()
        {
            var root = UIRoot.I.winLayer;
            layer = UIB.El("layer", root); layer.pickingMode = PickingMode.Ignore;
            topBar = UIB.El("letterbox", layer); topBar.style.top = 0; topBar.style.height = 0;
            botBar = UIB.El("letterbox", layer); botBar.style.bottom = 0; botBar.style.height = 0;
            var wrap = UIB.El("dlg-wrap", layer); wrap.pickingMode = PickingMode.Ignore;
            box = UIB.El("dlg-box", wrap);
            speakerLbl = UIB.Lbl("", "dlg-speaker", box); Theme.ApplyFont(speakerLbl, true);
            textLbl = UIB.Lbl("", "dlg-text", box);
            optsBox = UIB.Col(box);
            box.RegisterCallback<ClickEvent>(_ => skipType = true);
        }

        IEnumerator Run()
        {
            Active = true;
            prevMode = Game.I.mode;
            Game.I.mode = GameMode.Dialogue;
            UIRoot.I.hud.Hide();
            UIRoot.I.SetHoverLabel(null, Color.white);
            foreach (var a in Game.I.PartyActors) a.Stop();
            BuildUI();
            float t = 0;
            while (t < 0.35f) { t += Time.deltaTime; topBar.style.height = 90 * t / 0.35f; botBar.style.height = 90 * t / 0.35f; yield return null; }
            if (npc) { npc.Stop(); npc.Face(speakerPC ? speakerPC.transform.position : npc.transform.position + Vector3.forward); npc.anim?.LookAt(speakerPC ? speakerPC.HeadPos : (Vector3?)null); }
            if (speakerPC && npc) { speakerPC.Face(npc.transform.position); speakerPC.anim?.LookAt(npc.HeadPos); }
            Atmosphere.I?.SetDof(true, 2.5f);
            string nodeId = def.start();
            while (!string.IsNullOrEmpty(nodeId) && def.nodes.TryGetValue(nodeId, out var node))
            {
                node.onEnter?.Invoke();
                var spk = ResolveSpeaker(node);
                Frame(spk);
                if (spk != null) { spk.anim?.SetTalking(true); if (node.gesture != AnimAct.None) spk.PlayAnim(node.gesture); }
                speakerLbl.text = node.speaker ?? (spk != null ? spk.Name : "");
                string text = node.dynText != null ? node.dynText() : node.text;
                text = Fill(text);
                optsBox.Clear();
                yield return Type(text);
                if (spk != null) spk.anim?.SetTalking(false);
                if (node.end && node.options.Count == 0) { yield return WaitContinue(); break; }
                var opts = node.options.Where(o => (o.cond == null || o.cond()) && !(o.once && Game.I.Flag("dlg:" + o.id))).ToList();
                if (opts.Count == 0)
                {
                    yield return WaitContinue();
                    nodeId = node.next;
                    continue;
                }
                chosen = null;
                DOption pick = null;
                int k = 1;
                foreach (var o in opts)
                {
                    var opt = o;
                    string tag = o.tag;
                    string otext = o.text;
                    // a hand-written "[SKILL]" prefix replaces the automatic tag rather than doubling it
                    if (tag == null && otext.StartsWith("[") && otext.IndexOf(']') > 0) { tag = otext.Substring(0, otext.IndexOf(']') + 1); otext = otext.Substring(otext.IndexOf(']') + 1).TrimStart(); }
                    if (tag == null && o.skill.HasValue) tag = $"[{o.skill.Value.Nice().ToUpperInvariant()}]";
                    if (tag == null && o.ability.HasValue) tag = $"[{o.ability.Value.Long().ToUpperInvariant()}]";
                    string tagCol = o.skill.HasValue || o.ability.HasValue ? "#c9a0ff" : "#e6c67a";
                    string label = $"<color=#8a8278>{k}.</color> " + (tag != null ? $"<color={tagCol}>{tag}</color> " : "") + Fill(otext) + (o.end && o.next == null && !o.skill.HasValue && !o.ability.HasValue ? " <color=#6a6258>[Leave]</color>" : "");
                    var b = UIB.Btn(label, () => { pick = opt; }, "dlg-opt", optsBox);
                    if (o.once && Game.I.Flag("dlg:" + o.id)) b.AddToClassList("dlg-opt-used");
                    k++;
                }
                DevNode = node.id; DevOptions = opts.Select(o => o.text).ToList();
                while (pick == null)
                {
                    if (DevPick >= 0) { if (DevPick < opts.Count) pick = opts[DevPick]; DevPick = -1; if (pick != null) break; }
                    var kb = Keyboard.current;
                    for (int i = 0; i < opts.Count && i < 9; i++) if (kb[UnityEngine.InputSystem.Key.Digit1 + i].wasPressedThisFrame) pick = opts[i];
                    yield return null;
                }
                Audio.Sfx.Play("click");
                optsBox.Clear();
                if (pick.once) Game.I.SetFlag("dlg:" + pick.id);
                if (pick.inspires.HasValue && Dialogues.HasBg(pick.inspires.Value) && !Game.I.Flag("insp:" + pick.id))
                {
                    Game.I.SetFlag("insp:" + pick.id); Game.I.partyInspiration = Mathf.Min(4, Game.I.partyInspiration + 1);
                    Toast.Show($"Inspiration! ({pick.inspires.Value.Nice()} background)", Theme.Gold);
                }
                // echo the player's line
                if (speakerPC && !pick.skill.HasValue && !pick.ability.HasValue && pick.text.Length > 3 && !pick.text.StartsWith("*"))
                {
                    speakerLbl.text = speakerPC.Name; speakerLbl.style.color = Theme.Parchment;
                    Frame(speakerPC);
                    speakerPC.anim?.SetTalking(true);
                    yield return Type(Fill(pick.text), 1.6f);
                    speakerPC.anim?.SetTalking(false);
                    yield return new WaitForSeconds(0.25f);
                    speakerLbl.style.color = new Color(0.9f, 0.75f, 0.43f);
                }
                pick.effect?.Invoke();
                if (!Active) yield break;
                if (pick.skill.HasValue || pick.ability.HasValue)
                {
                    bool? ok = null;
                    var roller = BestRoller(pick);
                    if (pick.skill.HasValue) DiceRoller.Roll(roller, pick.skill.Value, pick.dc, Fill(pick.text), r => ok = r, pick.adv);
                    else DiceRoller.RollAbility(roller, pick.ability.Value, pick.dc, Fill(pick.text), r => ok = r);
                    while (!ok.HasValue) yield return null;
                    nodeId = ok.Value ? pick.success : pick.failure;
                    if (ok.Value) Game.I.GiveXP(20, null);
                    continue;
                }
                if (Game.I.mode != GameMode.Dialogue) break; // effect started combat etc.
                nodeId = pick.next;
                if (pick.end && pick.next == null) break;
            }
            Cleanup();
        }

        Creature BestRoller(DOption o) => speakerPC != null ? speakerPC.c : Game.I.SelectedC;

        void Cleanup()
        {
            Active = false;
            if (npc) { npc.anim?.LookAt(null); npc.anim?.SetTalking(false); }
            foreach (var a in Game.I.PartyActors) { a.anim?.LookAt(null); a.anim?.SetTalking(false); }
            layer?.RemoveFromHierarchy();
            Atmosphere.I?.SetDof(false);
            CameraRig.I.EndCinematic();
            if (Game.I.mode == GameMode.Dialogue) Game.I.mode = prevMode == GameMode.Dialogue ? GameMode.Explore : (prevMode == GameMode.Cutscene ? GameMode.Explore : prevMode);
            if (Game.I.mode != GameMode.Combat) UIRoot.I.hud.Show();
            var f = onFinish; onFinish = null;
            f?.Invoke();
        }

        Actor ResolveSpeaker(DNode n)
        {
            if (n.speakerActor == "pc") return speakerPC;
            if (!string.IsNullOrEmpty(n.speakerActor)) return Game.I.FindActor(n.speakerActor) ?? npc;
            return npc;
        }

        void Frame(Actor spk)
        {
            if (spk == null) return;
            var other = spk == speakerPC ? npc : speakerPC;
            Vector3 face = spk.HeadPos - Vector3.up * 0.08f;
            Vector3 dir = other != null ? (other.transform.position - spk.transform.position) : spk.transform.forward;
            dir.y = 0; if (dir.sqrMagnitude < 0.01f) dir = spk.transform.forward; dir.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            float dist = Mathf.Lerp(1.6f, 2.6f, Mathf.InverseLerp(0.9f, 2.2f, spk.Height));
            // over the listener's shoulder, a touch above the speaker's eye line
            Vector3 camPos = spk.transform.position + dir * dist + right * 0.55f;
            camPos.y = face.y + 0.12f;
            if (spk.Height < 1.2f) camPos.y = face.y + 0.3f;
            CameraRig.I.SetCinematic(camPos, face, 30f);
            Atmosphere.I?.SetDof(true, Vector3.Distance(camPos, face));
        }

        IEnumerator Type(string text, float speedMul = 1f)
        {
            skipType = false;
            textLbl.text = "";
            float cps = 55f * Settings.TextSpeed * speedMul;
            float shown = 0;
            yield return null;
            if (DevFast) skipType = true;
            while (shown < text.Length && !skipType)
            {
                shown += Time.deltaTime * cps;
                int n = Mathf.Min(text.Length, (int)shown);
                // don't cut inside rich-text tags
                string s = text.Substring(0, n);
                int lt = s.LastIndexOf('<'), gt = s.LastIndexOf('>');
                if (lt > gt) s = s.Substring(0, lt);
                textLbl.text = s;
                var kb = Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame && !UIRoot.PointerOverUI) skipType = true;
                yield return null;
            }
            textLbl.text = text;
            yield return null;
        }

        IEnumerator WaitContinue()
        {
            var b = UIB.Btn("<color=#8a8278>Continue...</color>", () => chosen = "c", "dlg-opt", optsBox);
            chosen = null;
            DevOptions = new List<string> { "(continue)" };
            while (chosen == null)
            {
                if (DevPick >= 0) { DevPick = -1; chosen = "c"; break; }
                var kb = Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.digit1Key.wasPressedThisFrame) chosen = "c";
                yield return null;
            }
            optsBox.Clear();
        }

        string Fill(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var pc = speakerPC?.c ?? Game.I.SelectedC;
            if (pc == null) return s;
            return s.Replace("{name}", pc.name).Replace("{race}", pc.sheet.Race.name).Replace("{class}", pc.sheet.Class.name);
        }
    }
}
