using Dungine.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dungine
{
    /// <summary>
    /// Baldur's Gate 3 style camera: orbits a ground target, WASD pans, Q/E or middle mouse rotates,
    /// the scroll wheel zooms (and tilts), it follows the selected character, and it frames dialogue.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig I;
        public Camera cam;
        public Vector3 target;
        public float yaw = 35f;
        public float zoom = 14f;
        public float minZoom = 3.5f, maxZoom = 28f;
        public bool follow = true;
        public Transform followT;
        Vector3 vel; float targetYaw; float targetZoom;
        bool cinematic; Vector3 cinPos; Quaternion cinRot; float cinFov = 30f;
        Vector3 shake; float shakeAmt;
        public float panSpeed = 1f, rotateSpeed = 1f;

        void Awake()
        {
            I = this;
            cam = Dev.EnsureCamera();
            DontDestroyOnLoad(cam.gameObject);
            cam.fieldOfView = 38f;
            targetYaw = yaw; targetZoom = zoom;
        }

        public void SnapTo(Vector3 p, float newYaw)
        {
            target = p;
            zoom = targetZoom = Mathf.Min(targetZoom, maxZoom);
            // don't start with the camera buried in a building: try the requested yaw, then turn until the view is clear
            float best = newYaw;
            foreach (var dy in new[] { 0f, 180f, 90f, -90f, 45f, -45f, 135f, -135f })
            {
                float y = newYaw + dy;
                if (ViewClear(p, y)) { best = y; break; }
            }
            yaw = targetYaw = best;
            Apply(true);
        }

        bool ViewClear(Vector3 p, float y)
        {
            float t = Mathf.InverseLerp(minZoom, maxZoom, zoom);
            float pitch = Mathf.Lerp(28f, 62f, Mathf.SmoothStep(0, 1, t));
            Vector3 focus = p + Vector3.up * Mathf.Lerp(1.4f, 0.5f, t);
            Vector3 pos = focus - Quaternion.Euler(pitch, y, 0) * Vector3.forward * zoom;
            return !Physics.Linecast(focus + Vector3.up * 0.3f, pos, Layers.Mask(Layers.Default, Layers.Walls), QueryTriggerInteraction.Ignore);
        }

        public void Focus(Vector3 p) { target = p; follow = true; }

        public void SetView(float newYaw, float newZoom) { targetYaw = newYaw; targetZoom = Mathf.Clamp(newZoom, minZoom, maxZoom); }

        public void SetCinematic(Vector3 pos, Vector3 lookAt, float fov = 30f)
        {
            cinematic = true; cinPos = pos; cinRot = Quaternion.LookRotation(lookAt - pos); cinFov = fov;
        }

        public void EndCinematic() { cinematic = false; }

        public void Shake(float amount) { shakeAmt = Mathf.Max(shakeAmt, amount); }

        public static bool TypingInUI => UI.UIRoot.I != null && UI.UIRoot.I.TextFieldFocused;

        void LateUpdate()
        {
            if (!cam) return;
            float dt = Time.unscaledDeltaTime;
            var kb = Keyboard.current; var mouse = Mouse.current;
            bool inputAllowed = !cinematic && Game.I != null && (Game.I.mode == GameMode.Explore || Game.I.mode == GameMode.Combat) && !TypingInUI;
            if (inputAllowed && kb != null)
            {
                Vector3 pan = Vector3.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) pan += Vector3.forward;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) pan += Vector3.back;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) pan += Vector3.left;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) pan += Vector3.right;
                if (pan.sqrMagnitude > 0)
                {
                    follow = false;
                    float spd = (6f + zoom * 0.9f) * panSpeed * (kb.leftShiftKey.isPressed ? 2f : 1f);
                    target += Quaternion.Euler(0, yaw, 0) * pan.normalized * spd * dt;
                }
                if (kb.qKey.isPressed) targetYaw += 110f * rotateSpeed * dt;
                if (kb.eKey.isPressed) targetYaw -= 110f * rotateSpeed * dt;
                if (kb.homeKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame) follow = true;
            }
            if (inputAllowed && mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !UI.UIRoot.PointerOverUI)
                    targetZoom = Mathf.Clamp(targetZoom * (scroll > 0 ? 0.88f : 1.13f), minZoom, maxZoom);
                if (mouse.middleButton.isPressed)
                {
                    var d = mouse.delta.ReadValue();
                    targetYaw += d.x * 0.25f * rotateSpeed;
                }
            }
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            if (follow)
            {
                var sel = followT != null ? followT : Game.I?.Selected?.transform;
                if (sel != null) target = Vector3.SmoothDamp(target, sel.position, ref vel, 0.25f, 60f, dt);
            }
            yaw = Mathf.LerpAngle(yaw, targetYaw, 1 - Mathf.Exp(-dt * 10f));
            zoom = Mathf.Lerp(zoom, targetZoom, 1 - Mathf.Exp(-dt * 8f));
            Apply(false);
        }

        void Apply(bool instant)
        {
            if (cinematic)
            {
                float k = instant ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime * 4f);
                cam.transform.position = Vector3.Lerp(cam.transform.position, cinPos, k);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, cinRot, k);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, cinFov, k);
                return;
            }
            // pitch rises as we zoom out: close = over the shoulder, far = tactical
            float t = Mathf.InverseLerp(minZoom, maxZoom, zoom);
            float pitch = Mathf.Lerp(28f, 62f, MathX.Smoothstep(0, 1, t));
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
            Vector3 focus = target + Vector3.up * Mathf.Lerp(1.4f, 0.5f, t);
            Vector3 pos = focus - rot * Vector3.forward * zoom;
            // keep above ground
            if (Physics.Raycast(pos + Vector3.up * 50, Vector3.down, out var hit, 100, Layers.GroundMask, QueryTriggerInteraction.Ignore))
                if (pos.y < hit.point.y + 1.2f) pos.y = hit.point.y + 1.2f;
            if (shakeAmt > 0.001f)
            {
                shake = Random.insideUnitSphere * shakeAmt;
                shakeAmt = Mathf.MoveTowards(shakeAmt, 0, Time.unscaledDeltaTime * 1.5f);
            }
            else shake = Vector3.zero;
            float kk = instant ? 1 : 1 - Mathf.Exp(-Time.unscaledDeltaTime * 14f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, pos + shake, kk);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(focus - pos), kk);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, 38f, kk);
        }

        public Ray MouseRay()
        {
            var m = Mouse.current;
            return cam.ScreenPointToRay(m != null ? m.position.ReadValue() : new Vector2(Screen.width / 2, Screen.height / 2));
        }

        public Vector2 WorldToScreen(Vector3 p) => cam.WorldToScreenPoint(p);
    }
}
