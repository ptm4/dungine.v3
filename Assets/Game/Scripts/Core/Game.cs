using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dungine.Combat;
using Dungine.Rules;
using Dungine.UI;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine
{
    /// <summary>The root of the running game: party, world state, area travel, rests and progression.</summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public GameMode mode = GameMode.Boot;
        public readonly List<Creature> party = new List<Creature>();
        public Stash stash = new Stash();
        public int gold;
        public Journal journal = new Journal();
        public Dictionary<string, int> flags = new Dictionary<string, int>();
        public string areaId, lastSpawn;
        public AreaContext area;
        public float playTime;
        public int shortRestsUsed;
        public int partyInspiration;
        public bool nightfall;
        public int selectedIndex;
        public event Action OnPartyChanged, OnSelectionChanged;
        public bool busy;           // loading / cutscene lock

        public List<Actor> PartyActors => party.Where(c => c.actor != null).Select(c => c.actor).ToList();
        public Actor Selected => selectedIndex >= 0 && selectedIndex < party.Count ? party[selectedIndex].actor : null;
        public Creature SelectedC => selectedIndex >= 0 && selectedIndex < party.Count ? party[selectedIndex] : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("Game");
            DontDestroyOnLoad(go);
            go.AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 1;
            Atmosphere.Ensure();
            gameObject.AddComponent<CameraRig>();
            gameObject.AddComponent<Occluders>();
            gameObject.AddComponent<Audio.AudioSys>();
            gameObject.AddComponent<UIRoot>();
            gameObject.AddComponent<CombatManager>();
            gameObject.AddComponent<Dialogue.DialogueRunner>();
            gameObject.AddComponent<PartyController>();
            gameObject.AddComponent<FX>();
            Campaign.RegisterAll();
        }

        IEnumerator Start()
        {
            yield return null;
            Settings.Load();
            // testing aid: "-devstart area:spawn" jumps straight into an area with the premade party
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-devstart");
            if (i >= 0 && i + 1 < args.Length)
            {
                var parts = args[i + 1].Split(':');
                DevStart(parts[0], parts.Length > 1 ? parts[1] : null);
                // "-devview x:z:yaw:zoom" then moves the party and camera, for checking a shot in a build
                int v = Array.IndexOf(args, "-devview");
                if (v >= 0 && v + 1 < args.Length) StartCoroutine(DevView(args[v + 1]));
                yield break;
            }
            if (mode != GameMode.Boot) yield break;   // something (a dev start, a load) already took over
            GoToMainMenu();
        }

        void Update()
        {
            if (mode == GameMode.Explore || mode == GameMode.Combat || mode == GameMode.Dialogue) playTime += Time.unscaledDeltaTime;
        }

        // ---------------------------------------------------------------- flags
        public int GetFlag(string k) => flags.TryGetValue(k, out var v) ? v : 0;
        public void SetFlag(string k, int v = 1) => flags[k] = v;
        public bool Flag(string k) => GetFlag(k) > 0;
        public void AddFlag(string k, int d = 1) => flags[k] = GetFlag(k) + d;

        // ---------------------------------------------------------------- menus
        public void GoToMainMenu()
        {
            StopAllCoroutines();
            ClearWorld();
            party.Clear();
            mode = GameMode.MainMenu;
            MenuScene.Build();
            UIRoot.I.ShowMainMenu();
            Audio.AudioSys.I.PlayMusic("menu");
            Audio.AudioSys.I.PlayAmbience("wind");
        }

        public void OpenCreation()
        {
            mode = GameMode.Creation;
            MenuScene.Clear();
            CreationScene.Build();
            UIRoot.I.ShowCreation();
            Audio.AudioSys.I.PlayMusic("creation");
        }

        // ---------------------------------------------------------------- new game
        /// <summary>Dev: start with the premade party directly in an area, skipping menus and narration.</summary>
        public void DevStart(string areaId, string spawn)
        {
            StopAllCoroutines();
            MenuScene.Clear();
            MainMenuScreen.Hide(); CreationScreen.Hide();
            devSkipIntro = true; devArea = areaId; devSpawn = spawn;
            NewGame(CreationScreen.Premade());
        }
        bool devSkipIntro; string devArea, devSpawn;

        IEnumerator DevView(string spec)
        {
            var f = spec.Split(':');
            float Num(int k, float d) => k < f.Length && float.TryParse(f[k], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : d;
            while (mode != GameMode.Explore || area == null) yield return null;
            yield return new WaitForSeconds(1f);
            Dev.PartyAt(Num(0, 0), Num(1, 0));
            CameraRig.I.SetView(Num(2, CameraRig.I.yaw), Num(3, CameraRig.I.zoom));
        }

        public void NewGame(List<CharacterSheet> sheets)
        {
            CreationScene.Clear();
            party.Clear(); flags.Clear(); stash = new Stash(); journal = new Journal(); gold = 40; playTime = 0; shortRestsUsed = 0; partyInspiration = 0;
            foreach (var s in sheets)
            {
                var c = Creature.FromSheet(s);
                foreach (var id in s.StartingItems) { var st = new ItemStack(id); if (!AutoEquip(c, st)) stash.Add(st); }
                party.Add(c);
            }
            stash.Add("potion_healing", 5);
            stash.Add("bread", 2);
            stash.Add("torch", 1);
            stash.Add("letter_kolyan", 1);
            selectedIndex = 0;
            StartCoroutine(IntroThenStart());
        }

        IEnumerator IntroThenStart()
        {
            mode = GameMode.Cutscene;
            if (devSkipIntro) { devSkipIntro = false; GoToArea(devArea, devSpawn); yield break; }
            yield return UIRoot.I.Narration(Campaign.IntroText, Campaign.IntroTitle);
            GoToArea("svalich_road", "start");
        }

        public bool AutoEquip(Creature c, ItemStack st)
        {
            var d = st.Def; if (d == null || !d.Equippable) return false;
            var slot = Equipment.SlotFor(d, c.eq);
            if (d.kind == ItemKind.Weapon && !d.IsRanged && c.eq.main != null)
            {
                if (c.eq.off == null && d.Has(WeaponProps.Light) && c.eq.main.Def.Has(WeaponProps.Light)) slot = Slot.OffHand;
                else return false;
            }
            if (c.eq.Get(slot) != null) return false;
            if (d.kind == ItemKind.Weapon && !d.IsRanged && d.Has(WeaponProps.TwoHanded) && c.eq.off != null) return false;
            c.eq.Set(slot, st);
            return true;
        }

        // ---------------------------------------------------------------- equipment
        public void Equip(Creature c, ItemStack st)
        {
            var d = st.Def; if (d == null || !d.Equippable) return;
            if (mode == GameMode.Combat && CombatManager.I.Current != c) { Toast.Show("You can only change equipment on your own turn.", Theme.Failure); return; }
            var slot = Equipment.SlotFor(d, c.eq);
            if (d.kind == ItemKind.Weapon && !d.IsRanged && d.Has(WeaponProps.Light) && c.eq.main != null && c.eq.main.Def.Has(WeaponProps.Light) && c.eq.off == null && UIRoot.ShiftHeld) slot = Slot.OffHand;
            var old = c.eq.Get(slot);
            stash.Remove(st);
            if (old != null) stash.Add(old);
            c.eq.Set(slot, st);
            if (d.kind == ItemKind.Weapon && !d.IsRanged && d.Has(WeaponProps.TwoHanded) && c.eq.off != null) { stash.Add(c.eq.off); c.eq.off = null; }
            if ((d.kind == ItemKind.Shield) && c.eq.main != null && c.eq.main.Def.Has(WeaponProps.TwoHanded)) { stash.Add(c.eq.main); c.eq.main = null; }
            if (d.kind == ItemKind.Armor && !c.ProficientArmor(d)) Toast.Show($"{c.name} isn't proficient with {d.name}: disadvantage on attacks and no spellcasting.", Theme.Failure);
            if (d.kind == ItemKind.Weapon && !c.ProficientWith(d)) Toast.Show($"{c.name} isn't proficient with {d.name}.", Theme.TextDim);
            RefreshVisual(c);
            Audio.Sfx.Play("equip");
            OnPartyChanged?.Invoke();
        }

        public void Unequip(Creature c, Slot slot)
        {
            var old = c.eq.Get(slot); if (old == null) return;
            c.eq.Set(slot, null); stash.Add(old);
            RefreshVisual(c);
            Audio.Sfx.Play("equip");
            OnPartyChanged?.Invoke();
        }

        public void RefreshVisual(Creature c)
        {
            if (c.actor == null) return;
            bool sel = c.actor == Selected;
            var a = ActorFactory.Rebuild(c.actor);
            a.transform.SetParent(area?.root, true);
            if (mode == GameMode.Combat) a.anim.SetCombat(true);
            PartyController.I.OnActorRebuilt(a, sel);
            PortraitRenderer.I?.Invalidate(c);
        }

        public void NotifyPartyChanged() => OnPartyChanged?.Invoke();

        // ---------------------------------------------------------------- selection
        public void Select(int idx)
        {
            if (idx < 0 || idx >= party.Count) return;
            if (mode == GameMode.Combat) return;
            selectedIndex = idx;
            foreach (var c in party) if (c.actor) c.actor.SetRing(c == party[idx], Theme.Gold);
            OnSelectionChanged?.Invoke();
            Audio.Sfx.Play("select");
        }

        public void SelectCreature(Creature c)
        {
            int i = party.IndexOf(c);
            if (i >= 0) { selectedIndex = i; foreach (var p in party) if (p.actor) p.actor.SetRing(p == c, Theme.Gold); OnSelectionChanged?.Invoke(); }
        }

        // ---------------------------------------------------------------- areas
        public void GoToArea(string id, string spawn) => StartCoroutine(LoadArea(id, spawn, null));

        public IEnumerator LoadArea(string id, string spawn, Action after)
        {
            if (!AreaLoader.Areas.TryGetValue(id, out var def)) { Debug.LogError("No area " + id); yield break; }
            busy = true;
            var prev = mode;
            mode = GameMode.Loading;
            yield return UIRoot.I.FadeOut(0.6f, def.Title, def.Subtitle);
            ClearWorld();
            yield return null;
            bool first = GetFlag("visited:" + id) == 0;
            areaId = id; lastSpawn = spawn;
            area = AreaLoader.Build(def);
            yield return null;
            SpawnParty(spawn);
            SetFlag("visited:" + id, 1);
            CameraRig.I.EndCinematic();
            CameraRig.I.maxZoom = def.CameraMaxZoom;
            CameraRig.I.SnapTo(Selected ? Selected.transform.position : Vector3.zero, area.spawnYaw.TryGetValue(spawn ?? "", out var yw) ? yw : CameraRig.I.yaw);
            Audio.AudioSys.I.PlayMusic(def.Music);
            Audio.AudioSys.I.PlayAmbience(def.Ambience);
            mode = GameMode.Explore;
            UIRoot.I.ShowHUD();
            yield return UIRoot.I.FadeIn(0.8f);
            busy = false;
            def.OnEnter(area, first);
            after?.Invoke();
            if (first && !string.IsNullOrEmpty(def.Title)) Toast.Show(def.Title, Theme.Gold, 3f);
            SaveSystem.Autosave();
        }

        void SpawnParty(string spawn)
        {
            Vector3 p = Vector3.zero; float yaw = 0;
            if (spawn != null && area.spawns.TryGetValue(spawn, out var sp)) { p = sp; area.spawnYaw.TryGetValue(spawn, out yaw); }
            else if (area.spawns.Count > 0) { p = area.spawns.Values.First(); }
            var fwd = Quaternion.Euler(0, yaw, 0);
            for (int i = 0; i < party.Count; i++)
            {
                var c = party[i];
                Vector3 off = fwd * (i == 0 ? Vector3.zero : new Vector3((i == 1 ? -1 : 1) * 0.9f, 0, -1.1f));
                Vector3 pos = p + off;
                if (NavMesh.SamplePosition(pos, out var hit, 4f, NavMesh.AllAreas)) pos = hit.position;
                var a = ActorFactory.Spawn(c, pos, fwd, 512);
                a.transform.SetParent(area.root, true);
                a.SetRing(i == selectedIndex, Theme.Gold);
            }
            PortraitRenderer.I?.InvalidateAll();
            OnPartyChanged?.Invoke();
        }

        void ClearWorld()
        {
            foreach (var c in party) { if (c.actor) Destroy(c.actor.gameObject); c.actor = null; }
            if (area != null && area.root) Destroy(area.root.gameObject);
            area = null;
            FX.I?.ClearAll();
            NavMesh.RemoveAllNavMeshData();
            Kit.ClearCache();
            Occluders.Clear();
        }

        public Actor FindActor(string id) => area != null && area.byId.TryGetValue(id, out var a) && a ? a : null;

        // ---------------------------------------------------------------- xp & rest
        public void GiveXP(int xp, string reason = null)
        {
            if (xp <= 0) return;
            bool anyLevel = false;
            foreach (var c in party)
            {
                if (c.sheet == null) continue;
                int before = c.sheet.xp;
                c.sheet.xp += xp;
                if (c.sheet.level < RulesData.MaxLevel && c.sheet.xp >= RulesData.XpForLevel[c.sheet.level + 1] && before < RulesData.XpForLevel[c.sheet.level + 1]) anyLevel = true;
            }
            Toast.Show($"+{xp} XP" + (reason != null ? " — " + reason : ""), Theme.Magic);
            if (anyLevel) { Toast.Show("Level up available! Open the character sheet (C).", Theme.Gold, 4f); Audio.Sfx.Play("levelup_ready"); }
            OnPartyChanged?.Invoke();
        }

        public bool CanLevelUp(Creature c) => c.sheet != null && c.sheet.level < RulesData.MaxLevel && c.sheet.xp >= RulesData.XpForLevel[c.sheet.level + 1];

        public bool ShortRest()
        {
            if (mode != GameMode.Explore) return false;
            if (shortRestsUsed >= 2) { Toast.Show("You need a long rest before resting again.", Theme.Failure); return false; }
            shortRestsUsed++;
            foreach (var c in party)
            {
                if (c.dead) continue;
                if (c.hp <= 0) c.hp = 1;
                int heal = Mathf.Max(1, c.MaxHPTotal / 2);
                if (party.Any(p => p.IsClass(ClassId.Bard) && p.level >= 2)) heal += Rng.D(6);
                c.hp = Mathf.Min(c.MaxHPTotal, c.hp + heal);
                c.pactUsed = 0;
                foreach (var k in new[] { "ki", "channel", "superiority", "wildshape" }) c.resUsed.Remove(k);
                if (c.IsClass(ClassId.Bard) && c.level >= 5) c.resUsed.Remove("inspiration");
                foreach (var a in c.AvailableActions().Where(a => a.recharge == Recharge.ShortRest)) c.uses.Remove(a.id);
                foreach (var wa in ActionLibrary.All.Values.Where(a => a.weaponAction)) c.uses.Remove(wa.id);
                if (c.IsClass(ClassId.Wizard) && GetFlag("arcane_recovery:" + c.uid) == 0)
                {
                    int budget = Mathf.CeilToInt(c.level / 2f);
                    for (int l = 3; l >= 1; l--) while (budget >= l && c.slotsUsed[l] > 0) { c.slotsUsed[l]--; budget -= l; }
                    SetFlag("arcane_recovery:" + c.uid, 1);
                }
                c.actor?.RefreshLife();
            }
            Toast.Show("The party takes a short rest.", Theme.Friendly);
            Audio.Sfx.Play("rest");
            OnPartyChanged?.Invoke();
            return true;
        }

        public void LongRest()
        {
            foreach (var c in party)
            {
                if (c.dead) continue;
                c.hp = c.MaxHPTotal; c.tempHP = 0;
                c.slotsUsed = new int[4]; c.pactUsed = 0; c.resUsed.Clear(); c.uses.Clear();
                c.conds.RemoveAll(x => x.id != Cond.MageArmor);
                c.strDrain = 0; c.maxHPPenalty = 0; c.relentlessUsed = false; c.vigilance = false;
                c.deathFail = 0; c.deathSucc = 0;
                SetFlag("arcane_recovery:" + c.uid, 0);
                c.actor?.RefreshLife();
            }
            shortRestsUsed = 0;
            OnPartyChanged?.Invoke();
        }

        public void CheckGameOver()
        {
            if (party.Count > 0 && party.All(c => c.dead || c.hp <= 0) && mode != GameMode.GameOver)
            {
                mode = GameMode.GameOver;
                StartCoroutine(GameOverRoutine());
            }
        }

        IEnumerator GameOverRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            UIRoot.I.ShowGameOver();
            Audio.AudioSys.I.PlayMusic("defeat");
        }
    }
}
