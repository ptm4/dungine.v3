# Agent I: the rig export, steps L1 to L3 of the animation plan

Status: done 2026-09-30. 19 rigged GLBs (the Chapter One people and three heroes) in the library under characters/<folder>/<id>/rig/.

## Brief (the prompt as given)

You're turning the voxel characters of Peter's asset library, `E:\Assets\DND5E\pixel3d`, into rigged glTF files ready to
animate. You're implementing the first library-side steps of the approved-for-work animation plan (Peter: "keep working
continuously ... unless I stop you on a part keep going"). Several agents work in parallel with the main session, so
follow the rules exactly.

**Read first, fully**
- `E:\Unity\Projects\dungine.v3\coord\research\animation_pipeline.md`: the plan. Sections 1 (the skeleton, skinning,
  joints, soft parts, props, mesh size) and 5.1 (steps L1 to L7) are your spec. Use its recommended defaults until Peter
  decides otherwise:
  - the rest stance is the kit's hang stance (arms about 11 degrees out, as v2's rig);
  - joints are rigid ball joints; only the trunk, neck, collarbones and toes blend;
  - outfits are baked (one whole build per outfit).
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`: the style bible and every lesson, including posing.
- The code:
  - `pipeline\kit\core.py`: `build`, and `Grid.owner`, which records which recipe part painted each voxel.
  - `pipeline\kit\pose.py`: the segment labels, owner rules and weights.
  - `pipeline\kit\bodygen.py`: joint positions `P`.
  - `pipeline\vox.py`.
  - `pipeline\rig_export.py`: the main session's v0. A pure-Python GLB writer on a 19-bone skeleton with rigid weights,
    checked in Blender: it imports with 19 bones and poses correctly. Capes and shawls follow the chest (`DRAPE`).
  - `pipeline\kit\weapons.py`: `TWO_HAND` grips.
  - `pipeline\village_review.py`: the Chapter One figures.
  - `pipeline\race_review.py` and `pipeline\recipes\characters\heroes\*.json`: the heroes' recipes.

**Your steps**
1. **L1, the rig data.**
   - Grow the plan's 24-bone skeleton from `P`: the collarbone roots, UpperChest and the balls of the feet are new; the
     names are exactly Unity's and Godot's humanoid names.
   - Give every voxel one bone, following pose.py's owner rules: head parts go to Head, held parts and world props come
     out, capes and shawls go to the chest.
   - Put this in a new module, `pipeline\kit\rig.py`. Import from `pose.py` if useful, but don't change it: the review 9
     posed figures must stay identical.
2. **L2, the game builds (a first cut).**
   - A build mode in the rest stance: arms in the hang stance, no `pose`, no arm overrides, no ground disc, no mannequin,
     no world props, no held props.
   - Fit sheets: numpy flat projections, front and side, of every Chapter One figure (the 16 in `village_review.py`) and
     the heroes fighter, ranger and wizard, in that stance.
   - List the kit parts that break in the rest stance (for example zone-painted shawls, or hands painted over hilts).
     Don't fix shared parts; report them.
3. **L3, the glTF writer.**
   - Extend `pipeline\rig_export.py` to the 24-bone skeleton, with merged faces per bone as the plan's section 1.7 says:
     greedy rectangles with a point-sampled texture atlas holding colour, and emission for glowing voxels.
   - Add the skin, the sockets (the plan's names) and a `rig.json` beside each GLB (bones, parents, joint positions,
     sockets, the owner rules used).
   - Keep plain glTF 2.0: no engine-specific extras beyond what the plan specifies.
   - Validate structurally in Python: accessor bounds, index ranges, weights summing to 1, the joint hierarchy, and the
     byte alignment of every bufferView.
   - Export every Chapter One figure and the three heroes to `pixel3d\characters\<same folder as the figure>\rig\<id>.glb`
     and `<id>.rig.json`.
   - Skip the locked key characters, Arkus and Chai'rn (`paths.LOCKED`): Peter hasn't been asked about rigging them.
   - Report the triangle counts before and after merging.

**Rules**
- Never run Blender. The main session validates your GLBs in Blender afterwards; only it runs Blender, one at a time.
- No downloads. The Khronos validator would need one, so do the structural checks in Python instead.
- Don't change `kit\core.py`, `vox.py`, `kit\pose.py` or any existing kit part. Voxel outputs must stay identical. Run the
  regression at the end:
  - rebuild the three heroes to a temp folder with `python -m kit.build recipes\characters\heroes\<id>.json --out <tmp>`
    and compare with `kit_diff.py`: 0 differences;
  - rebuild two posed review 9 figures (`python village_review.py --only arik,morgantha --what figures --out-root <tmp>`)
    and compare their npz with the library: identical.
- Don't edit other agents' files: `kit\props.py`, `props_review*.py`, `kit\props_religion.py`, `religion_review*.py`,
  `kit\adventuring_gear.py`, `gear_review*.py`, `kit\creatures.py`, `creature_review*.py`.
- Don't touch `E:\Unity\Projects\Dungine`, never modify `E:\Unity\Projects\dungine.v2` (reading it is fine: its
  `HumanoidRig` shows the bone mapping the plan describes), and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Test in `%TEMP%\dnd5e_agentI\` until it works, then export into the library.
- Write in plain, direct English in anything Peter reads.

**Final reply:** what works, file paths, triangle counts, the list of parts that break in the rest stance, anything you
doubt, and the regression result.
