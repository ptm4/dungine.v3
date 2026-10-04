# Agent A: phase 3, rigged library figures moving in v3 (agent B's step G2)

Status: started 2026-10-03.

Phase 2 and the look test are done (`research\phase2\`, `research\look_test\`). Peter has the look test pictures; his
answers on the look (native or pixelated) and outlines are pending, and they change only the shader.

## What to do

- Import the library's rigged GLBs into v3 with their skins (glTFast). The heroes have them at
  `E:\Assets\DND5E\pixel3d\characters\heroes\<id>\rig\<id>.glb`, and the review 9 people at
  `...\characters\npcs\<id>\rig\<id>.glb`. Their rigs use soft bends at the elbows and knees, Peter's choice, plus spring
  chains for skirts, capes and hair (VRM-style spring parameters in `<id>.rig.json`).
- Write the adapter that fills v2's `HumanoidRig` from those bones and sockets, as your audit and agent B's plan
  (`research\animation_pipeline.md`, G2) describe, with UpperChest taking a share of Chest. v2 animates in code, so v2's
  own walk, idle, attack and cast motions should drive the library figure unchanged.
- Check the rest pose against v2's (the kit's hang stance, arms 11° out; Peter chose 1a).
- Make the spring chains move (a simple spring solver or an existing free one already in v3's packages; no new
  downloads without asking).
- Put one library figure in place of one of v2's figures. A Chapter One villager in the village, walking v2's routes,
  is a good first test. Capture it moving (a GIF with v2's `gif.py`, and stills) for Peter.
- Commit in v3's git.

## Rules

As in your earlier briefs:
- Never modify dungine.v2 or touch `E:\Unity\Projects\Dungine`.
- Never write into `E:\Assets\DND5E`: the library's game export is being improved by agent N on the library side.
- Don't read `pixel3d\sealed`.
- Close the editors you open.
- No downloads without asking.
- Plain English in anything Peter reads.

**Final reply:** what moves and how well, captures' paths, problems with the rigs (for agent N), next steps.
