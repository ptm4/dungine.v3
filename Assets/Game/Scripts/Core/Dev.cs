using System.Linq;
using System.Collections.Generic;
using System.Text;
using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Dungine
{
    /// <summary>Developer entry points callable from the Unity CLI (eval) for visual checks.</summary>
    public static class Dev
    {
        public static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (!cam)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.fieldOfView = 35;
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 600f;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
            return cam;
        }

        public static string Lineup(int seed = 1, bool combat = false)
        {
            var old = GameObject.Find("DevLineup");
            if (old) Object.DestroyImmediate(old);
            Dungine.Visual.Atmosphere.Ensure().Apply(Dungine.Visual.Atmosphere.BaroviaDay);
            var root = new GameObject("DevLineup");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(root.transform);
            ground.transform.localScale = new Vector3(6, 1, 3);
            ground.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(.8f, .8f, .8f), TexId.Cobble, .35f, 0, 12f);
            var rnd = new System.Random(seed);
            var races = (RaceId[])System.Enum.GetValues(typeof(RaceId));
            var sb = new StringBuilder();
            ArmorVisual[] armors = { ArmorVisual.Plate, ArmorVisual.Robe, ArmorVisual.Leather, ArmorVisual.Chain, ArmorVisual.Hide, ArmorVisual.Cloth, ArmorVisual.Robe, ArmorVisual.Leather, ArmorVisual.Plate, ArmorVisual.Scale, ArmorVisual.Chain };
            WeaponVisual[] weps = { WeaponVisual.Longsword, WeaponVisual.Staff, WeaponVisual.Shortbow, WeaponVisual.Mace, WeaponVisual.Greataxe, WeaponVisual.Rapier, WeaponVisual.Quarterstaff, WeaponVisual.Dagger, WeaponVisual.Greatsword, WeaponVisual.Warhammer, WeaponVisual.Battleaxe };
            for (int i = 0; i < races.Length; i++)
            {
                var r = races[i];
                SubraceId sub = SubraceId.None;
                if (r == RaceId.Dragonborn) sub = SubraceId.Red;
                if (r == RaceId.Tiefling) sub = SubraceId.Asmodeus;
                var a = RaceLooks.Randomize(r, sub, i % 2, rnd);
                a.cloth1 = Color.HSVToRGB((float)rnd.NextDouble(), .5f, .45f);
                a.cloth2 = Color.HSVToRGB((float)rnd.NextDouble(), .3f, .25f);
                if (i == 3) a.cape = true;
                var g = new GearLook { armor = armors[i], main = weps[i], off = i == 3 || i == 0 ? WeaponVisual.Shield : WeaponVisual.None, drawn = combat };
                var rig = HumanoidBuilder.Build(a, g, r.ToString());
                rig.transform.SetParent(root.transform);
                rig.transform.position = new Vector3((i - (races.Length - 1) / 2f) * 1.05f, 0, 0);
                rig.transform.rotation = Quaternion.identity;
                var anim = rig.gameObject.AddComponent<HumanoidAnimator>();
                anim.Init(rig, MotionStyle.Normal);
                if (combat) anim.SetCombat(true);
                sb.AppendLine($"{r}: h={rig.height:F2} verts={rig.body.sharedMesh.vertexCount}");
            }
            var cam = EnsureCamera();
            cam.transform.position = new Vector3(0, 1.7f, -8.5f);
            cam.transform.LookAt(new Vector3(0, 0.95f, 0));
            cam.transform.position = new Vector3(0, 1.7f, 8.5f);
            cam.transform.LookAt(new Vector3(0, 0.95f, 0));
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.transform.SetParent(root.transform);
            fill.type = LightType.Point; fill.range = 12; fill.intensity = 3; fill.color = new Color(1, .8f, .6f);
            fill.transform.position = new Vector3(-3, 2.5f, 4);
            return sb.ToString();
        }

        /// <summary>Lists the first labels in the runtime UI with their resolved style, for diagnosing text rendering.</summary>
        public static string ProbeUI(int max = 12)
        {
            var doc = Object.FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
            var sb = new StringBuilder();
            sb.Append("body=" + (Dungine.UI.Theme.Body != null) + " ");
            if (doc == null) return sb + "nodoc";
            var labels = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.Label>(doc.rootVisualElement).ToList();
            sb.Append("labels=" + labels.Count + "\n");
            int i = 0;
            foreach (var l in labels)
            {
                if (i++ >= max) break;
                var rs = l.resolvedStyle;
                var fd = rs.unityFontDefinition;
                sb.Append($"[{l.text}] col={rs.color} fs={rs.fontSize} fa={(fd.fontAsset ? fd.fontAsset.name : "null")} f={(rs.unityFont ? rs.unityFont.name : "null")} {rs.width}x{rs.height} op={rs.opacity} disp={rs.display}\n");
            }
            return sb.ToString();
        }

        public static string FontExperiment(int mode)
        {
            var doc = Object.FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
            var all = UnityEngine.UIElements.UQueryExtensions.Query<UnityEngine.UIElements.VisualElement>(doc.rootVisualElement).ToList();
            UnityEngine.UIElements.StyleFontDefinition def;
            string info = "";
            if (mode == 0) def = new UnityEngine.UIElements.StyleFontDefinition(UnityEngine.UIElements.StyleKeyword.Null);
            else if (mode == 1)
            {
                var f = Font.CreateDynamicFontFromOSFont("Palatino Linotype", 48);
                info = "font=" + (f != null) + " ";
                def = new UnityEngine.UIElements.StyleFontDefinition(UnityEngine.UIElements.FontDefinition.FromFont(f));
            }
            else
            {
                var f = Font.CreateDynamicFontFromOSFont("Palatino Linotype", 48);
                var fa = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(f);
                info = "fa=" + (fa != null) + " src=" + (fa != null && fa.sourceFontFile != null) + " ";
                def = new UnityEngine.UIElements.StyleFontDefinition(UnityEngine.UIElements.FontDefinition.FromSDFFont(fa));
            }
            foreach (var e in all) e.style.unityFontDefinition = def;
            return info + "n=" + all.Count;
        }

        /// <summary>Writes a (possibly non-readable) texture to Assets/Captures/&lt;name&gt;.png, alpha shown over magenta.</summary>
        public static string DumpTex(Texture tex, string name)
        {
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(tex, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            var px = t.GetPixels();
            float amin = 1, amax = 0;
            for (int i = 0; i < px.Length; i++) { amin = Mathf.Min(amin, px[i].a); amax = Mathf.Max(amax, px[i].a); px[i] = Color.Lerp(Color.magenta, new Color(px[i].r, px[i].g, px[i].b, 1), px[i].a); }
            t.SetPixels(px); t.Apply();
            System.IO.File.WriteAllBytes(Application.dataPath + "/Captures/" + name + ".png", t.EncodeToPNG());
            return $"alpha {amin:F2}..{amax:F2}";
        }

        /// <summary>Points the cinematic camera at the renderer whose name starts with prefix that is nearest the selected character.</summary>
        public static string Peek(string prefix, float dist = 2.5f, float height = 1.2f)
        {
            var sel = Game.I.Selected; Vector3 from = sel ? sel.transform.position : CameraRig.I.cam.transform.position;
            Renderer best = null; float bd = 1e9f;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
            {
                if (!r.name.StartsWith(prefix)) continue;
                float d = (r.bounds.center - from).sqrMagnitude;
                if (d < bd) { bd = d; best = r; }
            }
            if (!best) return "none";
            var c = best.bounds.center;
            CameraRig.I.SetCinematic(c + new Vector3(dist * 0.7f, height, -dist * 0.7f), c, 40f);
            var mats = string.Join(",", System.Array.ConvertAll(best.sharedMaterials, m => m ? m.name : "null"));
            return best.name + " @" + c + " size=" + best.bounds.size + " mats=" + mats + " parent=" + (best.transform.parent ? best.transform.parent.name : "-");
        }

        /// <summary>Teleports the party next to an actor (by area id) — handy for testing encounters.</summary>
        public static string PartyTo(string actorId, float dist = 7f)
        {
            var a = Game.I.FindActor(actorId); if (!a) return "no actor " + actorId;
            var basePos = a.transform.position + (Game.I.Selected.transform.position - a.transform.position).normalized * dist;
            int i = 0;
            foreach (var p in Game.I.PartyActors)
            {
                var pos = basePos + new Vector3((i - 1) * 1.2f, 0, 0);
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var h, 4f, UnityEngine.AI.NavMesh.AllAreas)) p.Warp(h.position);
                i++;
            }
            CameraRig.I.SnapTo(Game.I.Selected.transform.position, CameraRig.I.yaw);
            return "ok";
        }

        public static string PartyAt(float x, float z)
        {
            int i = 0;
            foreach (var p in Game.I.PartyActors)
            {
                var pos = new Vector3(x + (i - 1) * 0.9f, Game.I.area.GroundY(x, z, p.transform.position.y), z);
                if (UnityEngine.AI.NavMesh.SamplePosition(pos, out var h, 3f, UnityEngine.AI.NavMesh.AllAreas)) p.Warp(h.position);
                i++;
            }
            CameraRig.I.SnapTo(Game.I.Selected.transform.position, CameraRig.I.yaw);
            return "ok";
        }

        /// <summary>Spawns the given monsters in a row in front of the party (peaceful) and frames them.</summary>
        public static string MonsterLineup(string ids, float spacing = 2.2f)
        {
            var ctx = Game.I.area; var sel = Game.I.Selected;
            var list = ids.Split(',');
            Vector3 origin = sel.transform.position + Vector3.forward * 5f;
            for (int i = 0; i < list.Length; i++)
            {
                var p = origin + Vector3.right * (i - (list.Length - 1) / 2f) * spacing;
                p.y = ctx.GroundY(p.x, p.z, p.y);
                var a = ctx.Monster("lineup" + i, list[i], p, 180, null, false, 0f);
                if (a) { a.c.faction = Rules.Faction.Neutral; if (a.agent) a.agent.enabled = false; }
            }
            var mid = origin + Vector3.up * 1f;
            CameraRig.I.SetCinematic(mid + new Vector3(0, 2.2f, -list.Length * spacing * 0.75f - 2f), mid, 40f);
            return "ok";
        }

        public static string CombatState()
        {
            var cm = Combat.CombatManager.I;
            var sb = new StringBuilder();
            sb.AppendLine($"mode={Game.I.mode} active={cm.Active} round={cm.Round} current={cm.Current?.name}");
            foreach (var c in cm.All) sb.AppendLine($"  {c.name} hp {c.hp}/{c.MaxHPTotal} {(c.dead ? "DEAD" : "")} [{string.Join(",", c.conds.Select(x => x.id))}]");
            foreach (var c in Game.I.party) sb.AppendLine($"  PC {c.name} L{c.sheet.level} xp {c.sheet.xp} hp {c.hp}/{c.MaxHPTotal}");
            return sb.ToString();
        }

        public static string TextTest()
        {
            var doc = Object.FindAnyObjectByType<UnityEngine.UIElements.UIDocument>();
            var a = new UnityEngine.UIElements.Label("PLAIN DEFAULT TEXT");
            a.style.position = UnityEngine.UIElements.Position.Absolute; a.style.left = 40; a.style.top = 40; a.style.fontSize = 40; a.style.color = Color.red;
            doc.rootVisualElement.Add(a);
            var b = new UnityEngine.UIElements.Label("PALATINO TEXT");
            b.style.position = UnityEngine.UIElements.Position.Absolute; b.style.left = 40; b.style.top = 100; b.style.fontSize = 40; b.style.color = Color.green;
            Dungine.UI.Theme.ApplyFont(b);
            doc.rootVisualElement.Add(b);
            return "added";
        }

        /// <summary>
        /// Contact sheet of heads: one column per hairstyle (or per premade when race &lt; 0), rows from the front,
        /// three-quarter, side and the gameplay camera's high angle. Saved to Assets/Captures/&lt;name&gt;.png.
        /// </summary>
        public static string HeadSheet(string name, int race = -1, int body = 0, int beard = 0, float turn = 0, float nod = 0, int cell = 200)
        {
            var looks = new List<Appearance>();
            if (race < 0) foreach (var s in UI.CreationScreen.Premade()) looks.Add(s.look);
            else
                for (int st = 0; st < RaceLooks.HairStyleNames.Length; st++)
                {
                    var a = RaceLooks.Randomize((RaceId)race, RulesData.Races[(RaceId)race].subraces.FirstOrDefault(), body, new System.Random(7));
                    a.hairStyle = st; a.beardStyle = beard; looks.Add(a);
                }
            var origin = new Vector3(0, 900, 0);
            var rootGo = new GameObject("HeadSheet");
            var lightGo = new GameObject("key"); lightGo.transform.SetParent(rootGo.transform);
            var key = lightGo.AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.6f; key.transform.rotation = Quaternion.Euler(35, 150, 0);
            var rigs = new List<HumanoidRig>();
            for (int i = 0; i < looks.Count; i++)
            {
                var rig = HumanoidBuilder.Build(looks[i], new GearLook(), "Head" + i, 256);
                rig.transform.SetParent(rootGo.transform, false);
                rig.transform.position = origin + new Vector3(i * 6f, 0, 0);
                // the look-at split used by HumanoidAnimator: 60% head, 40% neck
                rig[B.Head].localRotation = Quaternion.Euler(nod * 0.6f, turn * 0.6f, 0) * rig[B.Head].localRotation;
                rig[B.Neck].localRotation = Quaternion.Euler(nod * 0.4f, turn * 0.4f, 0) * rig[B.Neck].localRotation;
                rigs.Add(rig);
            }
            float[] yaws = { 0, 40, 90, 20, 140 };
            float[] pitches = { 0, 5, 0, 55, 40 };
            var camGo = new GameObject("sheetcam"); camGo.transform.SetParent(rootGo.transform);
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 20; cam.nearClipPlane = 0.02f; cam.farClipPlane = 20;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.16f, .16f, .18f);
            var rt = new RenderTexture(cell, cell, 24, RenderTextureFormat.ARGB32); cam.targetTexture = rt;
            var sheet = new Texture2D(cell * rigs.Count, cell * yaws.Length, TextureFormat.RGB24, false);
            bool fog = RenderSettings.fog; RenderSettings.fog = false;
            for (int i = 0; i < rigs.Count; i++)
                for (int k = 0; k < yaws.Length; k++)
                {
                    var r = rigs[i];
                    Vector3 target = r.headTop.position - Vector3.up * r.headSize * 0.6f;
                    var rot = r.transform.rotation * Quaternion.Euler(pitches[k], 180 + yaws[k], 0);
                    cam.transform.position = target - rot * Vector3.forward * (r.headSize * 5.2f);
                    cam.transform.LookAt(target);
                    cam.Render();
                    RenderTexture.active = rt;
                    sheet.ReadPixels(new Rect(0, 0, cell, cell), i * cell, (yaws.Length - 1 - k) * cell);
                    RenderTexture.active = null;
                }
            RenderSettings.fog = fog;
            sheet.Apply();
            System.IO.File.WriteAllBytes(Application.dataPath + "/Captures/" + name + ".png", sheet.EncodeToPNG());
            Object.DestroyImmediate(rootGo); rt.Release();
            return $"{rigs.Count} heads";
        }

        /// <summary>Feeds a synthetic mouse drag (screen pixels, origin bottom-left) through the Input System over a few frames.</summary>
        public static string SimDrag(float x0, float y0, float x1, float y1, int frames = 20, int button = 0)
        {
            Game.I.StartCoroutine(SimDragCo(new Vector2(x0, y0), new Vector2(x1, y1), frames, button));
            return "dragging";
        }

        static System.Collections.IEnumerator SimDragCo(Vector2 a, Vector2 b, int frames, int button)
        {
            var m = Mouse.current; if (m == null) yield break;
            Vector2 prev = a;
            InputSystem.QueueStateEvent(m, new MouseState { position = a });
            yield return null;
            for (int i = 0; i <= frames; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)frames);
                InputSystem.QueueStateEvent(m, new MouseState { position = p, delta = p - prev, buttons = (ushort)(1 << button) });
                prev = p;
                yield return null;
            }
            InputSystem.QueueStateEvent(m, new MouseState { position = b });
        }

        public static string Closeup(int index, float dist = 1.4f, float height = -1f)
        {
            var root = GameObject.Find("DevLineup");
            if (!root) return "no lineup";
            var rigs = root.GetComponentsInChildren<HumanoidRig>();
            if (index < 0 || index >= rigs.Length) return "bad index";
            var r = rigs[index];
            var cam = EnsureCamera();
            float h = height > 0 ? height : r.height * 0.85f;
            Vector3 target = r.transform.position + Vector3.up * h;
            cam.transform.position = target + r.transform.forward * dist + Vector3.up * 0.05f + r.transform.right * 0.25f;
            cam.transform.LookAt(target);
            return r.name;
        }
    }
}
