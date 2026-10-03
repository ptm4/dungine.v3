using System;
using System.Collections.Generic;
using Dungine.Rules;
using Dungine.Visual;
using UnityEngine;
using UnityEngine.AI;

namespace Dungine
{
    public static class Layers
    {
        public const int Default = 0, Actors = 8, Interact = 9, Walls = 10, Portrait = 11, Ground = 12, FX = 13, Roof = 14;
        public static int Mask(params int[] l) { int m = 0; foreach (var x in l) m |= 1 << x; return m; }
        public static int GroundMask => Mask(Default, Ground);
    }

    /// <summary>A creature standing in the world: movement, animation, highlight, selection.</summary>
    public class Actor : MonoBehaviour
    {
        public Creature c;
        public NavMeshAgent agent;
        public IAnimDriver anim;
        public HumanoidRig rig;
        public List<Renderer> renderers = new List<Renderer>();
        public string npcId;
        public string dialogue;
        public string barkOnClick;
        public bool talkable;
        public float radius = 0.4f;
        public GroundDecal ring;
        public Vector3 home;
        public string displayName;
        public bool hostileOnSight;
        public float sightRange = 12f;
        public string encounterId;
        public bool lootable = true;
        public List<ItemStack> loot = new List<ItemStack>();
        public int gold;
        public bool looted;

        Action onArrive;
        bool moving;
        MaterialPropertyBlock mpb;
        Color highlight = Color.black; float highlightAmt;
        float stuckTimer;
        public bool manualMove;

        public string Name => !string.IsNullOrEmpty(displayName) ? displayName : c?.name ?? name;
        public Vector3 Pos => transform.position;
        public float Height => anim != null ? anim.Height : 1.8f;
        public Vector3 Chest => transform.position + Vector3.up * Height * 0.62f;
        /// <summary>Where the eyes are right now — follows the animated head, so it works for seated or crouching actors.</summary>
        public Vector3 HeadPos => rig && rig.eyes ? rig.eyes.position : rig && rig[B.Head] ? rig[B.Head].position + Vector3.up * rig.headSize * 0.35f : transform.position + Vector3.up * Height * 0.95f;
        public bool IsMoving => moving;
        public bool IsPC => c != null && c.isPC;

        void Awake() { mpb = new MaterialPropertyBlock(); }

        public void Init(Creature cr)
        {
            c = cr; cr.actor = this;
            agent = GetComponent<NavMeshAgent>();
            home = transform.position;
        }

        public void MoveTo(Vector3 p, Action arrive = null, float stop = 0.15f)
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) { onArrive = arrive; transform.position = p; arrive?.Invoke(); return; }
            onArrive = arrive;
            agent.stoppingDistance = stop;
            agent.isStopped = false;
            if (agent.SetDestination(p)) { moving = true; stuckTimer = 0; }
            else { moving = false; var a = onArrive; onArrive = null; a?.Invoke(); }
        }

        public void Stop()
        {
            moving = false; onArrive = null;
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        }

        public void Warp(Vector3 p)
        {
            if (agent != null && agent.isActiveAndEnabled) agent.Warp(p); else transform.position = p;
        }

        public void Face(Vector3 p, bool instant = false)
        {
            Vector3 d = p - transform.position; d.y = 0;
            if (d.sqrMagnitude < 0.0001f) return;
            var q = Quaternion.LookRotation(d);
            if (instant) transform.rotation = q; else faceTarget = q;
        }

        Quaternion? faceTarget;

        void Update()
        {
            if (moving && agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !manualMove)
            {
                if (!agent.pathPending)
                {
                    if (agent.remainingDistance <= agent.stoppingDistance + 0.05f || (!agent.hasPath && agent.velocity.sqrMagnitude < 0.01f))
                    {
                        moving = false;
                        var a = onArrive; onArrive = null; a?.Invoke();
                    }
                    else if (agent.velocity.sqrMagnitude < 0.01f)
                    {
                        stuckTimer += Time.deltaTime;
                        if (stuckTimer > 1.2f) { moving = false; agent.ResetPath(); var a = onArrive; onArrive = null; a?.Invoke(); }
                    }
                    else stuckTimer = 0;
                }
            }
            if (faceTarget.HasValue)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, faceTarget.Value, 540 * Time.deltaTime);
                if (Quaternion.Angle(transform.rotation, faceTarget.Value) < 1f) faceTarget = null;
            }
            if (highlightAmt > 0.001f || lastApplied > 0.001f) ApplyHighlight();
        }

        float lastApplied;
        void ApplyHighlight()
        {
            float amt = highlightAmt;
            lastApplied = amt;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", highlight * amt);
                r.SetPropertyBlock(mpb);
            }
            if (amt <= 0.001f) foreach (var r in renderers) if (r) r.SetPropertyBlock(null);
        }

        public void SetHighlight(Color c, float amt) { highlight = c; highlightAmt = amt; }

        public void SetRing(bool on, Color color, float pulse = 0)
        {
            if (ring == null)
            {
                ring = GroundDecal.Create("SelRing", ProcTex.Ring, color, radius * 2.4f, transform);
            }
            ring.gameObject.SetActive(on);
            ring.SetColor(color);
            ring.pulse = pulse;
        }

        public void SetAgentSpeed(float s) { if (agent) agent.speed = s; }

        /// <summary>Seats a humanoid NPC (on a bench or chair); it stops pathing so nothing shoves it off.</summary>
        public void Sit(float seatHeight = 0.46f)
        {
            if (anim is HumanoidAnimator h) { h.seated = true; h.seatHeight = seatHeight; }
            if (agent) agent.enabled = false;
            sitting = true;
        }
        public bool sitting;

        public void PlayAnim(AnimAct a, Action impact = null) { if (anim != null) anim.Play(a, impact); else impact?.Invoke(); }

        public void RefreshLife()
        {
            if (anim == null || c == null) return;
            if (c.dead) anim.SetLife(LifeState.Dead);
            else if (c.hp <= 0) anim.SetLife(c.isPC ? LifeState.Downed : LifeState.Dead);
            else anim.SetLife(LifeState.Alive);
            if (agent)
            {
                bool want = c.hp > 0 && !c.dead && !sitting;
                if (want && !agent.enabled && !NavMesh.SamplePosition(transform.position, out _, 2f, NavMesh.AllAreas)) want = false;
                agent.enabled = want;
            }
            var col = GetComponent<Collider>();
            if (col) col.enabled = true;
        }

        public bool CanSee(Vector3 p)
        {
            Vector3 a = transform.position + Vector3.up * 1.4f, b = p + Vector3.up * 1.0f;
            return !Physics.Linecast(a, b, Layers.Mask(Layers.Walls, Layers.Default), QueryTriggerInteraction.Ignore);
        }
    }
}
