using System;
using System.Collections.Generic;
using System.Linq;
using Dungine.Rules;
using Dungine.Visual;
using Dungine.World;
using UnityEngine;

namespace Dungine
{
    /// <summary>Something in the world the party can click on and use.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public string id;
        public string label = "Object";
        public float useRange = 1.6f;
        public bool hidden;               // requires a Perception check to notice
        public int perceptionDC;
        public bool once;
        public bool used;
        public Vector3 useOffset;
        List<Renderer> rends;
        MaterialPropertyBlock mpb;
        float hl;

        public virtual string Verb => "Use";
        public virtual bool Available => !hidden && !(once && used);
        public Vector3 UsePoint => transform.position + transform.rotation * useOffset;

        protected virtual void Awake()
        {
            gameObject.layer = Layers.Interact;
            mpb = new MaterialPropertyBlock();
        }

        public abstract void Use(Actor user);

        public void SetHover(bool on)
        {
            rends ??= GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToList();
            float target = on ? 1 : 0;
            if (Mathf.Approximately(target, hl)) return;
            hl = target;
            foreach (var r in rends)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", on ? new Color(0.35f, 0.28f, 0.12f) : Color.black);
                r.SetPropertyBlock(mpb);
            }
        }

        public void Reveal()
        {
            if (!hidden) return;
            hidden = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            UI.Toast.Show("Noticed: " + label, UI.Theme.Gold);
            Audio.Sfx.Play("discover");
        }

        public void SetHiddenVisual()
        {
            if (hidden) foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }

        protected void MarkUsed()
        {
            used = true;
            if (!string.IsNullOrEmpty(id)) Game.I.SetFlag("used:" + id, 1);
        }

        public static Collider AddBoxCollider(GameObject go, Vector3 center, Vector3 size)
        {
            var bc = go.AddComponent<BoxCollider>(); bc.center = center; bc.size = size; bc.isTrigger = true;
            return bc;
        }
    }

    /// <summary>Chests, corpses, crates, bookshelves: a loot list.</summary>
    public class Container : Interactable
    {
        public List<ItemStack> items = new List<ItemStack>();
        public int gold;
        public bool locked; public int lockDC = 12; public string keyId;
        public bool trapped; public int trapDC = 12; public string trapDamage = "2d6"; public DamageType trapType = DamageType.Poison; public bool trapFound;
        public GameObject closedModel, openModel;
        public override string Verb => locked ? "Unlock" : "Search";

        public override void Use(Actor user)
        {
            if (locked)
            {
                if (!string.IsNullOrEmpty(keyId) && Game.I.stash.Has(keyId)) { locked = false; UI.Toast.Show($"Unlocked with {Items.Get(keyId).name}.", UI.Theme.Gold); Audio.Sfx.Play("unlock"); }
                else
                {
                    UI.DiceRoller.Roll(user.c, Skill.SleightOfHand, lockDC, $"Pick the lock ({label})", ok =>
                    {
                        if (ok) { locked = false; Audio.Sfx.Play("unlock"); Open(user); }
                        else { UI.Toast.Show("The lock holds.", UI.Theme.Failure); Audio.Sfx.Play("locked"); }
                    });
                    return;
                }
            }
            Open(user);
        }

        void Open(Actor user)
        {
            if (trapped && !trapFound)
            {
                trapped = false;
                int dmg = DiceExpr.Parse(trapDamage).Roll();
                Combat.RulesEngine.DealDamage(null, user.c, dmg, trapType, false, null);
                UI.Toast.Show($"A needle trap! {dmg} {trapType} damage.", UI.Theme.Failure);
                FX.Burst(transform.position + Vector3.up * 0.6f, FxKind.Poison, 0.8f);
            }
            if (closedModel && openModel) { closedModel.SetActive(false); openModel.SetActive(true); }
            Audio.Sfx.Play("chest");
            if (!string.IsNullOrEmpty(id)) Game.I.SetFlag("opened:" + id, 1);
            UI.LootWindow.Open(this, user);
        }

        public void SyncOpen() { if (closedModel && openModel && Game.I.GetFlag("opened:" + id) > 0) { closedModel.SetActive(false); openModel.SetActive(true); } }
    }

    /// <summary>Doors and stairs that move the party to another area.</summary>
    public class Transition : Interactable
    {
        public string targetArea, targetSpawn;
        public bool locked; public string keyId; public int lockDC = 14; public string lockedText = "It's locked.";
        public string requireFlag; public string requireText;
        public override string Verb => "Enter";

        public override void Use(Actor user)
        {
            if (!string.IsNullOrEmpty(requireFlag) && Game.I.GetFlag(requireFlag) <= 0) { UI.Toast.Show(requireText ?? "You can't go there yet.", UI.Theme.TextDim); return; }
            if (Game.I.mode == GameMode.Combat) { UI.Toast.Show("You can't leave during combat.", UI.Theme.Failure); return; }
            if (locked)
            {
                if (!string.IsNullOrEmpty(keyId) && Game.I.stash.Has(keyId)) { locked = false; Game.I.SetFlag("unlocked:" + id, 1); UI.Toast.Show("Unlocked.", UI.Theme.Gold); Audio.Sfx.Play("unlock"); }
                else
                {
                    UI.DiceRoller.Roll(user.c, Skill.SleightOfHand, lockDC, $"Pick the lock", ok =>
                    {
                        if (ok) { locked = false; Game.I.SetFlag("unlocked:" + id, 1); Audio.Sfx.Play("unlock"); Go(); }
                        else { UI.Toast.Show(lockedText, UI.Theme.Failure); Audio.Sfx.Play("locked"); }
                    });
                    return;
                }
            }
            Go();
        }

        void Go()
        {
            Audio.Sfx.Play("door");
            Game.I.GoToArea(targetArea, targetSpawn);
        }
    }

    /// <summary>Books, notes, inscriptions.</summary>
    public class Readable : Interactable
    {
        public string title, text; public string loreKey; public string giveItem; public string setFlag;
        public override string Verb => "Read";
        public override void Use(Actor user)
        {
            UI.ReadWindow.Show(title, text);
            if (!string.IsNullOrEmpty(loreKey)) Game.I.journal.AddLore(title);
            if (!string.IsNullOrEmpty(giveItem) && !used) { Game.I.stash.Add(giveItem); UI.Toast.Show("Received: " + Items.Get(giveItem).name, UI.Theme.Gold); }
            if (!string.IsNullOrEmpty(setFlag)) Game.I.SetFlag(setFlag, 1);
            MarkUsed();
            Audio.Sfx.Play("page");
        }
    }

    /// <summary>A scripted one-shot interaction (levers, altars, statues, examine points).</summary>
    public class ScriptedUse : Interactable
    {
        public Action<Actor, ScriptedUse> onUse;
        public string verb = "Examine";
        public override string Verb => verb;
        public override void Use(Actor user) { onUse?.Invoke(user, this); if (once) MarkUsed(); }
    }

    /// <summary>A place to make camp and long rest (campfire, inn bed).</summary>
    public class RestSpot : Interactable
    {
        public override string Verb => "Rest";
        public override void Use(Actor user) => UI.RestMenu.Open();
    }

    /// <summary>An item lying on the ground.</summary>
    public class Pickup : Interactable
    {
        public string itemId; public int count = 1;
        public override string Verb => "Pick up";
        public override void Use(Actor user)
        {
            Game.I.stash.Add(itemId, count);
            UI.Toast.Show($"Picked up {Items.Get(itemId).name}" + (count > 1 ? $" x{count}" : ""), UI.Theme.Gold);
            Campaign.OnItemTaken(itemId);
            Audio.Sfx.Play("pickup");
            MarkUsed();
            gameObject.SetActive(false);
        }
    }

    /// <summary>Invisible volume that fires once when a party member enters.</summary>
    public class TriggerZone : MonoBehaviour
    {
        public string id; public float radius = 3f; public Action onEnter; public bool fired; public bool repeat;
        public string requireFlag, forbidFlag;
        void Update()
        {
            if (fired && !repeat) return;
            if (Game.I == null || Game.I.mode != GameMode.Explore) return;
            if (!string.IsNullOrEmpty(requireFlag) && Game.I.GetFlag(requireFlag) <= 0) return;
            if (!string.IsNullOrEmpty(forbidFlag) && Game.I.GetFlag(forbidFlag) > 0) return;
            foreach (var a in Game.I.PartyActors)
            {
                if (a == null) continue;
                Vector3 d = a.transform.position - transform.position; d.y = 0;
                if (d.magnitude < radius)
                {
                    fired = true;
                    if (!string.IsNullOrEmpty(id)) Game.I.SetFlag("trig:" + id, 1);
                    onEnter?.Invoke();
                    if (!repeat) enabled = false;
                    return;
                }
            }
        }
    }
}
