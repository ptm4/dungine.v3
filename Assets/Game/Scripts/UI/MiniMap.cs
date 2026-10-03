using System.Collections.Generic;
using Dungine.Combat;
using Dungine.Rules;
using Dungine.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Dungine.UI
{
    /// <summary>
    /// A round, camera-aligned minimap in the top-right corner: an orthographic top-down render of the area around
    /// the selected character, refreshed a few times a second, with markers for the party, people, enemies and exits.
    /// </summary>
    public class MiniMap
    {
        const int Res = 320;
        readonly VisualElement frame, view, markerLayer, viewCone;
        readonly Label label;
        Camera cam; RenderTexture rt;
        float renderT;
        readonly List<VisualElement> pool = new List<VisualElement>();
        int used;
        public float size = 22f;       // metres from centre to edge
        public bool visible = true;
        readonly Vector2[] cone = new Vector2[4];
        bool coneValid;

        public MiniMap(VisualElement parent)
        {
            frame = UIB.El("minimap-frame", parent);
            frame.pickingMode = PickingMode.Ignore;
            view = UIB.El("minimap-view", frame);
            view.pickingMode = PickingMode.Ignore;
            // what the camera can see, projected onto the ground: shows where the view is relative to the party
            viewCone = UIB.El("minimap-markers", view);
            viewCone.pickingMode = PickingMode.Ignore;
            viewCone.generateVisualContent += DrawViewCone;
            markerLayer = UIB.El("minimap-markers", view);
            markerLayer.pickingMode = PickingMode.Ignore;
            var ring = UIB.El("minimap-ring", frame); ring.pickingMode = PickingMode.Ignore;
            var north = UIB.Lbl("N", "minimap-n", frame); north.pickingMode = PickingMode.Ignore; north.name = "north";
            label = UIB.Lbl("", "minimap-label", frame); label.pickingMode = PickingMode.Ignore;
            frame.RegisterCallback<WheelEvent>(e => { size = Mathf.Clamp(size * (e.delta.y > 0 ? 1.15f : 0.87f), 8f, 60f); e.StopPropagation(); });
        }

        void EnsureCamera()
        {
            if (cam) return;
            rt = new RenderTexture(Res, Res, 16, RenderTextureFormat.ARGB32) { name = "MiniMapRT", antiAliasing = 2 };
            var go = new GameObject("MiniMapCamera");
            Object.DontDestroyOnLoad(go);
            cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.035f, 1f);
            cam.targetTexture = rt;
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 200f;
            cam.cullingMask = Layers.Mask(Layers.Default, Layers.Ground, Layers.Walls, Layers.Interact);
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.antialiasing = AntialiasingMode.None;
            view.style.backgroundImage = Background.FromRenderTexture(rt);
        }

        public void Tick()
        {
            var g = Game.I;
            bool show = visible && g != null && g.area != null && (g.mode == GameMode.Explore || g.mode == GameMode.Combat) && g.Selected;
            frame.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            EnsureCamera();
            var rig = CameraRig.I;
            Vector3 centre = g.Selected.transform.position;
            float yaw = rig ? rig.yaw : 0f;
            float s = g.area.def.Interior ? Mathf.Min(size, 13f) : size;
            cam.orthographicSize = s;
            cam.transform.position = centre + Vector3.up * 80f;
            cam.transform.rotation = Quaternion.Euler(90f, yaw, 0f);
            renderT -= Time.unscaledDeltaTime;
            if (renderT <= 0)
            {
                renderT = 0.12f;
                // interiors: hide the ceiling-height clutter by clipping just above head height
                cam.nearClipPlane = g.area.def.Interior ? 77.4f : 0.3f;
                cam.farClipPlane = g.area.def.Interior ? 83f : 200f;
                bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
                cam.Render();
                RenderSettings.fog = fogWas;
            }
            label.text = g.area.def.Title;
            var north = frame.Q<Label>("north");
            if (north != null)
            {
                // north marker orbits the rim as the camera turns
                float a = -yaw * Mathf.Deg2Rad;
                north.style.left = 100 + Mathf.Sin(a) * 92 - 7; north.style.top = 100 - Mathf.Cos(a) * 92 - 10;
            }
            UpdateCone(centre, yaw, s);
            UpdateMarkers(centre, yaw, s);
        }

        Vector2 ToMap(Vector3 world, Vector3 centre, float yaw, float s)
        {
            Vector3 d = world - centre; d.y = 0;
            d = Quaternion.Euler(0, -yaw, 0) * d;
            Vector2 p = new Vector2(d.x, d.z) / s;
            if (p.magnitude > 3f) p = p.normalized * 3f;   // far corners only need to reach past the rim
            return new Vector2(100f + p.x * 100f, 100f - p.y * 100f);
        }

        void UpdateCone(Vector3 centre, float yaw, float s)
        {
            var cam = CameraRig.I ? CameraRig.I.cam : null;
            coneValid = false;
            if (cam)
            {
                var ground = new Plane(Vector3.up, new Vector3(0, centre.y, 0));
                Vector2[] vp = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                coneValid = true;
                for (int i = 0; i < 4; i++)
                {
                    var ray = cam.ViewportPointToRay(new Vector3(vp[i].x, vp[i].y, 0));
                    Vector3 hit;
                    if (ground.Raycast(ray, out float t) && t < 400f) hit = ray.GetPoint(t);
                    else { Vector3 flat = ray.direction; flat.y = 0; hit = cam.transform.position + flat.normalized * 400f; }
                    cone[i] = ToMap(hit, centre, yaw, s);
                }
            }
            viewCone.MarkDirtyRepaint();
        }

        void DrawViewCone(MeshGenerationContext mgc)
        {
            if (!coneValid) return;
            var p = mgc.painter2D;
            p.fillColor = new Color(1f, 0.92f, 0.7f, 0.13f);
            p.strokeColor = new Color(1f, 0.86f, 0.55f, 0.75f);
            p.lineWidth = 1.5f;
            p.lineJoin = LineJoin.Round;
            p.BeginPath();
            p.MoveTo(cone[0]);
            for (int i = 1; i < 4; i++) p.LineTo(cone[i]);
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }

        VisualElement Marker(string cls)
        {
            VisualElement m;
            if (used < pool.Count) m = pool[used];
            else { m = new VisualElement(); m.pickingMode = PickingMode.Ignore; markerLayer.Add(m); pool.Add(m); }
            used++;
            m.ClearClassList(); m.AddToClassList("mm-marker"); m.AddToClassList(cls);
            m.style.display = DisplayStyle.Flex;
            return m;
        }

        void Place(VisualElement m, Vector3 world, Vector3 centre, float yaw, float s, bool clampToRim)
        {
            Vector3 d = world - centre; d.y = 0;
            d = Quaternion.Euler(0, -yaw, 0) * d;
            Vector2 p = new Vector2(d.x, d.z) / s;           // -1..1
            if (p.magnitude > 0.94f) { if (!clampToRim) { m.style.display = DisplayStyle.None; return; } p = p.normalized * 0.94f; }
            float half = 100f;
            m.style.left = half + p.x * half - 5; m.style.top = half - p.y * half - 5;
        }

        void UpdateMarkers(Vector3 centre, float yaw, float s)
        {
            used = 0;
            var g = Game.I;
            foreach (var it in g.area.interactables)
            {
                if (!it || !it.Available) continue;
                if (it is Transition) Place(Marker("mm-exit"), it.transform.position, centre, yaw, s, true);
            }
            foreach (var n in g.area.npcs)
            {
                if (!n || !n.gameObject.activeInHierarchy || n.c == null || !n.c.Active) continue;
                string cls = n.c.faction == Faction.Hostile ? "mm-hostile" : n.talkable ? "mm-talk" : "mm-npc";
                if (n.c.faction == Faction.Hostile && !CombatManager.I.Active && Vector3.Distance(n.transform.position, centre) > 14f) continue; // unseen enemies stay hidden
                Place(Marker(cls), n.transform.position, centre, yaw, s, false);
            }
            if (CameraRig.I && !CameraRig.I.follow)
                Place(Marker("mm-camera"), CameraRig.I.target, centre, yaw, s, true);
            foreach (var a in g.PartyActors)
            {
                var m = Marker(a == g.Selected ? "mm-selected" : "mm-party");
                Place(m, a.transform.position, centre, yaw, s, true);
            }
            for (int i = used; i < pool.Count; i++) pool[i].style.display = DisplayStyle.None;
        }
    }
}
