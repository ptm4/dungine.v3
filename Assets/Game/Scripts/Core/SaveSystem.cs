using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dungine.Rules;
using Dungine.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dungine
{
    [Serializable] public class KV { public string k; public int v; public KV(string k, int v) { this.k = k; this.v = v; } }
    [Serializable] public class CondSave { public Cond id; public int turns; public int value; }

    [Serializable]
    public class PCSave
    {
        public CharacterSheet sheet;
        public int hp, tempHP, maxHPBonus, maxHPPenalty, pactUsed, strDrain, deathSucc, deathFail;
        public int[] slotsUsed = new int[4];
        public int bonusSlots;
        public List<KV> resUsed = new List<KV>(), uses = new List<KV>();
        public List<CondSave> conds = new List<CondSave>();
        public Equipment eq;
        public bool dead, relentlessUsed, vigilance;
        public Vector3 pos; public float yaw;
    }

    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string name, savedAt, area, spawn, partyDesc;
        public float playTime;
        public int gold, shortRests, inspiration, selected;
        public List<PCSave> party = new List<PCSave>();
        public List<ItemStack> stash = new List<ItemStack>();
        public Journal journal;
        public List<KV> flags = new List<KV>();
    }

    public class SaveInfo { public string name, area, party; public DateTime time; public float playTime; }

    public static class SaveSystem
    {
        static string Dir { get { var d = Path.Combine(Application.persistentDataPath, "saves"); Directory.CreateDirectory(d); return d; } }
        static string PathFor(string name) => Path.Combine(Dir, name + ".json");

        public static void Autosave() { if (Game.I.mode == GameMode.Explore && Game.I.party.Count > 0) Save("Autosave", false); }
        public static void QuickSave() { Save("Quicksave"); Toast.Show("Quicksaved.", Theme.Success); }
        public static void QuickLoad() { if (File.Exists(PathFor("Quicksave"))) Load("Quicksave"); else Toast.Show("No quicksave yet.", Theme.TextDim); }

        public static void Save(string name, bool announce = true)
        {
            var g = Game.I;
            if (g.party.Count == 0 || g.area == null) return;
            var d = new SaveData
            {
                name = name, savedAt = DateTime.Now.ToString("o"), area = g.areaId, spawn = g.lastSpawn, playTime = g.playTime,
                gold = g.gold, shortRests = g.shortRestsUsed, inspiration = g.partyInspiration, selected = g.selectedIndex,
                stash = g.stash.items.ToList(), journal = g.journal,
                partyDesc = string.Join(", ", g.party.Select(c => $"{c.name} L{c.level}"))
            };
            foreach (var kv in g.flags) d.flags.Add(new KV(kv.Key, kv.Value));
            foreach (var c in g.party)
            {
                var src = c.wildshapeBackup != null ? c.wildshapeBackup : c;
                var p = new PCSave
                {
                    sheet = c.sheet, hp = c.wildshapeBackup != null ? src.hp : c.hp, tempHP = c.tempHP, maxHPBonus = c.maxHPBonus, maxHPPenalty = c.maxHPPenalty,
                    pactUsed = c.pactUsed, strDrain = c.strDrain, deathSucc = c.deathSucc, deathFail = c.deathFail, slotsUsed = (int[])c.slotsUsed.Clone(),
                    bonusSlots = c.slotsMax[1] - RulesData.SlotsFor(c.sheet.Class.caster, c.level)[1],
                    eq = c.eq, dead = c.dead, relentlessUsed = c.relentlessUsed, vigilance = c.vigilance
                };
                foreach (var kv in c.resUsed) p.resUsed.Add(new KV(kv.Key, kv.Value));
                foreach (var kv in c.uses) p.uses.Add(new KV(kv.Key, kv.Value));
                foreach (var x in c.conds) if (x.turns < 0 && x.id != Cond.Dead && x.id != Cond.Downed && x.id != Cond.Concentrating) p.conds.Add(new CondSave { id = x.id, turns = x.turns, value = x.value });
                if (c.actor) { p.pos = c.actor.transform.position; p.yaw = c.actor.transform.eulerAngles.y; }
                d.party.Add(p);
            }
            File.WriteAllText(PathFor(name), JsonUtility.ToJson(d, true));
            if (announce && name != "Autosave") Debug.Log("[Dungine] Saved " + name);
        }

        public static IEnumerable<SaveInfo> List()
        {
            foreach (var f in Directory.GetFiles(Dir, "*.json").OrderByDescending(File.GetLastWriteTime))
            {
                SaveInfo info = null;
                try
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(f));
                    info = new SaveInfo { name = Path.GetFileNameWithoutExtension(f), area = AreaTitle(d.area), party = d.partyDesc, time = File.GetLastWriteTime(f), playTime = d.playTime };
                }
                catch { }
                if (info != null) yield return info;
            }
        }

        static string AreaTitle(string id) => World.AreaLoader.Areas.TryGetValue(id ?? "", out var a) ? a.Title : id;

        public static bool Any() => List().Any();
        public static void Delete(string name) { var p = PathFor(name); if (File.Exists(p)) File.Delete(p); }
        public static void LoadLatest() { var l = List().FirstOrDefault(); if (l != null) Load(l.name); else Game.I.GoToMainMenu(); }

        public static void Load(string name)
        {
            var p = PathFor(name);
            if (!File.Exists(p)) { Toast.Show("Save not found.", Theme.Failure); return; }
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
            Game.I.StartCoroutine(LoadRoutine(d));
        }

        static IEnumerator LoadRoutine(SaveData d)
        {
            var g = Game.I;
            Combat.CombatManager.I.StopAllCoroutines();
            if (Combat.CombatManager.I.Active) { Combat.CombatManager.I.Active = false; Combat.CombatManager.I.All.Clear(); }
            Dialogue.DialogueRunner.I?.ForceEnd();
            UIRoot.I.CloseAll();
            MenuScene.Clear(); CreationScene.Clear();
            MainMenuScreen.Hide(); CreationScreen.Hide();
            g.party.Clear();
            g.flags = d.flags.ToDictionary(k => k.k, k => k.v);
            g.stash = new Stash { items = d.stash ?? new List<ItemStack>() };
            foreach (var s in g.stash.items) Items.BumpUid(s.uid + 1);
            g.journal = d.journal ?? new Journal();
            g.gold = d.gold; g.playTime = d.playTime; g.shortRestsUsed = d.shortRests; g.partyInspiration = d.inspiration;
            foreach (var ps in d.party)
            {
                var c = Creature.FromSheet(ps.sheet);
                c.eq = ps.eq ?? new Equipment();
                c.hp = ps.hp; c.tempHP = ps.tempHP; c.maxHPBonus = ps.maxHPBonus; c.maxHPPenalty = ps.maxHPPenalty; c.pactUsed = ps.pactUsed;
                c.strDrain = ps.strDrain; c.deathSucc = ps.deathSucc; c.deathFail = ps.deathFail; c.slotsUsed = ps.slotsUsed ?? new int[4];
                c.slotsMax[1] += Mathf.Max(0, ps.bonusSlots);
                c.dead = ps.dead; c.relentlessUsed = ps.relentlessUsed; c.vigilance = ps.vigilance;
                foreach (var kv in ps.resUsed) c.resUsed[kv.k] = kv.v;
                foreach (var kv in ps.uses) c.uses[kv.k] = kv.v;
                foreach (var cs in ps.conds) c.AddCond(cs.id, cs.turns, null, 0, Ability.WIS, false, null, cs.value);
                if (c.dead) c.AddCond(Cond.Dead, -1);
                g.party.Add(c);
            }
            g.selectedIndex = Mathf.Clamp(d.selected, 0, g.party.Count - 1);
            yield return g.LoadArea(d.area, d.spawn, () =>
            {
                for (int i = 0; i < g.party.Count && i < d.party.Count; i++)
                {
                    var a = g.party[i].actor;
                    if (a && d.party[i].pos != Vector3.zero) { a.Warp(d.party[i].pos); a.transform.rotation = Quaternion.Euler(0, d.party[i].yaw, 0); }
                }
                if (g.Selected) CameraRig.I.SnapTo(g.Selected.transform.position, CameraRig.I.yaw);
                Toast.Show("Game loaded.", Theme.Success);
            });
        }
    }

    /// <summary>Persistent player preferences.</summary>
    public static class Settings
    {
        public static float Master = 0.8f, Music = 0.6f, Effects = 0.8f, Ambience = 0.7f, TextSpeed = 1f, PanSpeed = 1f;
        public static bool AutoReactions = true, ShowLog = false, HighShadows = true, Grain = true;

        public static void Load()
        {
            Master = PlayerPrefs.GetFloat("vol_master", Master); Music = PlayerPrefs.GetFloat("vol_music", Music);
            Effects = PlayerPrefs.GetFloat("vol_fx", Effects); Ambience = PlayerPrefs.GetFloat("vol_amb", Ambience);
            TextSpeed = PlayerPrefs.GetFloat("text_speed", TextSpeed); PanSpeed = PlayerPrefs.GetFloat("pan_speed", PanSpeed);
            AutoReactions = PlayerPrefs.GetInt("auto_react", 1) == 1; ShowLog = PlayerPrefs.GetInt("show_log", 0) == 1;
            HighShadows = PlayerPrefs.GetInt("hi_shadows", 1) == 1; Grain = PlayerPrefs.GetInt("grain", 1) == 1;
            if (CameraRig.I) CameraRig.I.panSpeed = PanSpeed;
            Audio.AudioSys.I?.ApplyVolumes();
            ApplyGraphics();
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("vol_master", Master); PlayerPrefs.SetFloat("vol_music", Music); PlayerPrefs.SetFloat("vol_fx", Effects); PlayerPrefs.SetFloat("vol_amb", Ambience);
            PlayerPrefs.SetFloat("text_speed", TextSpeed); PlayerPrefs.SetFloat("pan_speed", PanSpeed);
            PlayerPrefs.SetInt("auto_react", AutoReactions ? 1 : 0); PlayerPrefs.SetInt("show_log", ShowLog ? 1 : 0);
            PlayerPrefs.SetInt("hi_shadows", HighShadows ? 1 : 0); PlayerPrefs.SetInt("grain", Grain ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ApplyGraphics()
        {
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) urp.shadowDistance = HighShadows ? 80f : 40f;
            if (Visual.Atmosphere.I?.Current != null && !Grain) { }
        }
    }
}
