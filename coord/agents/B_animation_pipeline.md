# Agent B: the animation pipeline

Status: done 2026-09-30. Report: `coord\research\animation_pipeline.md` (UniMate included; seven decisions for Peter).

## Brief (the prompt as given)

**Context**
- Peter's asset library, `E:\Assets\DND5E`, builds voxel "3D pixel art" characters by Python in `pixel3d\pipeline`, at
  1 voxel = 1.5 cm.
- The kit is in `pipeline\kit`. Bodies grow from joint positions `P`: shoulder, elbow, wrist, hand, hip, knee, ankle,
  toe, `neck_a` and `neck_b`, `head_c`, `chest_c`, `waist_c` and `hips_c`. Clothes and gear are painted onto the body.
- It exports `.npz` and `.vox`, and GLB through Blender (`render_vox.py --glb`). Figures are faceless; the style
  reference is Hytale.
- The library is meant for Dungine.v3, a Curse of Strahd party RPG, but must stay engine-neutral. The engine isn't
  chosen yet; Unity is likely, since the v2 demo is Unity.
- Peter: "we'll eventually need a full range of animations on these characters". The main session is adding a skeleton
  to the kit right now: each voxel gets a bone and a weight, so heads bow and turn and bodies stoop. That skeleton should
  become the rig.
- Read `E:\Unity\Projects\dungine.v3\coord\PLAN.md` and skim `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`.

**Task:** write a proposal, `E:\Unity\Projects\dungine.v3\coord\research\animation_pipeline.md`.
1. **From voxel figures to animated game characters, engine-neutral:** a glTF 2.0 skinned mesh with a humanoid skeleton.
   - Give bone names and a hierarchy that map cleanly to Unity Humanoid, Godot's SkeletonProfileHumanoid, Mixamo and
     Blender, as a table.
   - Compare rigid skinning (one bone per voxel) with blended skinning for a voxel look, and how Hytale- and
     Minecraft-style games animate blocky characters.
   - Say how to keep voxels crisp, with no stretching, at the joints.
   - Say what to do with long hair, skirts, cloaks, capes, held props and weapons (attachment bones or sockets,
     secondary motion), and with rigid props like a handcart.
2. **Where animations come from.** Research these, no downloads, and say what each costs Peter:
   - Mixamo (its terms for a private game, and retargeting to our rig);
   - paid packs;
   - hand keyframing in Blender;
   - procedural animation (IK, look-at).
3. **The animation set** a Curse of Strahd party RPG needs:
   - locomotion and idles;
   - combat by weapon family: the library has simple and martial melee, ranged, and two-handers carried across the
     chest;
   - spellcasting, reactions, hits and deaths;
   - interactions: doors, chests, picking things up;
   - emotes and conversation gestures;
   - NPC behaviours: villagers working, praying, the tavern;
   - villagers' reactions to non-human party members. Peter wants Barovians to meet non-humans with distrust scaled by
     race: stepping back, turning away, a warding sign, shutting a door.

   Prioritise: what the first playable slice needs, and what comes later.
4. **A step-by-step plan** for the library side (what the kit and export must add) and the game side, with effort
   estimates and risks.

**Rules**
- Read-only everywhere except that one file.
- Don't touch `E:\Unity\Projects\Dungine`. Never modify dungine.v2; reading it read-only is fine if it helps.
- Don't read `E:\Assets\DND5E\pixel3d\sealed`.
- No Blender and no downloads.
- Keep anything Peter reads free of Curse of Strahd spoilers.
- Write in plain, direct English.

**Final reply:** 5 to 10 lines summarising the recommendation, plus the file path.
