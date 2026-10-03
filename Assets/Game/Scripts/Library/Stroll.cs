using System.Collections;
using Dungine.World;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine.Library
{
    /// <summary>
    /// A villager's walk: round a loop of points on the area's navmesh, at a stroll, pausing at each point to look about.
    /// It moves the figure the way v2 moves everyone (Actor.MoveTo on the NavMeshAgent), so v2's animator does the rest.
    /// v2's own villagers stand still; this gives the first rigged library figure somewhere to walk.
    /// </summary>
    public class Stroll : MonoBehaviour
    {
        public Vector3[] points;
        public float speed = 1.3f, pause = 1.6f;
        Actor actor;

        public static Stroll Start(AreaContext ctx, string npcId, Vector2[] xz)
        {
            if (!ctx.byId.TryGetValue(npcId, out var a) || !a) return null;
            var s = a.gameObject.AddComponent<Stroll>();
            s.points = new Vector3[xz.Length];
            for (int i = 0; i < xz.Length; i++)
            {
                var p = ctx.G3(xz[i].x, xz[i].y);
                if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) p = hit.position;
                s.points[i] = p;
            }
            return s;
        }

        IEnumerator Start()
        {
            actor = GetComponent<Actor>();
            while (actor && actor.agent && !actor.agent.enabled) yield return null;   // the area bakes its navmesh first
            if (!actor) yield break;
            actor.SetAgentSpeed(speed);
            int k = 0;
            while (actor)
            {
                if (Game.I.mode == GameMode.Explore && actor.c.Active && !actor.c.dead)
                {
                    bool arrived = false;
                    actor.MoveTo(points[k], () => arrived = true, 0.3f);
                    float t = 0;
                    while (!arrived && actor && t < 30f) { t += Time.deltaTime; yield return null; }
                    // a look round before going on
                    var next = points[(k + 1) % points.Length];
                    actor.anim?.LookAt(next + Vector3.up * 1.5f);
                    yield return new WaitForSeconds(pause);
                    actor.anim?.LookAt(null);
                    k = (k + 1) % points.Length;
                }
                else yield return null;
            }
        }
    }
}
