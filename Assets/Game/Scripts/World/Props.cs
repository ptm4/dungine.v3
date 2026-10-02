using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    /// <summary>Procedural prop meshes. Each returns (mesh, materials) for Kit.Place caching.</summary>
    public static class Props
    {
        static (Mesh, Material[]) M(MeshBuilder mb, string n, params Material[] m) => (mb.Build(n), m);

        public static (Mesh, Material[]) LampPost()
        {
            var mb = new MeshBuilder(3);
            mb.AddCylinder(0, Vector3.zero, 0.12f, 0.09f, 0.3f, 8);
            mb.AddCylinder(0, new Vector3(0, 0.3f, 0), 0.05f, 0.04f, 2.9f, 8);
            mb.AddBox(0, new Vector3(0.25f, 3.1f, 0), new Vector3(0.55f, 0.05f, 0.05f));
            mb.AddBox(0, new Vector3(0.45f, 2.75f, 0), new Vector3(0.24f, 0.04f, 0.24f));
            mb.AddCone(0, new Vector3(0.45f, 3.05f, 0), 0.2f, 0.18f, 4);
            mb.AddBox(1, new Vector3(0.45f, 2.9f, 0), new Vector3(0.18f, 0.28f, 0.18f));
            for (int i = 0; i < 4; i++) { mb.Push(); mb.Translate(new Vector3(0.45f, 2.9f, 0)); mb.Rotate(Quaternion.Euler(0, i * 90 + 45, 0)); mb.AddBox(0, new Vector3(0.12f, 0, 0), new Vector3(0.02f, 0.3f, 0.02f)); mb.Pop(); }
            return M(mb, "lamppost", Pal.Iron, MatLib.Emissive(new Color(1f, .75f, .4f), new Color(3f, 1.8f, .6f)), Pal.Iron);
        }

        public static (Mesh, Material[]) Well()
        {
            var mb = new MeshBuilder(4) { uvScale = 0.7f };
            int n = 14;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2;
                mb.Push(); mb.Translate(new Vector3(Mathf.Cos(a) * 0.95f, 0.45f, Mathf.Sin(a) * 0.95f)); mb.Rotate(Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0));
                mb.AddRoughBox(0, Vector3.zero, new Vector3(0.3f, 0.9f, 0.45f), 0.03f, i); mb.Pop();
            }
            mb.AddDisc(3, new Vector3(0, 0.3f, 0), 0.8f, 14, true);
            foreach (var s in new[] { -1, 1 }) mb.AddBox(1, new Vector3(s * 1.0f, 1.4f, 0), new Vector3(0.14f, 2.0f, 0.14f));
            mb.AddCylinder(1, new Vector3(-1.0f, 2.1f, 0), 0.06f, 0.06f, 0.1f, 6);
            mb.Push(); mb.Translate(new Vector3(0, 2.1f, 0)); mb.Rotate(Quaternion.Euler(0, 0, 90)); mb.AddCylinder(1, new Vector3(0, -1.05f, 0), 0.07f, 0.07f, 2.1f, 8); mb.Pop();
            mb.Push(); mb.Translate(new Vector3(0, 2.4f, 0));
            mb.AddPrism(2, new Vector3(-1.4f, 0, -0.8f), new Vector3(0, 0.8f, -0.8f), new Vector3(1.4f, 0, -0.8f), 1.6f);
            mb.Pop();
            mb.AddCylinder(1, new Vector3(0.3f, 1.4f, 0), 0.16f, 0.2f, 0.3f, 8);
            return M(mb, "well", Pal.Stone, Pal.TimberDark, Pal.Shingle, MatLib.Lit(new Color(.02f, .03f, .04f), null, .95f));
        }

        public static (Mesh, Material[]) Cart(bool broken)
        {
            var mb = new MeshBuilder(2) { uvScale = 0.8f };
            mb.AddBox(0, new Vector3(0, 0.75f, 0), new Vector3(1.4f, 0.08f, 2.4f));
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * 0.68f, 1.0f, 0), new Vector3(0.06f, 0.45f, 2.4f));
            mb.AddBox(0, new Vector3(0, 1.0f, -1.18f), new Vector3(1.4f, 0.45f, 0.06f));
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * 0.4f, 0.7f, 1.9f), new Vector3(0.07f, 0.07f, 1.6f));
            foreach (var s in new[] { -1, 1 })
            {
                if (broken && s > 0) { mb.Push(); mb.Translate(new Vector3(0.9f, 0.1f, -0.3f)); mb.Rotate(Quaternion.Euler(0, 0, 80)); }
                else { mb.Push(); mb.Translate(new Vector3(s * 0.78f, 0.55f, -0.3f)); mb.Rotate(Quaternion.Euler(0, 0, 90)); }
                mb.AddCylinder(1, new Vector3(0, -0.05f, 0), 0.55f, 0.55f, 0.1f, 14, true);
                mb.AddCylinder(0, new Vector3(0, -0.07f, 0), 0.12f, 0.12f, 0.14f, 8);
                mb.Pop();
            }
            return M(mb, broken ? "cart_broken" : "cart", Pal.Timber, Pal.TimberDark);
        }

        public static (Mesh, Material[]) Barrel()
        {
            var mb = new MeshBuilder(2);
            var rings = new List<MeshBuilder.Ring>();
            for (int i = 0; i <= 6; i++) { float t = i / 6f; rings.Add(new MeshBuilder.Ring { center = new Vector3(0, t * 0.95f, 0), rot = Quaternion.identity, rx = 0.3f + Mathf.Sin(t * Mathf.PI) * 0.06f, rz = 0.3f + Mathf.Sin(t * Mathf.PI) * 0.06f, weight = MeshBuilder.W(0) }); }
            mb.AddLoft(0, rings, 14, true, true, 1f);
            foreach (var y in new[] { 0.12f, 0.83f }) mb.AddCylinder(1, new Vector3(0, y, 0), 0.33f, 0.33f, 0.05f, 14, false);
            return M(mb, "barrel", Pal.Planks, Pal.Iron);
        }

        public static (Mesh, Material[]) Crate(float s)
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, s / 2, 0), Vector3.one * s);
            foreach (var y in new[] { 0.05f, s - 0.05f }) foreach (var z in new[] { -1, 1 }) mb.AddBox(1, new Vector3(0, y, z * s / 2), new Vector3(s + 0.02f, 0.08f, 0.04f));
            foreach (var x in new[] { -1, 1 }) foreach (var z in new[] { -1, 1 }) mb.AddBox(1, new Vector3(x * s / 2, s / 2, z * s / 2), new Vector3(0.06f, s, 0.06f));
            return M(mb, "crate" + s, Pal.Planks, Pal.TimberDark);
        }

        public static (Mesh, Material[]) Sack()
        {
            var mb = new MeshBuilder(1);
            mb.AddEllipsoid(0, new Vector3(0, 0.28f, 0), new Vector3(0.3f, 0.32f, 0.25f), 8, 10, p => new Vector3(p.x, p.y < 0 ? p.y * 0.6f : p.y, p.z) * (1 + 0.05f * Mathf.Sin(p.x * 30)));
            mb.AddCylinder(0, new Vector3(0, 0.58f, 0), 0.08f, 0.12f, 0.12f, 8);
            return M(mb, "sack", Pal.Fabric(new Color(.55f, .47f, .33f)));
        }

        public static (Mesh, Material[]) Bench(float len)
        {
            var mb = new MeshBuilder(1) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.45f, 0), new Vector3(len, 0.07f, 0.35f));
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * (len / 2 - 0.15f), 0.22f, 0), new Vector3(0.07f, 0.45f, 0.3f));
            return M(mb, "bench" + len, Pal.Timber);
        }

        public static (Mesh, Material[]) Table(float w, float d)
        {
            var mb = new MeshBuilder(1) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.78f, 0), new Vector3(w, 0.07f, d));
            foreach (var x in new[] { -1, 1 }) foreach (var z in new[] { -1, 1 }) mb.AddBox(0, new Vector3(x * (w / 2 - 0.1f), 0.38f, z * (d / 2 - 0.1f)), new Vector3(0.08f, 0.76f, 0.08f));
            mb.AddBox(0, new Vector3(0, 0.2f, 0), new Vector3(w - 0.25f, 0.05f, 0.05f));
            return M(mb, $"table{w}x{d}", Pal.Timber);
        }

        public static (Mesh, Material[]) Chair(bool fancy)
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            mb.AddBox(fancy ? 1 : 0, new Vector3(0, 0.46f, 0), new Vector3(0.45f, 0.06f, 0.45f));
            foreach (var x in new[] { -1, 1 }) foreach (var z in new[] { -1, 1 }) mb.AddBox(0, new Vector3(x * 0.19f, 0.22f, z * 0.19f), new Vector3(0.05f, 0.45f, 0.05f));
            mb.AddBox(0, new Vector3(0, fancy ? 0.95f : 0.8f, -0.2f), new Vector3(0.45f, fancy ? 0.9f : 0.62f, 0.05f));
            if (fancy) mb.AddBox(1, new Vector3(0, 0.95f, -0.17f), new Vector3(0.36f, 0.6f, 0.03f));
            return M(mb, fancy ? "chair_fancy" : "chair", Pal.TimberDark, Pal.Velvet);
        }

        public static (Mesh, Material[]) Bed(bool fourPoster, Color blanket)
        {
            var mb = new MeshBuilder(4) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.3f, 0), new Vector3(1.5f, 0.3f, 2.1f));
            mb.AddBox(1, new Vector3(0, 0.5f, 0.05f), new Vector3(1.4f, 0.15f, 1.95f));
            mb.AddBox(2, new Vector3(0, 0.6f, 0.25f), new Vector3(1.44f, 0.06f, 1.6f));
            mb.AddEllipsoid(3, new Vector3(0, 0.62f, -0.75f), new Vector3(0.55f, 0.1f, 0.2f), 5, 8);
            mb.AddBox(0, new Vector3(0, 0.75f, -1.02f), new Vector3(1.5f, 0.9f, 0.08f));
            if (fourPoster)
            {
                foreach (var x in new[] { -1, 1 }) foreach (var z in new[] { -1, 1 }) mb.AddCylinder(0, new Vector3(x * 0.72f, 0, z * 1.0f), 0.05f, 0.05f, 2.3f, 6);
                mb.AddBox(2, new Vector3(0, 2.3f, 0), new Vector3(1.55f, 0.05f, 2.1f));
                foreach (var x in new[] { -1, 1 }) mb.AddBox(2, new Vector3(x * 0.77f, 1.7f, 0), new Vector3(0.03f, 1.2f, 2.0f));
            }
            return M(mb, "bed" + fourPoster + blanket, Pal.TimberDark, Pal.Fabric(new Color(.7f, .66f, .58f)), Pal.Fabric(blanket), Pal.Fabric(new Color(.75f, .72f, .66f)));
        }

        public static (Mesh, Material[]) Bookshelf(int seed)
        {
            var rnd = new System.Random(seed);
            var mb = new MeshBuilder(3) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 1.1f, -0.15f), new Vector3(1.6f, 2.2f, 0.05f));
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * 0.78f, 1.1f, 0), new Vector3(0.05f, 2.2f, 0.36f));
            for (int shelf = 0; shelf < 5; shelf++)
            {
                float y = 0.05f + shelf * 0.5f;
                mb.AddBox(0, new Vector3(0, y, 0), new Vector3(1.55f, 0.04f, 0.34f));
                if (shelf == 4) continue;
                float x = -0.72f;
                while (x < 0.7f)
                {
                    float bw = 0.04f + (float)rnd.NextDouble() * 0.05f;
                    float bh = 0.28f + (float)rnd.NextDouble() * 0.14f;
                    if (rnd.NextDouble() < 0.12) { x += 0.12f; continue; }
                    mb.Push(); mb.Translate(new Vector3(x + bw / 2, y + 0.02f + bh / 2, 0.02f)); mb.Rotate(Quaternion.Euler(0, 0, rnd.NextDouble() < 0.1 ? 15 : 0));
                    mb.color = Color.HSVToRGB((float)rnd.NextDouble(), .5f, .35f);
                    mb.AddBox(rnd.NextDouble() < 0.5 ? 1 : 2, Vector3.zero, new Vector3(bw, bh, 0.24f)); mb.Pop();
                    x += bw + 0.005f;
                }
            }
            return M(mb, "bookshelf" + seed, Pal.TimberDark, MatLib.Lit(new Color(.4f, .15f, .12f), TexId.Leather, .3f), MatLib.Lit(new Color(.2f, .25f, .3f), TexId.Leather, .3f));
        }

        public static (Mesh, Material[]) Fireplace()
        {
            var mb = new MeshBuilder(3) { uvScale = 0.8f };
            mb.AddRoughBox(0, new Vector3(-0.85f, 0.7f, 0), new Vector3(0.4f, 1.4f, 0.6f), 0.02f, 1);
            mb.AddRoughBox(0, new Vector3(0.85f, 0.7f, 0), new Vector3(0.4f, 1.4f, 0.6f), 0.02f, 2);
            mb.AddRoughBox(0, new Vector3(0, 1.55f, 0.05f), new Vector3(2.2f, 0.3f, 0.7f), 0.02f, 3);
            mb.AddBox(0, new Vector3(0, 2.4f, -0.1f), new Vector3(1.4f, 1.6f, 0.4f));
            mb.AddBox(1, new Vector3(0, 0.7f, -0.2f), new Vector3(1.3f, 1.4f, 0.1f));
            mb.AddBox(0, new Vector3(0, 0.03f, 0.1f), new Vector3(2.0f, 0.06f, 0.9f));
            for (int i = 0; i < 3; i++) { mb.Push(); mb.Translate(new Vector3((i - 1) * 0.18f, 0.12f, 0.05f)); mb.Rotate(Quaternion.Euler(0, i * 50, 90)); mb.AddCylinder(2, new Vector3(0, -0.3f, 0), 0.06f, 0.06f, 0.6f, 6); mb.Pop(); }
            return M(mb, "fireplace", Pal.Stone, Pal.Black, Pal.Bark);
        }

        public static (Mesh, Material[]) Candelabra(bool floor)
        {
            var mb = new MeshBuilder(2);
            float h = floor ? 1.5f : 0.35f;
            mb.AddCylinder(0, Vector3.zero, floor ? 0.18f : 0.08f, 0.04f, 0.08f, 8);
            mb.AddCylinder(0, new Vector3(0, 0.08f, 0), 0.025f, 0.02f, h, 6);
            for (int i = -1; i <= 1; i++)
            {
                if (i != 0) mb.AddBox(0, new Vector3(i * 0.1f, h, 0), new Vector3(0.2f, 0.02f, 0.02f));
                mb.AddCylinder(0, new Vector3(i * 0.18f, h, 0), 0.035f, 0.035f, 0.02f, 6);
                mb.AddCylinder(1, new Vector3(i * 0.18f, h + 0.02f, 0), 0.02f, 0.02f, 0.12f, 6);
            }
            return M(mb, floor ? "candelabra_floor" : "candelabra", Pal.Iron, Pal.Candle);
        }

        public static (Mesh, Material[]) Chest(bool open)
        {
            var mb = new MeshBuilder(3) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.25f, 0), new Vector3(0.9f, 0.5f, 0.55f));
            if (open)
            {
                mb.Push(); mb.Translate(new Vector3(0, 0.5f, -0.27f)); mb.Rotate(Quaternion.Euler(-100, 0, 0));
                mb.AddBox(0, new Vector3(0, 0, 0.27f), new Vector3(0.9f, 0.1f, 0.55f)); mb.Pop();
                mb.AddBox(2, new Vector3(0, 0.46f, 0), new Vector3(0.8f, 0.04f, 0.45f));
            }
            else mb.AddCylinder(0, new Vector3(0, 0.5f, 0), 0.01f, 0.01f, 0.01f, 3, false);
            if (!open) { mb.Push(); mb.Translate(new Vector3(0, 0.5f, 0)); mb.Rotate(Quaternion.Euler(0, 0, 90)); mb.AddCylinder(0, new Vector3(0, -0.45f, 0), 0.28f, 0.28f, 0.9f, 10); mb.Pop(); }
            foreach (var x in new[] { -0.3f, 0.3f }) mb.AddBox(1, new Vector3(x, 0.3f, 0), new Vector3(0.06f, 0.62f, 0.58f));
            mb.AddBox(1, new Vector3(0, 0.4f, 0.28f), new Vector3(0.1f, 0.12f, 0.03f));
            return M(mb, open ? "chest_open" : "chest", Pal.Timber, Pal.Iron, Pal.Black);
        }

        public static (Mesh, Material[]) Wardrobe()
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 1.0f, 0), new Vector3(1.2f, 2.0f, 0.6f));
            mb.AddBox(0, new Vector3(0, 2.05f, 0), new Vector3(1.3f, 0.1f, 0.65f));
            mb.AddBox(1, new Vector3(0, 1.0f, 0.31f), new Vector3(0.02f, 1.8f, 0.02f));
            foreach (var x in new[] { -0.08f, 0.08f }) mb.AddEllipsoid(1, new Vector3(x, 1.0f, 0.33f), Vector3.one * 0.025f, 3, 5);
            return M(mb, "wardrobe", Pal.TimberDark, Pal.Iron);
        }

        public static (Mesh, Material[]) Rug(float w, float d, Color c)
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0, 0.01f, 0), new Vector3(w, 0.02f, d), 4, false);
            mb.AddBox(1, new Vector3(0, 0.012f, 0), new Vector3(w * 0.8f, 0.02f, d * 0.8f), 4, false);
            return M(mb, $"rug{w}{d}{c}", Pal.Fabric(c), Pal.Fabric(Color.Lerp(c, new Color(.7f, .6f, .3f), 0.4f)));
        }

        public static (Mesh, Material[]) Painting(float w, float h, int seed)
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, Vector3.zero, new Vector3(w + 0.12f, h + 0.12f, 0.06f));
            mb.AddBox(1, new Vector3(0, 0, 0.035f), new Vector3(w, h, 0.01f), 16);
            var rnd = new System.Random(seed);
            var c = Color.HSVToRGB((float)rnd.NextDouble(), .4f, .3f);
            return M(mb, "painting" + seed, Pal.Gold, MatLib.Lit(c, TexId.Plaster, .4f, 0, .5f, .3f));
        }

        public static (Mesh, Material[]) BarCounter(float len)
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.55f, 0), new Vector3(len, 1.1f, 0.6f));
            mb.AddBox(1, new Vector3(0, 1.12f, 0.05f), new Vector3(len + 0.1f, 0.06f, 0.75f));
            return M(mb, "bar" + len, Pal.TimberDark, Pal.Timber);
        }

        public static (Mesh, Material[]) Altar()
        {
            var mb = new MeshBuilder(3) { uvScale = 0.8f };
            mb.AddRoughBox(0, new Vector3(0, 0.5f, 0), new Vector3(2.2f, 1.0f, 1.0f), 0.03f, 5);
            mb.AddBox(0, new Vector3(0, 1.05f, 0), new Vector3(2.4f, 0.1f, 1.15f));
            mb.AddBox(1, new Vector3(0, 0.6f, 0.51f), new Vector3(1.6f, 0.8f, 0.02f));
            mb.AddBox(2, new Vector3(0, 1.11f, 0), new Vector3(1.0f, 0.02f, 0.5f));
            return M(mb, "altar", Pal.Ashlar, Pal.Velvet, Pal.Fabric(new Color(.8f, .78f, .7f)));
        }

        public static (Mesh, Material[]) BloodAltar()
        {
            var mb = new MeshBuilder(3) { uvScale = 0.8f };
            mb.AddRoughBox(0, new Vector3(0, 0.45f, 0), new Vector3(2.4f, 0.9f, 1.2f), 0.05f, 9);
            mb.AddBox(1, new Vector3(0, 0.905f, 0), new Vector3(1.8f, 0.01f, 0.8f), 4);
            for (int i = 0; i < 4; i++) { mb.AddCylinder(2, new Vector3(-0.9f + i * 0.6f, 0.9f, 0.45f), 0.03f, 0.03f, 0.15f, 6); }
            return M(mb, "bloodaltar", Pal.Stone, Pal.Blood, Pal.Candle);
        }

        public static (Mesh, Material[]) Pew(float len)
        {
            var mb = new MeshBuilder(1) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.45f, 0), new Vector3(len, 0.06f, 0.45f));
            mb.AddBox(0, new Vector3(0, 0.8f, -0.22f), new Vector3(len, 0.7f, 0.05f));
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * len / 2, 0.5f, -0.02f), new Vector3(0.06f, 1.0f, 0.5f));
            return M(mb, "pew" + len, Pal.TimberDark);
        }

        public static (Mesh, Material[]) Coffin(bool open)
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            var rings = new List<MeshBuilder.Ring>();
            float[] zs = { -1.0f, 0.45f, 1.0f }; float[] ws = { 0.25f, 0.38f, 0.28f };
            for (int i = 0; i < 3; i++) rings.Add(new MeshBuilder.Ring { center = new Vector3(0, 0.02f, zs[i]), rot = Quaternion.Euler(90, 0, 0), rx = ws[i], rz = 0.01f, weight = MeshBuilder.W(0) });
            mb.AddBox(0, new Vector3(0, 0.25f, 0), new Vector3(0.7f, 0.5f, 2.0f));
            if (!open) { mb.AddBox(0, new Vector3(0, 0.52f, 0), new Vector3(0.74f, 0.06f, 2.04f)); mb.AddBox(1, new Vector3(0, 0.56f, 0.3f), new Vector3(0.05f, 0.02f, 0.5f)); mb.AddBox(1, new Vector3(0, 0.56f, 0.4f), new Vector3(0.3f, 0.02f, 0.05f)); }
            else mb.AddBox(1, new Vector3(0, 0.42f, 0), new Vector3(0.6f, 0.02f, 1.9f));
            return M(mb, open ? "coffin_open" : "coffin", Pal.TimberDark, open ? Pal.Velvet : Pal.Iron);
        }

        public static (Mesh, Material[]) Sarcophagus()
        {
            var mb = new MeshBuilder(1) { uvScale = 0.8f };
            mb.AddRoughBox(0, new Vector3(0, 0.45f, 0), new Vector3(1.0f, 0.9f, 2.3f), 0.02f, 3);
            mb.AddBox(0, new Vector3(0, 0.95f, 0), new Vector3(1.1f, 0.12f, 2.4f));
            mb.AddEllipsoid(0, new Vector3(0, 1.1f, 0.2f), new Vector3(0.3f, 0.12f, 0.8f), 6, 8);
            mb.AddEllipsoid(0, new Vector3(0, 1.12f, -0.8f), new Vector3(0.14f, 0.12f, 0.14f), 5, 8);
            return M(mb, "sarcophagus", Pal.Ashlar);
        }

        public static (Mesh, Material[]) Gravestone(int kind, int seed)
        {
            var mb = new MeshBuilder(1) { uvScale = 1.2f };
            var rnd = new System.Random(seed);
            float tilt = (float)(rnd.NextDouble() - 0.5) * 18;
            mb.Push(); mb.Rotate(Quaternion.Euler(tilt * 0.4f, 0, tilt));
            switch (kind % 4)
            {
                case 0: mb.AddRoughBox(0, new Vector3(0, 0.45f, 0), new Vector3(0.55f, 0.9f, 0.14f), 0.02f, seed); mb.AddCylinder(0, new Vector3(0, 0.9f, -0.07f), 0.27f, 0.27f, 0.001f, 10, false); mb.Push(); mb.Translate(new Vector3(0, 0.9f, -0.07f)); mb.Rotate(Quaternion.Euler(90, 0, 0)); mb.AddCylinder(0, Vector3.zero, 0.27f, 0.27f, 0.14f, 12); mb.Pop(); break;
                case 1: mb.AddRoughBox(0, new Vector3(0, 0.55f, 0), new Vector3(0.12f, 1.1f, 0.12f), 0.01f, seed); mb.AddRoughBox(0, new Vector3(0, 0.8f, 0), new Vector3(0.6f, 0.12f, 0.12f), 0.01f, seed + 1); break;
                case 2: mb.AddRoughBox(0, new Vector3(0, 0.35f, 0), new Vector3(0.7f, 0.7f, 0.18f), 0.04f, seed); break;
                default: mb.AddRoughBox(0, new Vector3(0, 0.25f, 0), new Vector3(0.6f, 0.5f, 0.2f), 0.02f, seed); mb.AddCone(0, new Vector3(0, 0.5f, 0), 0.3f, 0.3f, 4); break;
            }
            mb.Pop();
            return M(mb, "grave" + kind + "_" + seed, Pal.Rock);
        }

        public static (Mesh, Material[]) GraveMound()
        {
            var mb = new MeshBuilder(1);
            mb.AddEllipsoid(0, new Vector3(0, 0, 0), new Vector3(0.5f, 0.22f, 1.0f), 5, 10, p => new Vector3(p.x, Mathf.Max(p.y, -0.02f), p.z));
            return M(mb, "gravemound", Pal.Dirt);
        }

        public static (Mesh, Material[]) Signpost()
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 1.3f, 0), new Vector3(0.12f, 2.6f, 0.12f));
            mb.Push(); mb.Translate(new Vector3(0.45f, 2.2f, 0)); mb.Rotate(Quaternion.Euler(0, 0, -5)); mb.AddBox(1, Vector3.zero, new Vector3(0.9f, 0.22f, 0.04f)); mb.Pop();
            mb.Push(); mb.Translate(new Vector3(-0.1f, 1.8f, 0.3f)); mb.Rotate(Quaternion.Euler(0, 80, 6)); mb.AddBox(1, Vector3.zero, new Vector3(0.8f, 0.2f, 0.04f)); mb.Pop();
            return M(mb, "signpost", Pal.TimberDark, Pal.Planks);
        }

        public static (Mesh, Material[]) NoticeBoard()
        {
            var mb = new MeshBuilder(3) { uvScale = 1f };
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * 0.7f, 1.0f, 0), new Vector3(0.12f, 2.0f, 0.12f));
            mb.AddBox(1, new Vector3(0, 1.35f, 0), new Vector3(1.4f, 0.9f, 0.05f));
            mb.AddPrism(0, new Vector3(-0.9f, 1.9f, -0.2f), new Vector3(0, 2.2f, -0.2f), new Vector3(0.9f, 1.9f, -0.2f), 0.4f);
            var rnd = new System.Random(3);
            for (int i = 0; i < 5; i++) { mb.Push(); mb.Translate(new Vector3(-0.45f + i * 0.22f, 1.35f + (float)(rnd.NextDouble() - .5) * 0.4f, 0.03f)); mb.Rotate(Quaternion.Euler(0, 0, (float)(rnd.NextDouble() - .5) * 20)); mb.AddBox(2, Vector3.zero, new Vector3(0.2f, 0.28f, 0.01f)); mb.Pop(); }
            return M(mb, "noticeboard", Pal.TimberDark, Pal.Planks, Pal.Fabric(new Color(.85f, .82f, .7f)));
        }

        public static (Mesh, Material[]) HangingCage()
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0, 2.2f, 0), new Vector3(0.14f, 4.4f, 0.14f));
            mb.AddBox(0, new Vector3(0.7f, 4.3f, 0), new Vector3(1.5f, 0.12f, 0.12f));
            mb.AddCylinder(1, new Vector3(1.3f, 2.7f, 0), 0.01f, 0.01f, 1.6f, 4, false);
            for (int i = 0; i < 10; i++) { float a = i / 10f * Mathf.PI * 2; mb.AddCylinder(1, new Vector3(1.3f + Mathf.Cos(a) * 0.4f, 1.4f, Mathf.Sin(a) * 0.4f), 0.012f, 0.012f, 1.3f, 4, false); }
            mb.AddCylinder(1, new Vector3(1.3f, 1.38f, 0), 0.42f, 0.42f, 0.04f, 10);
            mb.AddCone(1, new Vector3(1.3f, 2.7f, 0), 0.42f, 0.2f, 10);
            return M(mb, "cage", Pal.TimberDark, Pal.Rust);
        }

        public static (Mesh, Material[]) Brazier()
        {
            var mb = new MeshBuilder(2);
            for (int i = 0; i < 3; i++) { float a = i / 3f * Mathf.PI * 2; mb.Push(); mb.Translate(new Vector3(Mathf.Cos(a) * 0.2f, 0.4f, Mathf.Sin(a) * 0.2f)); mb.Rotate(Quaternion.Euler(Mathf.Sin(a) * 15, 0, -Mathf.Cos(a) * 15)); mb.AddBox(0, Vector3.zero, new Vector3(0.04f, 0.85f, 0.04f)); mb.Pop(); }
            mb.AddCylinder(0, new Vector3(0, 0.8f, 0), 0.18f, 0.35f, 0.2f, 10);
            mb.AddDisc(1, new Vector3(0, 0.98f, 0), 0.32f, 10, true);
            return M(mb, "brazier", Pal.Iron, MatLib.Emissive(new Color(.3f, .1f, .05f), new Color(2.5f, .8f, .2f)));
        }

        public static (Mesh, Material[]) BonePile(int seed)
        {
            var rnd = new System.Random(seed);
            var mb = new MeshBuilder(1);
            for (int i = 0; i < 12; i++)
            {
                mb.Push(); mb.Translate(new Vector3((float)(rnd.NextDouble() - .5) * 0.9f, 0.04f, (float)(rnd.NextDouble() - .5) * 0.9f)); mb.Rotate(Quaternion.Euler(90, (float)rnd.NextDouble() * 360, 0));
                mb.AddCylinder(0, new Vector3(0, -0.2f, 0), 0.025f, 0.025f, 0.4f, 5);
                mb.AddEllipsoid(0, new Vector3(0, -0.21f, 0), Vector3.one * 0.04f, 3, 5); mb.AddEllipsoid(0, new Vector3(0, 0.21f, 0), Vector3.one * 0.04f, 3, 5);
                mb.Pop();
            }
            mb.AddEllipsoid(0, new Vector3(0.1f, 0.1f, 0.05f), new Vector3(0.1f, 0.11f, 0.12f), 6, 8);
            return M(mb, "bones" + seed, Pal.Bone);
        }

        public static (Mesh, Material[]) Stairs(float w, float h, float len, int steps)
        {
            var mb = new MeshBuilder(1) { uvScale = 1f };
            for (int i = 0; i < steps; i++)
            {
                float y = h * (i + 1) / steps, z = len * i / steps;
                mb.AddBox(0, new Vector3(0, y / 2, z + len / steps / 2), new Vector3(w, y, len / steps));
            }
            return M(mb, $"stairs{w}{h}{len}", Pal.Planks);
        }

        public static (Mesh, Material[]) Statue(bool headless)
        {
            var mb = new MeshBuilder(1) { uvScale = 1f };
            mb.AddRoughBox(0, new Vector3(0, 0.4f, 0), new Vector3(1.0f, 0.8f, 1.0f), 0.03f, 1);
            mb.AddCylinder(0, new Vector3(0, 0.8f, 0), 0.32f, 0.3f, 1.1f, 10);
            mb.AddEllipsoid(0, new Vector3(0, 2.3f, 0), new Vector3(0.42f, 0.55f, 0.28f), 8, 10);
            if (!headless) mb.AddEllipsoid(0, new Vector3(0, 3.0f, 0), new Vector3(0.18f, 0.22f, 0.2f), 7, 9);
            foreach (var s in new[] { -1, 1 }) mb.AddCylinder(0, new Vector3(s * 0.45f, 1.5f, 0.1f), 0.1f, 0.09f, 0.95f, 6);
            return M(mb, headless ? "statue_headless" : "statue", Pal.Rock);
        }

        public static (Mesh, Material[]) Dollhouse()
        {
            var mb = new MeshBuilder(4) { uvScale = 3f };
            mb.AddBox(0, new Vector3(0, 0.35f, 0), new Vector3(0.9f, 0.7f, 0.6f));
            mb.Push(); mb.Translate(new Vector3(0, 0.7f, 0));
            mb.AddPrism(1, new Vector3(-0.5f, 0, -0.32f), new Vector3(0, 0.35f, -0.32f), new Vector3(0.5f, 0, -0.32f), 0.64f);
            mb.Pop();
            for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) mb.AddBox(2, new Vector3(-0.3f + i * 0.3f, 0.2f + j * 0.3f, 0.305f), new Vector3(0.1f, 0.12f, 0.01f));
            mb.AddBox(3, new Vector3(0, 0.1f, 0.305f), new Vector3(0.1f, 0.18f, 0.01f));
            return M(mb, "dollhouse", Pal.PlasterDark, Pal.Slate, Pal.WindowLit, Pal.TimberDark);
        }

        public static (Mesh, Material[]) Organ()
        {
            var mb = new MeshBuilder(3) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.5f, 0), new Vector3(1.8f, 1.0f, 0.8f));
            mb.AddBox(1, new Vector3(0, 0.95f, 0.3f), new Vector3(1.4f, 0.05f, 0.25f));
            for (int i = 0; i < 12; i++) { float x = -0.8f + i * 0.145f; float h = 1.2f + Mathf.Sin(i / 11f * Mathf.PI) * 1.4f; mb.AddCylinder(2, new Vector3(x, 1.0f, -0.25f), 0.05f, 0.05f, h, 8); }
            return M(mb, "organ", Pal.TimberDark, MatLib.Lit(new Color(.85f, .82f, .75f), null, .6f), Pal.Gold);
        }

        public static (Mesh, Material[]) Keg()
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0, 0.25f, 0), new Vector3(1.2f, 0.5f, 0.5f));
            for (int i = -1; i <= 1; i++) { mb.Push(); mb.Translate(new Vector3(i * 0.38f, 0.75f, 0)); mb.Rotate(Quaternion.Euler(90, 0, 0)); mb.AddCylinder(1, new Vector3(0, -0.25f, 0), 0.2f, 0.2f, 0.5f, 10); mb.Pop(); }
            return M(mb, "kegs", Pal.TimberDark, Pal.Planks);
        }

        public static (Mesh, Material[]) Crib()
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            mb.AddBox(0, new Vector3(0, 0.35f, 0), new Vector3(0.7f, 0.08f, 1.1f));
            for (int i = 0; i < 9; i++) foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(s * 0.34f, 0.65f, -0.5f + i * 0.125f), new Vector3(0.03f, 0.6f, 0.03f));
            foreach (var s in new[] { -1, 1 }) mb.AddBox(0, new Vector3(0, 0.65f, s * 0.54f), new Vector3(0.72f, 0.6f, 0.04f));
            mb.AddBox(1, new Vector3(0, 0.42f, 0), new Vector3(0.6f, 0.06f, 1.0f));
            return M(mb, "crib", Pal.Planks, Pal.Fabric(new Color(.6f, .6f, .7f)));
        }

        public static (Mesh, Material[]) Cobweb()
        {
            var mb = new MeshBuilder(1);
            mb.AddQuad(0, new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0), false);
            return M(mb, "cobweb", MatLib.Cutout(CobwebTex, new Color(.85f, .85f, .85f), 0.3f));
        }

        static Texture2D _web;
        static Texture2D CobwebTex
        {
            get
            {
                if (_web) return _web;
                int s = 128; _web = new Texture2D(s, s, TextureFormat.RGBA32, true) { name = "cobweb", wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[s * s];
                for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
                    {
                        float u = x / (float)s, v = y / (float)s;
                        float ang = Mathf.Atan2(v, u), r = Mathf.Sqrt(u * u + v * v);
                        float spokes = Mathf.Abs(Mathf.Sin(ang * 9)) < 0.04f ? 1 : 0;
                        float rings = Mathf.Abs(Mathf.Sin(r * 38 + Mathf.Sin(ang * 9) * 0.6f)) < 0.07f ? 1 : 0;
                        float a = Mathf.Clamp01((spokes + rings) * (1 - r * 0.9f));
                        px[y * s + x] = new Color(1, 1, 1, a);
                    }
                _web.SetPixels32(px); _web.Apply(true, true);
                return _web;
            }
        }

        public static (Mesh, Material[]) Haybale()
        {
            var mb = new MeshBuilder(1) { uvScale = 1.5f };
            mb.AddRoughBox(0, new Vector3(0, 0.3f, 0), new Vector3(1.0f, 0.6f, 0.6f), 0.05f, 2);
            return M(mb, "hay", Pal.Thatch);
        }

        public static (Mesh, Material[]) Woodpile()
        {
            var mb = new MeshBuilder(2);
            for (int r = 0; r < 3; r++) for (int i = 0; i < 5 - r; i++)
                { mb.Push(); mb.Translate(new Vector3(-0.5f + i * 0.24f + r * 0.12f, 0.12f + r * 0.2f, 0)); mb.Rotate(Quaternion.Euler(90, 0, 0)); mb.AddCylinder(0, new Vector3(0, -0.5f, 0), 0.11f, 0.11f, 1.0f, 7, false); mb.AddDisc(1, new Vector3(0, 0.5f, 0), 0.11f, 7, true); mb.Pop(); }
            return M(mb, "woodpile", Pal.Bark, Pal.Planks);
        }

        public static (Mesh, Material[]) MarketStall(Color cloth)
        {
            var mb = new MeshBuilder(2) { uvScale = 1f };
            foreach (var x in new[] { -1, 1 }) foreach (var z in new[] { -1, 1 }) mb.AddBox(0, new Vector3(x * 1.1f, 1.2f, z * 0.6f), new Vector3(0.08f, 2.4f, 0.08f));
            mb.AddBox(0, new Vector3(0, 0.85f, 0.2f), new Vector3(2.2f, 0.08f, 1.0f));
            mb.Push(); mb.Translate(new Vector3(0, 2.3f, 0)); mb.Rotate(Quaternion.Euler(-12, 0, 0)); mb.AddBox(1, Vector3.zero, new Vector3(2.5f, 0.04f, 1.6f)); mb.Pop();
            return M(mb, "stall" + cloth, Pal.TimberDark, Pal.Fabric(cloth));
        }
    }
}
