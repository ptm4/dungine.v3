# The library's village in v3 (agent A, 2026-10-04)

The kit village is agent E's layout (review 36) built from agent N's game export. It stands in for v2's village
buildings. The page for Peter is `index.html`.

## Re-running it

The village is placed from the JSON every time the area is built (`Library/VillageKit.cs`), so a new layout or new
modules only need copying in:

- **The layouts:** copy `export/game/landmarks/village_of_barovia/village_set/village_layout.json` and
  `village_layout_night.json` to `Assets/Game/Resources/Library/Village/`.
- **The far meshes:** copy `far/*.glb` and `far_night/*.glb` to `.../Village/far/` and `.../Village/far_night/`.
- **The modules:** for each placement, copy `export/game/<glb>` (and its `.json`) to
  `.../Village/<kit>[_2]/<module>[_2].glb`. That's 315 modules, 490 MB. The copy is the Python snippet in the
  session's notes: one loop over both layouts' placements.
- **Refresh:** glTFast imports everything in about 2 minutes.

Nothing else has to change unless the layout's building ids or v2's plots change. Each kit building is matched to v2's
by `v2_centre_cm` (within 1.5 m).

## How it fits into v2's village (`AreaVillage.Build`)

1. After v2's terrain, `VillageKit.Begin` places every module.
   - Each building stands at its own ground height. Site pieces each stand at the ground under them.
   - The layout is x east, y north in cm. A module goes at `(x, ground + z, y) / 100`, turned
     `Euler(0, 180 - 90 * quarter, 0)`. The half turn undoes glTFast's X mirror.
   - This matches N's `position_m` and `yaw_deg` with glTF's z flipped.
2. v2's buildings are still built. `VillageKit.Adopt(info, v2Centre, v2Out, label)` then:
   - switches each one off;
   - moves its `BuildingInfo.doorWorld` to the kit's front door: a door leaf facing the way the building faces,
     pushed out to the front of the wall modules beside it, plus v2's own offset (houses 1.2 m, church 2 m, manor
     3.05 m).
   - Everything v2 sets from the door point follows: the door trigger, the area's spawn, the knock doors, and the
     barrels, bench and crates by the tavern and shop doors.
3. `VillageKit.Finish` runs before the navmesh is baked:
   - it hides v2's fences, walls, gateposts, well, stalls, notice board and statue, which the kit replaces. They are
     kept invisible so their colliders and uses (the well, the notices, the stall crate) still work.
   - Anything inside the reserved plot is left exactly as v2 has it.
   - It destroys the retired buildings.
   - It registers the kit buildings with v2's see-through system (`Occluders`).
4. **Colliders:** each kit building gets box colliders from its ground-floor modules, up to 3 m. That covers the
   navmesh, the camera and clicks.
5. **The square's cobbles** stay v2's for now (`VillageKit.PlaceSquare = false`). The kit's cobbles are 11 M
   triangles at LOD0 and would double the site cost. v2's ground, roads, lamps, fog, trees, interiors and gameplay are
   unchanged.

## Detail levels

