using System.Collections.Generic;
using UnityEngine;

namespace Dungine.Library
{
    /// <summary>
    /// v3 phase 4: v2's props, as <c>Kit.Place</c> builds them, wear the library's model where the library has one.
    /// v2's object stays (its collider, its uses, whatever v2 attached to it) with its own mesh switched off, and the library
    /// model stands in it at true size. The table below is the swap list of coord/research/pixel3d_swap/inventory.md.
    ///
    /// Sources: agent N's game export (atlas, AO, LODs) where it has the prop, else the look test's bake of the library's
    /// model.glb (LookTest/Baked, vertex colour). Turn: the library's models face their own -y, which comes in as Unity +Z
    /// (glTFast mirrors x), the way v2's props face, unless the table says otherwise.
    /// </summary>
    public static class LibrarySwap
    {
        /// <summary>Dev switch (then rebuild the area).</summary>
        public static bool On = true;

        struct Swap
        {
            public string res; public bool export; public float yaw; public Vector3 offset; public bool sinkTopToGround;
            public Swap(string r, bool e, float y = 0, Vector3 o = default, bool sink = false) { res = r; export = e; yaw = y; offset = o; sinkTopToGround = sink; }
        }

        const string Export = "LookTest/Export/", Baked = "LookTest/Baked/";

        // v2's Kit.Place key (exact, or the part before a size suffix like "crate0.8") -> the library model
        static readonly Dictionary<string, Swap> Table = new Dictionary<string, Swap>
        {
            ["lamppost"] = new Swap(Export + "lamppost", true),
            ["barrel"] = new Swap(Export + "barrel", true),
            ["crate"] = new Swap(Export + "crate", true),
            ["bench"] = new Swap(Export + "bench", true),
            ["cart"] = new Swap(Export + "cart", true),
            ["haybale"] = new Swap(Export + "haybale", true),
            ["woodpile"] = new Swap(Export + "woodpile", true),
            ["gravemound"] = new Swap(Export + "grave_mound", true),
            // (the open grave is a cut into the ground: it waits for the library's terrain, which can have a hole in it)
            ["coffin_closed"] = new Swap(Export + "coffin", true),
        };

        /// <summary>Per-key corrections found by looking (degrees about Y, and a local offset in metres).</summary>
        public static readonly Dictionary<string, (float yaw, Vector3 offset)> Tweak = new Dictionary<string, (float, Vector3)>();

        public static int Swapped, Missed;
        static readonly HashSet<string> DropLod2 = new HashSet<string>();   // (the cart's shafts are whole at 6 cm since the 2026-10-04 re-export)
        static readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();

        static string Base(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            int i = key.Length;
            while (i > 0 && (char.IsDigit(key[i - 1]) || key[i - 1] == '.')) i--;
            return key.Substring(0, i);
        }

        public static bool Has(string key) => On && Table.ContainsKey(Base(key));

        /// <summary>Called by Kit.Place after v2 has built its prop. Returns the library model placed, or null.</summary>
        public static GameObject Apply(GameObject go, string key, float scale)
        {
            if (!On || !go) return null;
            if (!Table.TryGetValue(Base(key), out var s)) return null;
            if (!cache.TryGetValue(s.res, out var prefab)) cache[s.res] = prefab = Resources.Load<GameObject>(s.res);
            if (!prefab) { Missed++; return null; }
            foreach (var r in go.GetComponents<Renderer>()) r.enabled = false;
            var m = Object.Instantiate(prefab, go.transform, false);
            m.name = "Library_" + Base(key);
            float inv = Mathf.Abs(scale) > 1e-4f ? 1f / scale : 1f;
            m.transform.localScale = Vector3.one * inv;
            Tweak.TryGetValue(Base(key), out var tw);
            m.transform.localRotation = Quaternion.Euler(0, s.yaw + tw.yaw, 0);
            m.transform.localPosition = (s.offset + tw.offset) * inv;
            // any prop whose 6 cm level is broken stops at 3 cm (none now)
            if (DropLod2.Contains(Base(key))) foreach (var r in m.GetComponentsInChildren<Renderer>(true)) if (r.name == "Body_LOD2") Object.DestroyImmediate(r.gameObject);
            if (s.export) LibraryFigures.PrepareStatic(m, Resources.Load<TextAsset>(s.res)?.text);
            if (s.sinkTopToGround)
            {
                // a cut into the ground (the open grave): its block's top is the grass line
                var b = Bounds(m);
                if (b.size != Vector3.zero) m.transform.position += Vector3.up * (go.transform.position.y - b.max.y);
            }
            foreach (var r in m.GetComponentsInChildren<Renderer>(true)) { r.gameObject.layer = go.layer; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            Swapped++;
            return m;
        }

        static Bounds Bounds(GameObject m)
        {
            Bounds b = default; bool any = false;
            foreach (var r in m.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name.StartsWith("Body_LOD") && r.name != "Body_LOD0") continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return any ? b : default;
        }
    }
}
