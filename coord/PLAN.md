# Dungine.v3 plan

**Goal:** take the working dungine.v2 demo, re-asset it with the DND5E pixel3d library (voxel "3D pixel art", faceless
figures, Hytale as the reference), and test it further. The result is a Curse of Strahd party RPG Peter plays with friends.

**Ground rules**
- The asset library stays independent (`E:\Assets\DND5E`): v3 takes copies of approved assets and never edits the library
  in place.
- v2 is read-only. It's where v3 starts, not something to change.
- Everything Peter reads stays free of Curse of Strahd spoilers: he plays the campaign. What only play reveals is built
  and sealed in the library, never shown.
- One headless Blender at a time, on this machine. Agents never run Blender; the main session does the renders.

## Phases

| # | Phase | What it takes | Status |
|---|---|---|---|
| 0 | Set up v3 | this folder, `coord\`, the board | done 2026-09-30 |
| 1 | Engine decision | an audit of v2, then Unity against Godot and the rest, for a voxel pixel-art party RPG | recommendation: stay on Unity 6 (`research\engine_decision.md`); Peter decides |
| 2 | Fork v2 | back up v2 (its git repo has no commits), copy it into v3, get it running unchanged | after phase 1 |
| 3 | Asset bridge | library to game: a skeleton and animation for the characters (`research\animation_pipeline.md`: library steps L1 to L7, game steps G1 to G7), and the voxel material and lighting | L1 to L3 running (agent I); G1 onwards after phase 2 |
| 4 | Re-asset | swap v2's generated art for approved library assets, scene by scene, starting with the village of Barovia and the house at the edge of the fog | as library batches are approved |
| 5 | Game design additions | Barovian suspicion of non-humans, scaled by race (`design\barovian_suspicion.md`, a draft); more as Peter decides | draft waiting for Peter |

## What the library does next (for the game)

- Posing in the kit (the main session, now): heads bow and turn, bodies stoop and lean, on a skeleton. The same skeleton
  becomes the characters' rig for animation.
- Review 9 round 2: the Chapter One people posed as their briefs describe.
- The rest of Chapter One's batches: beasts and mounts (C01), village kit (E01), terrain and vegetation (E02, E03),
  furniture and lighting (P01, P02), graveyard and religion (P03), gear (G01) and more. See the library's scope plan.
