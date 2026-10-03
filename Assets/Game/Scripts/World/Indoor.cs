using System.Collections.Generic;
using Dungine.Visual;
using UnityEngine;

namespace Dungine.World
{
    /// <summary>Helpers for building interior areas: rooms with cutaway walls, warm pools of light, furniture.</summary>
    public class Indoor
    {
        public readonly AreaContext ctx;
        public Transform Root => ctx.root;
        public float height = 3.2f;
        public Material wallIn = Pal.Plaster, wallOut = Pal.PlasterDark, floor = Pal.FloorBoards;

        public Indoor(AreaContext c) { ctx = c; }

        /// <summary>A room centred at (x, z) with doorways and windows as Openings.</summary>
        public void Room(float x, float z, float w, float d, params Opening[] openings)
            => Interiors.Room(Root, ctx.cutaway, new Vector3(x, 0, z), new Vector2(w, d), height, wallIn, wallOut, floor, openings);

        public void RoomMats(float x, float z, float w, float d, Material wIn, Material fl, params Opening[] openings)
            => Interiors.Room(Root, ctx.cutaway, new Vector3(x, 0, z), new Vector2(w, d), height, wIn, wallOut, fl, openings);

        /// <summary>Ceiling beams across a room, purely decorative (they don't block the camera).</summary>
        public void Beams(float x, float z, float w, float d, float spacing = 2.2f)
        {
            for (float bx = x - w / 2 + spacing / 2; bx < x + w / 2; bx += spacing)
                Interiors.Beam(Root, new Vector3(bx, height - 0.1f, z - d / 2), new Vector3(bx, height - 0.1f, z + d / 2), 0.22f, Pal.TimberDark);
        }

        public GameObject Prop(string key, System.Func<(Mesh, Material[])> make, float x, float z, float yaw = 0, float scale = 1, Kit.ColliderKind col = Kit.ColliderKind.Box, float y = 0)
            => ctx.Prop(key, make, new Vector3(x, y, z), yaw, scale, col);

        /// <summary>A candle flame plus a small warm light.</summary>
        public void Candle(float x, float y, float z, float intensity = 1.8f, float range = 5f)
        {
            Kit.Flame(Root, new Vector3(x, y, z), 0.035f);
            Kit.PointLight(Root, new Vector3(x, y + 0.15f, z), new Color(1f, .68f, .38f), intensity, range, false);
        }

        public void Candelabra(float x, float z, float yaw = 0)
        {
            Prop("candelabra_f", () => Props.Candelabra(true), x, z, yaw, 1, Kit.ColliderKind.Capsule);
            for (int i = 0; i < 3; i++) Kit.Flame(Root, new Vector3(x + (i - 1) * 0.2f, 1.62f + (i == 1 ? 0.08f : 0), z), 0.03f);
            Kit.PointLight(Root, new Vector3(x, 1.9f, z), new Color(1f, .66f, .36f), 1.8f, 6f, true);
        }

        public void Fireplace(float x, float z, float yaw)
        {
            Prop("fireplace", Props.Fireplace, x, z, yaw);
            var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            var fire = new Vector3(x, 0.35f, z) + fwd * 0.25f;
            var ps = FX.MakePS("hearth", fire, ProcTex.FogPuff, true, 0, 0.7f, 0.6f, 0.35f, new Color(1f, .55f, .2f, .9f), new Color(.5f, .1f, 0, 0), -0.6f, 0.25f, ParticleSystemShapeType.Box, 5f, true, 30f);
            ps.transform.SetParent(Root, true);
            var sh = ps.shape; sh.scale = new Vector3(0.8f, 0.1f, 0.3f);
            Kit.PointLight(Root, fire + fwd * 0.6f + Vector3.up * 0.4f, new Color(1f, .55f, .25f), 5f, 10f, true);
        }

        /// <summary>An iron lantern bracketed to a wall, facing into the room.</summary>
        public void Sconce(float x, float z, float yaw, float y = 2.1f)
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(0, 0, 0.12f), new Vector3(0.04f, 0.04f, 0.24f));
            mb.AddBox(0, new Vector3(0, -0.05f, 0.25f), new Vector3(0.16f, 0.02f, 0.16f));
            mb.AddCone(0, new Vector3(0, 0.17f, 0.25f), 0.12f, 0.12f, 4);
            mb.AddBox(1, new Vector3(0, 0.06f, 0.25f), new Vector3(0.12f, 0.2f, 0.12f));
            var go = Kit.FromBuilder(mb, new[] { Pal.Iron, Pal.WindowLit }, Root, "Sconce");
            go.transform.position = new Vector3(x, y, z); go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Kit.PointLight(Root, go.transform.TransformPoint(new Vector3(0, 0.06f, 0.5f)), new Color(1f, .7f, .42f), 2.2f, 7f, false);
        }

        /// <summary>A dim fill light so rooms never go fully black away from candles.</summary>
        public void Fill(float x, float z, Color c, float intensity = 0.8f, float range = 12f)
            => Kit.PointLight(Root, new Vector3(x, height + 1.5f, z), c, intensity * 2.4f, range * 1.3f, false, false);

        /// <summary>A door that leads out of the interior.</summary>
        public Transition Exit(string id, string label, float x, float z, float yaw, string area, string spawn, float width = 1.4f)
            => ctx.Door(id, label, new Vector3(x, 0, z), yaw, area, spawn, new Vector3(width, 2.3f, 0.5f));

        /// <summary>A plain door leaf standing in an opening (visual only).</summary>
        public void DoorLeaf(float x, float z, float yaw, float width = 1.2f, bool open = false)
        {
            var mb = new MeshBuilder(2);
            mb.AddBox(0, new Vector3(width / 2, 1.1f, 0), new Vector3(width, 2.2f, 0.07f));
            for (int i = 0; i < 3; i++) mb.AddBox(1, new Vector3(width / 2, 0.35f + i * 0.75f, 0.05f), new Vector3(width * 0.9f, 0.06f, 0.02f));
            var go = Kit.FromBuilder(mb, new[] { Pal.Planks, Pal.Iron }, Root, "DoorLeaf");
            go.transform.position = new Vector3(x, 0, z) - Quaternion.Euler(0, yaw, 0) * new Vector3(width / 2, 0, 0);
            go.transform.rotation = Quaternion.Euler(0, yaw + (open ? -100 : 0), 0);
        }

        /// <summary>Blackness outside the building: a big dark ground plane so the void has a floor.</summary>
        public void Void()
        {
            var mb = new MeshBuilder(1);
            mb.AddBox(0, new Vector3(0, -0.4f, 0), new Vector3(200, 0.2f, 200), 4);
            Kit.FromBuilder(mb, new[] { Pal.Black }, Root, "Void");
        }
    }
}
