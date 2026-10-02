using UnityEngine;
using UnityEngine.AI;

namespace Dungine
{
    /// <summary>An escorted NPC that trails the selected party member outside combat.</summary>
    public class Follower : MonoBehaviour
    {
        public float distance = 2.6f;
        Actor self; float repath;

        void Start() { self = GetComponent<Actor>(); }

        void Update()
        {
            if (!self || Game.I == null || Game.I.mode == GameMode.Combat || Game.I.mode == GameMode.Dialogue) return;
            var leader = Game.I.Selected;
            if (!leader || !self.agent || !self.agent.isActiveAndEnabled || !self.agent.isOnNavMesh) return;
            repath -= Time.deltaTime;
            float d = Vector3.Distance(leader.transform.position, transform.position);
            if (d > 25f) { if (NavMesh.SamplePosition(leader.transform.position - leader.transform.forward * 2f, out var h, 3f, NavMesh.AllAreas)) self.Warp(h.position); return; }
            if (d > distance + 1.2f && repath <= 0)
            {
                repath = 0.4f;
                var target = leader.transform.position - (leader.transform.position - transform.position).normalized * distance;
                self.SetAgentSpeed(d > 8 ? 5f : 4f);
                self.MoveTo(target, null, 0.4f);
            }
        }
    }
}
