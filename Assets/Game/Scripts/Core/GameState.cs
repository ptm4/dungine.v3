using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using UnityEngine;

namespace Dungine
{
    public enum GameMode { Boot, MainMenu, Creation, Loading, Explore, Combat, Dialogue, Cutscene, GameOver }

    public enum QuestState { Unknown, Active, Done, Failed }

    [Serializable]
    public class QuestEntry
    {
        public string id, title;
        public QuestState state;
        public List<string> log = new List<string>();
        public int order;
    }

    /// <summary>Quests and lore the party has discovered.</summary>
    [Serializable]
    public class Journal
    {
        public List<QuestEntry> quests = new List<QuestEntry>();
        public List<string> lore = new List<string>();
        int counter;

        public QuestEntry Get(string id) => quests.FirstOrDefault(q => q.id == id);

        public void Update(string id, string title, string entry, QuestState st = QuestState.Active)
        {
            var q = Get(id);
            bool isNew = q == null;
            if (q == null) { q = new QuestEntry { id = id, title = title, order = counter++ }; quests.Add(q); }
            if (!string.IsNullOrEmpty(title)) q.title = title;
            if (!string.IsNullOrEmpty(entry) && !q.log.Contains(entry)) q.log.Add(entry);
            var prev = q.state;
            q.state = st;
            string msg = isNew ? "New quest: " + q.title : st == QuestState.Done && prev != QuestState.Done ? "Quest complete: " + q.title : st == QuestState.Failed ? "Quest failed: " + q.title : "Journal updated: " + q.title;
            UI.Toast.Show(msg, st == QuestState.Done ? UI.Theme.Success : UI.Theme.Gold);
            Audio.Sfx.Play(st == QuestState.Done ? "quest_done" : "journal");
        }

        public void AddLore(string text)
        {
            if (!lore.Contains(text)) lore.Add(text);
        }
    }

    /// <summary>Shared party inventory.</summary>
    [Serializable]
    public class Stash
    {
        public List<ItemStack> items = new List<ItemStack>();

        public void Add(string id, int count = 1)
        {
            var d = Items.Get(id);
            if (d == null) { Debug.LogWarning("Unknown item " + id); return; }
            if (d.stackable)
            {
                var ex = items.FirstOrDefault(i => i.id == id);
                if (ex != null) { ex.count += count; return; }
                items.Add(new ItemStack(id, count));
            }
            else for (int i = 0; i < count; i++) items.Add(new ItemStack(id, 1));
        }

        public void Add(ItemStack s)
        {
            if (s.Def != null && s.Def.stackable) Add(s.id, s.count);
            else items.Add(s);
        }

        public bool Has(string id, int count = 1) => items.Where(i => i.id == id).Sum(i => i.count) >= count;
        public int Count(string id) => items.Where(i => i.id == id).Sum(i => i.count);

        public bool Remove(string id, int count = 1)
        {
            if (!Has(id, count)) return false;
            while (count > 0)
            {
                var s = items.First(i => i.id == id);
                int take = Mathf.Min(count, s.count);
                s.count -= take; count -= take;
                if (s.count <= 0) items.Remove(s);
            }
            return true;
        }

        public void Remove(ItemStack s) => items.Remove(s);
        public float Weight => items.Sum(i => (i.Def?.weight ?? 0) * i.count);
    }
}
