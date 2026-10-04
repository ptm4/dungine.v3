using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// v3 phase 4: the HUD's icons and portraits from the library.
    ///   Actions and spells: ui/icons/actions|spells/&lt;id&gt;/&lt;id&gt;_64.png (reviews 27 to 29: a voxel motif over stained
    ///   glass, a full tile), copied to Resources/Library/Icons/actions|spells/&lt;id&gt;.png. v2's action ids are the library's,
    ///   bar a few aliases.
    ///   Consumables: the item's sprite.png, copied to Resources/Library/Icons/items/&lt;v2 item id&gt;.png.
    ///   Portraits: the key characters' face.png renders, copied to Resources/Library/Portraits/&lt;id&gt;.png.
    /// </summary>
    public static class LibraryIcons
    {
        public static bool On = true;

        static readonly Dictionary<string, string> Alias = new Dictionary<string, string>
        {
            ["attack"] = "main_hand_attack", ["offhand_attack"] = "off_hand_attack", ["flourish"] = "defensive_flourish",
            ["protection_evil"] = "protection_from_evil_and_good", ["spiritual_strike"] = "spiritual_weapon",
        };
        // v2 item ids (stash consumables) -> the copied sprite's name
        static readonly Dictionary<string, string> Items = new Dictionary<string, string>
        {
            ["potion_healing"] = "potion_healing", ["scroll_magic_missile"] = "scroll", ["bread"] = "bread",
        };
        // the party's sheet names -> portrait ids
        static readonly Dictionary<string, string> Faces = new Dictionary<string, string>
        {
            ["arkus"] = "arkus", ["chairn"] = "chairn", ["chai'rn"] = "chairn",
        };

        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        static Texture2D Load(string path)
        {
            if (cache.TryGetValue(path, out var t)) return t;
            t = Resources.Load<Texture2D>(path);
            if (t) { t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; }
            cache[path] = t;
            return t;
        }

        /// <summary>The library's icon for an action (by v2's action id), or null.</summary>
        public static Texture2D Action(string actionId)
        {
            if (!On || string.IsNullOrEmpty(actionId)) return null;
            var id = Alias.TryGetValue(actionId, out var a) ? a : actionId;
            return Load("Library/Icons/actions/" + id) ?? Load("Library/Icons/spells/" + id);
        }

        /// <summary>The library's sprite for a v2 item, or null.</summary>
        public static Texture2D Item(string itemId)
        {
            if (!On || string.IsNullOrEmpty(itemId) || !Items.TryGetValue(itemId, out var n)) return null;
            return Load("Library/Icons/items/" + n);
        }

        /// <summary>The library's portrait for a party member, or null (then v2's live render).</summary>
        public static Texture2D Portrait(Rules.Creature c)
        {
            if (!On || c == null || string.IsNullOrEmpty(c.name)) return null;
            if (!Faces.TryGetValue(c.name.ToLowerInvariant(), out var id)) return null;
            var t = Load("Library/Portraits/" + id);
            if (t) t.filterMode = FilterMode.Bilinear;
            return t;
        }
    }
}
