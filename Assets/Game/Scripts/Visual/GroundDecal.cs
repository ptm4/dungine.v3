using UnityEngine;

namespace Dungine.Visual
{
    /// <summary>A textured quad that drapes itself over whatever ground is beneath it (selection rings, AoE templates).</summary>
    public class GroundDecal : MonoBehaviour
    {
        Mesh mesh; MeshRenderer mr; Material mat;
        public float size = 1f;
        public float pulse;
        public int res = 10;
        public float lift = 0.035f;
        public float yaw;
        float lastYaw;
        public Color color;
        Vector3 lastPos; float lastSize; bool dirty = true;
        Vector3[] verts;

        public static GroundDecal Create(string name, Texture tex, Color color, float size, Transform parent = null, int res = 10, bool additive = true)
        {
            var go = new GameObject(name);
            go.layer = Layers.FX;
            if (parent) go.transform.SetParent(parent, false);
            var d = go.AddComponent<GroundDecal>();
            d.size = size; d.res = res; d.color = color;
            d.mesh = new Mesh { name = name };
            d.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = d.mesh;
            d.mr = go.AddComponent<MeshRenderer>();
            d.mat = MatLib.FXInstance(tex, color, additive, 0f);
            d.mat.renderQueue = 3050;
            d.mr.sharedMaterial = d.mat;
            d.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            d.mr.receiveShadows = false;
            d.BuildTopology();
            return d;
        }

        void BuildTopology()
        {
            int n = res + 1;
            verts = new Vector3[n * n];
            var uv = new Vector2[n * n];
            var tris = new int[res * res * 6];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) uv[y * n + x] = new Vector2(x / (float)res, y / (float)res);
            int k = 0;
            for (int y = 0; y < res; y++) for (int x = 0; x < res; x++)
                {
                    int a = y * n + x, b = a + 1, c = a + n, d = c + 1;
                    tris[k++] = a; tris[k++] = c; tris[k++] = d; tris[k++] = a; tris[k++] = d; tris[k++] = b;
                }
            mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tris;
        }

        public void SetColor(Color c) { color = c; if (mat) mat.SetColor("_BaseColor", c); }
        public void SetSize(float s) { if (Mathf.Abs(s - size) > 0.001f) { size = s; dirty = true; } }
        public void MarkDirty() => dirty = true;

        void LateUpdate()
        {
            transform.rotation = Quaternion.identity;
            if (pulse > 0 && mat) mat.SetColor("_BaseColor", new Color(color.r, color.g, color.b, color.a * (0.75f + 0.25f * Mathf.Sin(Time.time * 4f))));
            if (!dirty && (transform.position - lastPos).sqrMagnitude < 0.0025f && Mathf.Abs(yaw - lastYaw) < 0.5f) return;
            dirty = false; lastPos = transform.position; lastSize = size; lastYaw = yaw;
            Quaternion rq = Quaternion.Euler(0, yaw, 0);
            int n = res + 1;
            Vector3 origin = transform.position;
            float half = size * 0.5f;
            int mask = Layers.GroundMask;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Vector3 l = rq * new Vector3(-half + size * x / res, 0, -half + size * y / res);
                    float lx = l.x, lz = l.z;
                    Vector3 wp = origin + new Vector3(lx, 2.0f, lz);
                    float gy = origin.y;
                    if (Physics.Raycast(wp, Vector3.down, out var hit, 4.5f, mask, QueryTriggerInteraction.Ignore)) gy = hit.point.y;
                    verts[y * n + x] = new Vector3(lx, gy - origin.y + lift, lz);
                }
            mesh.vertices = verts;
            mesh.RecalculateBounds();
        }
    }
}
