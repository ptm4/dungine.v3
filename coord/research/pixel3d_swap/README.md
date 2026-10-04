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
