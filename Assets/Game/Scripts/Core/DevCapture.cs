using System;
using System.Collections;
using System.IO;
using System.Linq;
using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dungine
{
    /// <summary>
    /// Developer tool: puts one character or creature in an isolated studio, steps its animator frame by frame and writes
    /// PNG frames to &lt;project&gt;/DevCaptures/&lt;name&gt;/ for turntables and animation reviews (tools/gif.py turns them into GIFs).
    /// Subjects: a premade's name (arkus, chairn, dulandir) or a monster id (wolf, zombie...).
    /// Actions: idle, turn, combat, walk, jog, run, hit, die, or an AnimAct name (slash, overhead, thrust, castpoint...).
    /// Views: side, front, three (front three-quarter), back, top (the game camera's angle).
    /// </summary>
    public class DevCapture : MonoBehaviour
    {
        static readonly Vector3 Origin = new Vector3(4000, 0, 4000);

        public static string Run(string subject, string action, string name, string view = "side", int frames = 48, float fps = 24, int size = 420, float zoom = 1f)
        {
            if (FindAnyObjectByType<DevCapture>()) return "busy";
            var go = new GameObject("DevCapture");
            var dc = go.AddComponent<DevCapture>();
            dc.StartCoroutine(dc.Go(subject.ToLowerInvariant(), action.ToLowerInvariant(), name, view, frames, fps, size, zoom));
            return "started " + name;
        }

        public static string OutDir(string name) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "DevCaptures", name));

        IEnumerator Go(string subject, string action, string name, string view, int frames, float fps, int size, float zoom)
        {
            yield return null;   // return to the caller first: building a character can take longer than an eval may block
            string dir = OutDir(name);
            if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir)) File.Delete(f);
            Directory.CreateDirectory(dir);
            var studio = new GameObject("CaptureStudio").transform;
            studio.position = Origin;
            string info = "";
            try
            {
                // ground with a pattern, so sliding feet show
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.SetParent(studio, false);
                ground.transform.localScale = new Vector3(12, 1, 12);
                ground.GetComponent<Renderer>().sharedMaterial = MatLib.Lit(new Color(.62f, .6f, .56f), TexId.Cobble, .2f, 0, 40f);
                ground.layer = Layers.Ground;
                var key = new GameObject("Key").AddComponent<Light>(); key.transform.SetParent(studio, false);
                key.type = LightType.Directional; key.intensity = 1.5f; key.color = new Color(1f, .95f, .88f); key.shadows = LightShadows.Soft;
                key.transform.rotation = Quaternion.Euler(42, -35, 0);
                var fill = new GameObject("Fill").AddComponent<Light>(); fill.transform.SetParent(studio, false);
                fill.type = LightType.Directional; fill.intensity = 0.45f; fill.color = new Color(.7f, .78f, 1f); fill.shadows = LightShadows.None;
                fill.transform.rotation = Quaternion.Euler(25, 150, 0);
            }
            catch (Exception e) { info += "studio: " + e.Message; }

            // ---------- subject ----------
            GameObject body = null; HumanoidAnimator ha = null; BeastAnimator qa = null; IAnimDriver drv = null;
            float height = 1.8f, length = 0.6f;
            bool quad = false;
            Library.LibraryRigDriver ld = null;
            var sheet = subject.StartsWith("lib:") ? null : UI.CreationScreen.Premade().FirstOrDefault(s => s.name.ToLowerInvariant() == subject);
            if (subject.StartsWith("lib:"))
            {
                // v3: a rigged library figure (Library/LibraryFigures.cs), driven by v2's own animator
                var rig = Library.LibraryFigures.Build(subject.Substring(4), new GearLook(), subject.Substring(4));
                if (rig == null) { File.WriteAllText(Path.Combine(dir, "done.txt"), "unknown library figure " + subject); Destroy(studio.gameObject); Destroy(gameObject); yield break; }
                body = rig.gameObject; height = rig.height;
                ha = body.AddComponent<HumanoidAnimator>(); ha.Init(rig, MotionStyle.Normal); ha.manual = true; drv = ha;
                ld = body.GetComponent<Library.LibraryRigDriver>(); ld.manual = true;
            }
            else if (sheet != null)
            {
                var c = Creature.FromSheet(sheet);
                foreach (var id in sheet.StartingItems) Game.I.AutoEquip(c, new ItemStack(id));
                var rig = HumanoidBuilder.Build(sheet.look, ActorFactory.GearFor(c), sheet.name, 512);
                body = rig.gameObject; height = rig.height;
                ha = body.AddComponent<HumanoidAnimator>(); ha.Init(rig, MotionStyle.Normal); ha.manual = true; drv = ha;
            }
            else
            {
                var m = Monsters.Get(subject);
                if (m == null) { File.WriteAllText(Path.Combine(dir, "done.txt"), "unknown subject " + subject); Destroy(studio.gameObject); Destroy(gameObject); yield break; }
                if (m.body == BodyKind.Humanoid)
                {
                    var rig = HumanoidBuilder.Build(m.look ?? new Appearance(), m.gear ?? new GearLook(), m.name, 256);
                    body = rig.gameObject; height = rig.height;
                    ha = body.AddComponent<HumanoidAnimator>(); ha.Init(rig, m.motion); ha.manual = true; drv = ha;
                }
                else
                {
                    var q = BeastBuilder.Build(m.body, m.tint, m.scale, m.name);
                    body = q.gameObject; height = q.height; length = q.length; quad = true;
                    qa = body.AddComponent<BeastAnimator>(); qa.Init(q); qa.manual = true; drv = qa;
                }
            }
            var root = body.transform;
            root.SetParent(studio, false);
            root.position = Origin;
            root.rotation = Quaternion.identity;
            foreach (var t in body.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layers.Default;
            Action<float> tick = dt => { if (ha) ha.Tick(dt); if (ld) ld.Tick(dt); if (qa) qa.Tick(dt); };

            // ---------- action ----------
            float speed = 0; float spin = 0; AnimAct act = AnimAct.None; LifeState life = LifeState.Alive;
            bool combat = false;
            switch (action)
            {
                case "idle": break;
                case "turn": spin = 360f; break;
                case "combat": combat = true; break;
                case "walk": speed = quad ? 1.5f : 1.5f; break;
                case "jog": speed = quad ? 3.6f : 3.0f; break;
                case "run": speed = quad ? 7.5f : 4.2f; break;
                case "die": life = LifeState.Dead; break;
                default:
                    combat = true;
                    act = Enum.GetValues(typeof(AnimAct)).Cast<AnimAct>().FirstOrDefault(a => a.ToString().ToLowerInvariant() == action);
                    if (act == AnimAct.None) info += " unknown action " + action;
                    break;
            }

            // pre-roll: settle stances and let a gait reach its rhythm
            float dt0 = 1f / fps;
            if (combat) drv.SetCombat(true);
            for (int i = 0; i < (int)(fps * 1.5f); i++)
            {
                root.position += root.forward * speed * dt0;
                tick(dt0);
            }
            if (act != AnimAct.None) { drv.Play(act); frames = Mathf.CeilToInt((Anim.Duration(act) + 0.35f) * fps); }
            if (life != LifeState.Alive) { drv.SetLife(life); frames = Mathf.CeilToInt(2f * fps); }

            // ---------- camera ----------
            var camGo = new GameObject("CaptureCam"); camGo.transform.SetParent(studio, false);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 30; cam.nearClipPlane = 0.05f; cam.farClipPlane = 200;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.13f, .13f, .15f);
            var cd = cam.GetUniversalAdditionalCameraData();
            cd.renderPostProcessing = false; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cd.renderShadows = true;
            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt; cam.enabled = false;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            // frame what is actually there (tail, muzzle and weapon included)
            var bb = new Bounds(root.position + Vector3.up * height * 0.5f, Vector3.one * 0.1f);
            foreach (var r in body.GetComponentsInChildren<Renderer>()) if (r.enabled) bb.Encapsulate(r.bounds);
            float extent = quad ? Mathf.Max(height * 1.6f, length * 1.2f) : height * 1.12f;
            float dist = extent / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) / zoom;
            Vector3 camDir;
            switch (view)
            {
                case "front": camDir = new Vector3(0, 0.08f, 1); break;
                case "back": camDir = new Vector3(0, 0.12f, -1); break;
                case "three": camDir = new Vector3(0.75f, 0.12f, 1); break;
                case "top": camDir = Quaternion.Euler(55, 215, 0) * Vector3.back; break;
                default: camDir = new Vector3(1, 0.06f, 0.02f); break;
            }
            camDir.Normalize();

            // neutral studio light whatever area (or menu) happens to be loaded
            var feetLog = new System.Text.StringBuilder();
            bool fog = RenderSettings.fog;
            var amb = (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor);
            var sceneLights = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.enabled && l.type == LightType.Directional && !l.transform.IsChildOf(studio)).ToList();
            for (int f = 0; f < frames; f++)
            {
                if (f > 0)
                {
                    root.position += root.forward * speed * dt0;
                    if (spin != 0) root.rotation = Quaternion.Euler(0, spin * f / frames, 0);
                    tick(dt0);
                }
                Vector3 look = new Vector3(root.position.x, Origin.y + height * (quad ? 0.62f : 0.52f), root.position.z);
                cam.transform.position = look + camDir * dist;
                cam.transform.LookAt(look);
                RenderSettings.fog = false;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.42f, .43f, .47f); RenderSettings.ambientEquatorColor = new Color(.3f, .3f, .31f); RenderSettings.ambientGroundColor = new Color(.14f, .13f, .12f);
                foreach (var l in sceneLights) l.enabled = false;
                cam.Render();
                foreach (var l in sceneLights) l.enabled = true;
                RenderSettings.fog = fog;
                (RenderSettings.ambientMode, RenderSettings.ambientSkyColor, RenderSettings.ambientEquatorColor, RenderSettings.ambientGroundColor) = amb;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(dir, $"f{f:000}.png"), tex.EncodeToPNG());
                if (ha)
                {
                    var rg = ha.rig;
                    feetLog.AppendLine($"{f},{rg[B.FootL].position.x - Origin.x:F3},{rg[B.FootL].position.y:F3},{rg[B.FootL].position.z - Origin.z:F3},{rg[B.FootR].position.x - Origin.x:F3},{rg[B.FootR].position.y:F3},{rg[B.FootR].position.z - Origin.z:F3},{rg[B.Hips].position.y:F3}");
                }
                // one render per engine frame: the GPU Resident Drawer only picks up moved mesh renderers once a frame
                yield return null;
            }
            if (feetLog.Length > 0) File.WriteAllText(Path.Combine(dir, "feet.csv"), "f,lx,ly,lz,rx,ry,rz,hipy" + System.Environment.NewLine + feetLog);
            File.WriteAllText(Path.Combine(dir, "done.txt"), $"frames={frames} fps={fps} {info}");
            rt.Release();
            Destroy(tex);
            Destroy(studio.gameObject);
            Destroy(gameObject);
        }
    }
}
