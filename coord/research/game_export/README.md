# v3 on agent N's game export (agent A, 2026-10-03)

## The switch

- The rigs are copied from `E:\Assets\DND5E\pixel3d\export\game\characters\<folder>\<id>\rig\` into
  `Assets/Game/Resources/Library/Rigs` (`<id>.glb` and `<id>.rig.json`, replacing the round-2 rigs).
  - There are 14 of them: the 11 Chapter One people v2 places, plus the fighter, the ranger and the wizard.
  - The export also has elisabeth_durst, gustav_durst, kolyan_indirovich, mad_mary and morgantha. v2 gives none of them
    a figure (Kolyan is a coffin, Mary a door), so they aren't copied.
- `LibraryFigures.Build`
  - `Body_LOD0/1/2` go into a LODGroup.
    - The switch heights are the export's `screen_height_min` (0.25, 0.08); the last LOD is culled below 0.002.
    - The LODGroup's size is the figure's height. Its renderers' bounds are padded for cloth.
    - The project's `QualitySettings.lodBias` is 2. At the default camera (zoom 14, FOV 38, about 17.6 m away) a
      1.73 m figure measures 0.286, so it shows LOD0; LOD1 appears zoomed out.
  - Dev switches: `LodsOn`, `LodScale`, `ForceLod`, `AtlasShaderOn`.
- **Material:** glTFast's `glTF-pbrMetallicRoughness` material is swapped for `Dungine/VoxelAtlas`
  (`Assets/Game/Resources/Library/DungineVoxelAtlas.shader`), one material per source material.
  - Albedo is the atlas times the COLOR_0 AO, raised to `_VoxAOPower`.
  - Metal comes from the B channel and roughness from G.
  - Glow is the emission map times `_SpecColor`.
  - `_EmissionColor` stays additive for v2's hover highlight.
  - It reads mip 0 only: glTFast generates mips, but the export has none.
- **Global dials:**
  - `_VoxWrap`: wrap diffuse, (N.L + w) / (1 + w);
  - `_VoxFill`: a multiplier on the ambient;
  - `_VoxAOPower`;
  - `_VoxNoShadow` (diagnostic only).
  - `LibraryFigures.Dials(w, f, ao)` sets them. They default to 0.6, 1.25 and 0.7 (`DefaultWrap/Fill/AO`).
  - Front-lit at the game angle, the streaks are the N.L <= 0 step sides. With no shadow at all they look the same, so
    self-shadowing isn't the cause.
- **Springs (`LibraryRigDriver`):** a rig with `SkirtCentre*` chains no longer pulls its leg panels toward the hips.
  The round-2 workaround (`SkirtFrontBackFollow/SideFollow`) is kept only for rigs without centre chains, and only on
  thigh chains, never the `*Low` shin chains.

## The checks

- **Centre cloth in a sit and a crouch (springs and colliders on):** Bildrath's apron and Ireena's dress.
  - The centre chain hangs straight down from the waist between the knees, and the knees stay outside it, so nothing
    shows through from the front.
  - The apron doesn't lie across the lap: from three-quarters the near thigh passes in front of the apron's top.
  - Captures: `DevCaptures/k_*`, `k3_*` and `k4_*` (`tools/cap.sh lib:<id> sit|crouch ...`).
  - The crouch is a dev pose, `HumanoidAnimator.devCrouch` (hips down 0.3 H, knees bent by the leg IK). v2 has no
    crouch.
- **Hem flicks:** gone in the walk (`cloth_walk.gif`). The apron and the dress front hang as one piece, pushed by the
  leg colliders.
- **Cape and the new casts:** the half-cape rides the right arm fully.
  - In cast A it spreads over the chest at the tuck, then sleeves the arm at the release.
  - In cast B and the stab it sleeves the extended right arm.
  - N's suggested fix stands: a short cape chain of its own on the arm.

## Captures

- `DevCaptures/s_*`: the dials, front-lit (`DevCapture.KeyEuler = (48,205,0)`, top view).
- `DevCaptures/l_*`: the LODs, with `LibraryFigures.ForceLod`.
- `DevCaptures/e_cape*`: the cape in the casts.

## Round 2 (2026-10-03)

- **N's cape-arm chains:** the fighter, Ireena, Ismark and the ranger are recopied. Their chains are
  `RightCapeArm`/`LeftCapeArm`, on the shoulder, with the `arms` collider group. No code change was needed.
  - Checked with the real springs:
    - Cast A, side, game and front views (`DevCaptures/n_fA_*`): the arm comes out from under the cape, with no red
      sleeve. At the tuck the cape stays at the right side and doesn't slide across the chest (front view, f009 and
      f012).
    - Cast B (`n_fB_*`): the cape stays on the shoulder, its hem lifted a little by the arm.
    - The stab (`n_fS_*`): the cape drapes over the top of the horizontal upper arm, not along the forearm.
    - Ireena's cast A, with her left-arm cape over the steadying arm (`n_iA_*`): it hangs beside the arm.
    - Ismark's stab, with both sides (`n_mS_*`).
  - Before and after: `cape_fixed.gif` and `cape_fixed_push.gif`; stills in `cape_tuck_front.png`.
- **Static figures:** the export's 15 static models (8 figures and 7 props) are copied into
  `Assets/LookTest/Resources/LookTest/Export` (`<id>.glb` and `<id>.json`).
  - `LookTest.Place` uses them first (`UseExport`, on by default), falling back to the old bake. The road view's 5 come
    from the export.
  - `LibraryFigures.PrepareStatic` gives them the Dungine/VoxelAtlas material and a LODGroup sized to the model.
  - The bake stays for the weapons and held things (cup, jug, sack), which the export doesn't carry.
  - Capture: `road_figures.png` (`Assets/Captures/road_bake.png` and `road_export2.png`).