- **One LODGroup per building and per 24 m site tile (N's far groups):**
  - the 1.5 cm modules nearer than 24 m;
  - 3 cm to 55 m;
  - 6 cm to 110 m;
  - then the group's far mesh (one merged 15 cm model, one draw) in place of the modules' own 15 cm level.
- `VillageKit.LodDistances` converts these distances to LODGroup heights for the camera's 38-degree field of view and
  `lodBias` 2.
- At play zoom the camera is about 18 m from the party, so the buildings round the party show 1.5 cm and the rest
  drop away.
- The far meshes stand at the building's ground height. For site tiles they stand at the tile's middle; tiles whose
  cobbles aren't placed keep the modules' own 15 cm level.
- Dev switches: `On`, `PlaceSite`, `PlaceSquare`, `FarMeshes`, `LodDistances`.

## Shading

- **New shader `Dungine/VoxelAtlasOccluder`:** a twin of `Dungine/VoxelAtlas` with v2's screen-door holes.
  - The forward pass is shared through `VoxelAtlasForward.hlsl`.
  - `Occluders.Swap` uses it for VoxelAtlas materials and makes one twin per source material per building. A kit house
    has hundreds of renderers and a handful of materials.
  - The roof map is read from the far mesh, which keeps registration at about 0.3 s for the village.
- **Earlier the same day (commit 0de8ce0):** the voxel shaders now compile their own shadow-caster pass and have no
  URP Lit fallback.
  - In a fresh editor, `UsePass` from URP Lit made the GPU Resident Drawer reject URP Lit itself ("variant shared by
    inconsistent other shader fallback").
  - The result was that every v2 material drew magenta.

## Doors and spots

All 22 doors v2's village uses were checked, with the kit on and the kit off:

- 17 knock doors;
- the house on the narrow lane;
- the tavern, the shop, the manor and the church;
- the reserved plot's door.

Results:

- **Door points:** every door point moved to the kit's door, by 0.5 to 3.2 m. They are logged in
  `VillageKit.DoorLog`.
- **Reachability:** every door has a navmesh point within its use range, reachable from the square.
  - With v2's own buildings, 5 of the knock doors weren't. So the kit fixes 5 and breaks none.
- **Walked through:** the party walked into each of the tavern, the shop, the manor and the church and arrived in that
  interior. Coming back out lands at the moved spawn in front of the kit door.
- **What sits inside the kit's colliders:** no NPC, spawn or interactable. The only thing there is the manor's scored
  door note, which sits on the kit's manor door, where it belongs.
- **v2's other objects:** none of the barrels, benches, crates, carts, lamps, trees and the rest overlaps a kit building by more than 15 cm.
- **Nothing needs moving by hand.** v2's knock doors stay working without changes, because they're built from the
  moved door point.

## Frame time

- **Method:** village square, high street, church gate, and a wide view at zoom 28. Default camera, 600 frames with
  vsync off (`tools/lookmeasure.sh`), the kit on and off, paired.
- **Noise:** this machine runs other agents. Means for the same view swing 5 to 12 ms between runs, so the frame-time
  medians only bound the difference.
- **What is solid:**
  - triangles per frame: about 1.5 to 1.8 M with v2's village; with the kit, 9.6 to 10.1 M in the square and street
    and 19.3 M at the church gate;
  - draw calls rise by about 20%.
  - At the church gate about 16 M of the 19.3 M triangles are shadow casting. Turning the kit's shadows off there
    dropped it to 2.8 M.
- **The GPU medians** are in the table below.

GPU frame time, medians of the profiler's "GPU Frame Time", two paired runs each (`measurements.txt`):

| View | v2's village | the kit |
|---|---|---|
| square | 2.54 / 2.58 ms | 3.11 / 3.06 ms |
| high street | 1.69 / (0.36) ms | 2.74 / 3.64 ms |
| church gate | 1.66 / 2.42 ms | 2.83 / 2.62 ms |
| wide (zoom 28) | 2.09 / (0.27) ms | 3.42 / 4.63 ms |

(The two very low v2 readings are likely missed GPU samples.)

- The kit adds about 0.5 to 2 ms of GPU time a frame.
- In the editor the frame is limited by the main thread (5 to 12 ms either way), so the visible frame time hardly
  changes. In a build the GPU share matters more.
- Building the area: 0.8 s on a warm second load. The first load in a fresh editor session took 7 to 15 s while
  Resources loaded 315 modules and 42 far meshes; most of that is import caching, so measure it in a build.

The next cuts, in order:

1. a lower shadow LOD (the far mesh or the 6 cm level as shadow caster);
2. N's `--no-ao` export (LOD0 halves) with SSAO;
3. tighter `LodDistances`.
