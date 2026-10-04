# Pixel3d swap, round 1 (agent A, 2026-10-04)

The brief is `coord\agents\A_phase4_all_pixel3d.md`. Files in this folder:

- `inventory.md`: every v2 asset on screen against the library.
- `gaps.md`: what blocks a wholly pixel3d capture, most visible first.
- `measurements.txt`: the frame-time runs.

**There is no capture for Peter yet.** The ground, the trees and the party are still v2's, so no frame is wholly pixel3d.

## What changed in v3

- **`Library/LibrarySwap.cs`, hooked into `Kit.Place`:**
  - v2's props wear the library's model where the library has one: lampposts, barrels, crates, bench, cart, haybales,
    woodpiles, grave mounds and the coffin.
  - Hook: `Kit.Place` (`Kit.cs`) calls `LibrarySwap.Apply` after building each prop.
  - v2's object stays, keeping its collider and uses, with its own mesh switched off. The library model stands in it
    at true size.
  - Sources: N's game export where it has the prop (lamppost, barrel, crate); otherwise the look test's bake of the
    library's `model.glb`.
  - 411 props are swapped in the village and 1 (the lamp) on the title screen.
- **`Library/LibraryIcons.cs` + `UI/HUD.cs`:**
  - the hotbar shows the library's action and spell icons (reviews 27 to 29, by v2's action id) and the item sprites
    for the stash consumables;
  - the party panel and turn order show the library's face renders for Arkus and Chai'rn.
  - Dulandir and anything unmapped keep v2's.
- **Weapons:** `ActorFactory` calls `LibraryProps.Weaponise` on v2's own figures too, so the party carries the library's
  glaive and quarterstaves. The glaive was added to the weapon table (a0 -136 cm, hand at -80 cm).
- **The kit's cobbles (`VillageKit.PlaceSquare = true`):**
  - v2's ground is lowered 15 cm under each cobble tile, so it no longer fills the stone joints.
  - A flat collider floor at the stones' level carries the navmesh and clicks.
  - Each tile has its own LODs: 1.5 cm within `CobbleNear` = 16 m of the camera, 3 cm to 55 m, 6 cm to 110 m, then the
    site tile's far mesh.
  - The cobbles cast no shadow.
  - `LightCobbles` (3 cm everywhere) costs least but reads as speckle.
- **The old village page** (`research\village_kit\index.html`) is withdrawn (commit aaf9ff9). Its captures are deleted;
  its README and measurements stay.

## Cost

Village, default camera, 600 frames, two runs (`measurements.txt`):

- Triangles a frame: square 14.1 M, high street 10.3 M, church gate 20.9 M, wide view 13.0 to 14.9 M.
  - The village without the cobbles and props was 9.6 to 10.4 M, and 19.3 M at the church.
  - Sitting at 1.5 cm: square 24.4 M; at 3 cm: 11.6 M.
- GPU medians are 2.5 to 4.5 ms, against 2.6 to 4.6 ms before this round, inside the noise. The editor's main
  thread (5.5 to 8.3 ms) still sets the frame.
- **Shadows** at the church gate: turning off the swapped props' shadows saves 1.2 M triangles (20.9 to 19.7 M). The
  kit buildings' shadows remain the big share (about 16 M there, as measured last round).
- **Load:** building the village takes 2.0 s on a warm reload. A fresh editor session's first load is slower, while
  Resources imports.

# Round 2 (2026-10-04)

**There is still no capture for Peter.** The trees, Dulandir and the title screen remain v2's (`gaps.md`).

## What changed

