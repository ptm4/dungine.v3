# Rigged library figures in v3 (phase 3, agent B's step G2)

Agent A, 2026-10-03. Open `index.html` for the pictures.

## What was built

- **Rigged figures in v3:** `Assets\Game\Resources\Library\Rigs\`. These are copies of the library's rig exports
  (`<id>.glb` plus `<id>.rig.json`) for Bildrath, Arik, Ireena and the fighter hero. glTFast imports them with their
  skins:
  - bones carry the humanoid names and rest unrotated;
  - the mirror from glTF to Unity puts the figure's left at -X, as in v2;
  - the atlas textures are point-sampled.
- **`Assets\Game\Scripts\Library\`**:
  - `LibraryFigures.cs` fills v2's `HumanoidRig` from a library figure: the bones, their rest positions, the sockets
    (`LookFrom` becomes v2's `Eyes`) and the size. v2's `HumanoidAnimator` then drives it unchanged.
    `LibraryFigures.ByNpcId` says which v2 figure becomes which library figure; `ActorFactory` checks it (two lines
    changed).
  - `LibraryRigDriver.cs` runs after v2's animator and does three things:
    - splits v2's Chest turn half and half with the library's UpperChest;
    - lays the figure's own posture (`base_pose`) over the trunk, neck and head;
    - runs the spring chains, using the VRM parameters from the `.rig.json` and its sphere and capsule colliders.
  - `Stroll.cs`: a villager walks a loop on the area's navmesh with v2's own `Actor.MoveTo`. v2's villagers stood still.
  - `MiniJson.cs`: reads the `.rig.json`.
- **v2's capture tool takes library figures:** `bash tools/cap.sh lib:<id> walk <name> side 48` (in `DevCapture.cs`).
- **The look test** (`Assets\LookTest`) now:
  - assigns `villager0` in the village to Bildrath's figure and starts its walk;
  - turns off the static figure row in the square (`LookTest.VillageLineup`);
  - adds `LookTest.Clip(...)`, which records the game view at a fixed 1/24 s per frame.

## What moves, and how well

- **Walk, run, idle, cast, strike and fall** all play on the library figures as on v2's own. Feet stay planted by v2's
  leg IK, and the arms swing. A strike is empty-handed for now.
- **Rest pose:** it matches v2. The upper arms are 11.0 degrees out on all four figures. The forearms are 3.2 degrees
  out where v2's continue the same 11-degree line, so the elbows are a touch bent at rest. That changes nothing visible.
- **Posture:** the figure's own pose rides over v2's motion. Arik keeps his bowed head while idling and walking.
- **Springs:**
  - Hair, capes and skirts swing.
  - They're simulated in the figure's own space, so walking doesn't trail them behind like flags; turning, the gait's
    bob and the legs still move them.
  - Skirt panels hang between their thigh and the hips: front and back 70% toward the hips, sides 40%. The settings are
    `LibraryRigDriver.SkirtFrontBackFollow` and `SkirtSideFollow`.
- **In the village:** Bildrath's figure walks round the square on v2's navmesh at 1.3 m/s, pausing to look about. The
  only console message is the dev tool's usual boot timeout.

## Problems with the rigs (for agent N)

1. **Cloth that spans both legs splits.** Aprons, dress fronts and robe fronts are weighted to a panel on each thigh
   and shin, so at each step the cloth stretches into a sheet between the legs (`apron_before_after.png`, middle).
   - The game now hangs the panel tops and the knee-level panels part way to the hips, which mostly hides it.
   - The real fix belongs in the export: cloth that spans both legs should hang from centre-front and centre-back
     chains on Hips, and let the leg colliders push it.
2. **The hem's lowest voxels still flick out** as thin light slivers on long aprons, even with the fix above.
3. **A cape lying over an arm rides that arm 100%,** so a raised arm lifts that part of the cape like a red sleeve (the
   fighter's cast). Perhaps share it, half on the arm and half on the cape chain.
4. **Not tested yet:** held props on the hand sockets, and v2's weapons on the library's socket frames. The library
   sockets put a prop's +Y along the business end, as v2's weapons are built, so they should line up.
   - Fixed in the game, not the rig: glTFast's material names its glow `emissiveFactor`, not `_EmissionColor`, so v2's
     hover highlight missed library figures. `Actor` now sets both, and the highlight shows (tested).
5. **What's good and can stay:**
   - the bone names, the unrotated rest and the 11-degree arms;
   - sockets as nodes, the spring parameters and the colliders;
   - no ground disc, metal in the texture, and point sampling.

   Triangles: Bildrath 21,630, Arik 25,592, Ireena 38,936, the fighter 33,326. That's lighter than the static
   `model.glb`s.

## Next steps

- Assign more of Chapter One's people (`LibraryFigures.ByNpcId`) as their rigs arrive, each in their own area.
- Hold props and weapons: library weapon GLBs on `SocketHandR`, `SocketHandL` and `SocketBack`, with v2's
  two-handed IK.
- Move the rigs to the `Dungine/Voxel` shader (or teach it the atlas) once Peter picks the look; the occluder's dither
  is only needed on buildings, so figures don't need it.
- Measure a crowd of rigged library figures against v2's.
- Re-check the cloth with agent N's next export.
