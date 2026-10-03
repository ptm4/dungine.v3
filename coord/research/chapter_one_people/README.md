# Chapter One's people, props and crowd cost (agent A, 2026-10-03)

## People

- `LookTest.Boot` now calls `LibraryFigures.UseChapterOne()`. The villager0 test mapping is gone; villager0 strolls as
  v2's own figure.
- The ids mapped are:
  - arik;
  - ismark, ismark_home, ismark_g;
  - ireena, ireena_g, ireena_f;
  - donavich, donavich_g;
  - vistani0/1/2 (Alenka, Mirabel, Sorvia);
  - bildrath, parriwimple;
  - rose, thorn.
- The house's ghosts and "strahd" stay v2's. Nothing from `pixel3d\sealed` is used.
- `ActorFactory` builds the library rig whenever the NPC id is mapped. `Area.NPC` gives the NPC id as `m.id`.
- Positions, seating (`Actor.Sit`) and facing are v2's.
- Checked in place, with captures in `Assets/Captures/people_*.png`:
  - arik, ismark, and vistani0 to 2 in the tavern, seated as in v2;
  - bildrath and parriwimple in the shop;
  - ireena in the mansion;
  - donavich in the church;
  - rose and thorn by the house in the village (spawn deathhouse).
- Clips: `DevCaptures/p_*`, cropped to `p_*_c`.
- **v2 issue:** Arik spawns at (2.4, 4.3), but his NavMeshAgent snaps him to (2.64, 5.84), against the back wall,
  where the game camera can't see him. v2's own figure does the same, so the cause is v2's placement or navmesh.
  Not changed.

## Props

- `LibraryProps.Weaponise(rig, rigData)` swaps v2's weapon mesh for the library's baked model, in hand or sheathed.
  The models are in `LookTest/Baked`: longsword, greatsword, rapier, scimitar, dagger, mace, quarterstaff/staff and
  longbow.
- When the rig's `parts` include a sheathed weapon (`*sword*_hip`, `greatsword_back`, ...), `ShowWhenDrawn` shows v2's
  copy only while drawn. That applies to Ismark and Ireena.
- **Orientation:** with `LibraryProps.ShowV2Too` the library model lies on v2's mesh along +Y with the grip in the
  fist. See `weapons_against_v2.png` (`DevCaptures/w_*`). The two-handed IK (greatsword, quarterstaff) places both
  hands as in v2.
- **Held things:** `LibraryProps.HeldBy`, kept upright by `KeepUpright`:
  - Arik: ale_mug at SocketHandR;
  - Alenka: jug_or_pitcher at SocketHandL;
  - Parriwimple: a sack on each upper arm.

## Crowd cost

Village square, default camera, `tools/lookmeasure.sh` with 600 frames. The library crowd is `LookTest.RigCrowd(30)`
(export rigs, LODs on, springs on). The v2 crowd is `V2Crowd(30)`.

| | mean | median | 95th | tris | verts | draws |
|---|---|---|---|---|---|---|
| nothing added | 7.31 | 7.03 | 9.32 | 1.50 M | 1.53 M | 1207 |
| +30 library rigs | 9.20 / 10.38 (two runs) | 7.92 / 9.71 | 13.9 / 15.5 | 6.05 M | 10.6 M | 1381 |
| +30 library rigs, springs off | 7.94 | 7.49 | 11.05 | 6.01 M | 10.6 M | 1373 |
| +30 library rigs, all LOD1 (springs on) | 9.07 | 7.47 | 13.70 | 3.64 M | 5.79 M | 1383 |
| +30 v2 figures | 8.07 | 7.23 | 11.82 | 5.90 M | 5.62 M | 3172 |

- The springs (C# verlet with colliders, in `LibraryRigDriver`) are most of the extra cost, about 0.05 to 0.08 ms per
  figure.
- With the springs off, 30 library rigs cost about what 30 v2 figures do, in fewer draw calls.
- The cheap next steps:
  - run the springs at a lower rate, or not at all, for far or off-screen figures;
  - a Burst job.
