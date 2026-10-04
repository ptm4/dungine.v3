# Agent A: phase 4, everything on screen is pixel3d

Status: started 2026-10-04.

## Peter's rule (2026-10-04)

Looking at `research\village_kit\index.html`, he wrote: "All the data captured must be with the new models & textures in
pixel3d style otherwise nothing matters", and of the captures: "its with old assets. This means nothing to us".

The village round put the library's buildings in, but the ground, roads, cobbles, lamps, carts, benches, barrels, trees,
the party, the HUD portraits and icons were still v2's, so the captures read as v2. From now on:

- **No capture, still, GIF or video goes to Peter unless everything in frame is the library's pixel3d** (models and
  textures): ground, roads, buildings, props, lamps, trees, creatures, the party, the townsfolk, and the HUD's portraits
  and icons. Only HUD frames and text may stay as they are for now.
- **No stand-ins in v2's style.** If something has no library version yet, take it out of the scene for the capture, or
  wait, and tell the main session so a library agent builds it.
- A before-and-after pair is fine, as long as "after" is wholly pixel3d.

## What to do

1. **Supersede the old page now.** Replace `research\village_kit\index.html` with a short note: those captures still
   showed v2's ground, props, trees and party, and a fully pixel3d capture will replace them. Keep the README and the
   measurements for the record.
2. **The inventory.** Every v2 asset visible in the village and on the default screen (what Play opens), including the
   HUD: what it is, where v2 uses it, and its library replacement (path) or "missing". Write it as
   `research\pixel3d_swap\inventory.md`. Spoiler-free: the reserved plot stays as it is and stays out of every capture.
   Library sources to check (`E:\Assets\DND5E\pixel3d`, and agent N's `export\game\`):
   - people: the heroes and races (reviews 1 to 3), Arkus (locked, as built), Chai'rn (review 7, locked), the
     archetypes (review 6), Chapter One's people (review 9);
   - props: furniture and lighting (10), graveyard and religion (11), adventuring gear (12), horror dressing (14), v2's
     loot (15), weapons and armour (4, 5), mounts, tack and vehicles with the carts (32), treasure and tools (24, 25);
   - creatures: beasts and mounts (13), livestock (26);
   - the village kit (33 to 36) with its cobbles; terrain (43, agent E, in progress) and vegetation (44, agent E, next);
     water and crossings (45, rendering now);
   - the HUD: the icons (reviews 16, 27 to 29, voxel over stained glass; a tooltip on hover); portraits made from the
     library figures' sprites or renders.
3. **Swap everything that has a library version.** Ask agent N (through the main session) for game exports you need
   that don't exist yet; N's `game_export.py` already covers the village kit and rigged figures. Turn the kit's cobbles
   on and make them affordable (11 M triangles is too many: merged faces, N's LODs, or a flat-topped cobble tile at
   distance).
4. **Report the gaps** to the main session: everything still "missing", most-visible first. Expected: the ground and
   roads (agent E's terrain), the trees (agent E's vegetation), and Dulandir (no library model; his design is Peter's to
   brief). Don't build library assets yourself.
5. **Capture only when the frame is wholly pixel3d:** the same three walks and stills, at the game camera, plus what
   Play opens. Then a page for Peter, `research\pixel3d_village\index.html`, plain English.

## Rules

- Commit to v3's main with clear messages; close the editor when you finish; don't touch the mcp processes.
- No downloads (glTFast is already in). Don't touch `E:\Unity\Projects\Dungine`; never modify dungine.v2; never read
  `pixel3d\sealed`; never mark anything approved.
- Frame time still matters: report triangles, GPU time and load time as before, and the shadow cost.

**Final reply:** the inventory's counts (swapped, missing), the gaps list, the commits, and the capture page (if the
frame is wholly pixel3d), or why not yet.