- **The ground** (`Library/VillageGround.cs`, called first in `AreaVillage.Build`):
  - **Source:** agent N's `village_ground.json`, laid as the kit is: 22,264 tiles and 49 outcrops from 40 terrain
    modules (`Resources/Library/Ground`), plus 99 far meshes (`Village/ground_far`).
  - **Heights:** the file's `levels` are written into v2's Unity terrain, which stops drawing and stays the collider.
    So the navmesh, `ctx.GroundY` and everything v2 places later (spawns, people, doors, props) stand on the
    library's ground: the manor on its 3 m hill, the church on its 1.5 m rise.
  - **v2's roads and splat ground are no longer drawn.**
  - **Detail levels:** each tile has its own LODGroup (1.5 cm within 12 m of the camera, 3 cm to 40 m, 6 cm to 90 m),
    then its 24 m square's far mesh. That far mesh sits in a LODGroup that draws nothing near and the far mesh beyond
    90 m.
  - **Shadows and AO:** the tiles cast no shadows. Texture AO: a missing `COLOR_0` is read as no vertex AO
    (`_OcclusionStrength` 0, in URP Lit's own material slot so the shader stays SRP-batcher compatible).
- **The village relaid** from the new `village_layout.json` (3,109 placements, the darker second looks, the lifts):
  - On the library ground the layout's heights are absolute, and each building's lift comes from `lift_cm`.
  - Ground-floor colliders are taken relative to the lift. Far meshes sit at the origin.
- **The villagers:** v2's four (`villager0..3`) are the Barovian commoner rigs, both looks.
- **The party:** Arkus and Chai'rn are their library rigs (Peter, 2026-10-04: "Yes, both into v3").
  - Arkus is empty-handed (`LibraryFigures.EmptyHanded`) until agent O's greatsword lands; no v2 weapon stands in.
  - Chai'rn carries the library quarterstaff.
  - Dulandir is v2's until review 42.
- **Props:** the bench, cart, haybale, woodpile, grave mound, coffin and glaive are agent N's game exports, re-run with
  the new LOD rule, at normal LOD distances. The glaive's grip is taken from its bounds (a butt-up model).
- **The cobbles:** `cobbled_square` with texture AO, at full 1.5 cm within 55 m (`CobbleNear`), i.e. the whole square
  at play zoom.
- **The HUD buttons:** bag, character, journal, camp and settings are the library's (`ui\icons\hud\hud_*`, approved by
  Peter).

## Doors and the navmesh on the new ground

- **Reachability:** all 22 village doors are reachable on the navmesh from the square, each within its use range. This
  includes the manor door on the 3 m hill and the church door on the rise.
- **Walked in for real:** the party walked into the tavern, the shop, the manor and the church, and each time arrived
  in that interior.
- **Coming back out:** each lands on the navmesh on the new ground.
  - Manor door: y 2.91, ground 2.89.
  - Church door: y 0.92, ground 0.89.
  - Tavern and shop: at 0.
- **What sits inside the kit buildings' colliders:** nothing (no NPC, spawn or interactable). The scored manor door
  note sits on the kit's door, as before.

## Cost (`measurements_round2.txt`; 600 frames, default camera, the editor)

| View | Triangles: library ground + cobbles at 1.5 cm | cobbles 1.5 cm within 16 m | v2's ground |
|---|---|---|---|
| square | 17.2 M | 12.3 M | 16.7 M |
| high street | 13.4 M | 11.2 M | 9.2 M |
| church gate | 23.1 M | 23.1 M | 19.3 M |
| manor gate | 9.5 M | 9.5 M | 5.6 M |
| wide (zoom 28) | 18.9 M | 12.3 M | 18.5 M |

- **The ground** adds 0.5 to 4 M triangles a view.
  - Its whole LOD0 is 297 M, but with per-tile LODs only the tiles within 12 m show 1.5 cm.
  - The v2-ground column also has the full-detail cobbles, which is why the square is so close.
- **Full-detail cobbles** add about 5 to 6.6 M triangles in the square views.
- **GPU medians:** 2.7 to 5.2 ms in every configuration, with no clear difference between them on this machine. The
  editor's main thread (5 to 8 ms) still sets the frame.
- **Shadows:** the church gate's triangles are mostly the kit buildings' shadow casters (about 16 M, round 1).
  - The ground and the cobbles cast no shadows.
  - The cheapest next cut is a simpler shadow version of the buildings (their 6 cm level or the far mesh as the shadow
    caster).
- **Load:** on a warm reload, the ground takes 4.2 s (heights 0.4 s, placing 22,313 tiles 3.8 s) and the kit 1.3 s.
