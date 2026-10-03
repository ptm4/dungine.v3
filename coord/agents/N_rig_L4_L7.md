# Agent N: the rig, steps L4 (joints) and L7 (soft chains)

Status: L7, L4 and review 17 done 2026-09-30; round 2 running (clipping in poses, from Peter's screenshots), resumed 2026-09-30.

## Brief (the prompt as given)

You're continuing the rig work on Peter's voxel "3D pixel art" asset library, `E:\Assets\DND5E\pixel3d`. Agent I built
steps L1 to L3 of the animation plan. There are rigged GLBs for the 16 Chapter One figures and the heroes fighter,
ranger and wizard, in `characters\<folder>\<id>\rig\`. The main session checked them in Blender:
- they import with 23 bones and 8 socket empties;
- the texture atlas maps correctly, and posing works.

Two known gaps showed:
- a long skirt, split per thigh, opens when a leg steps and shows the bare leg painted under it;
- the zone-painted shawl sits like a band on the upper arms.

Your job is steps L7 (soft chains) and L4 (joints), in that order, as agent I recommended. Peter: "keep working
continuously ... unless I stop you on a part keep going".

**Read first, fully**
- `E:\Unity\Projects\dungine.v3\coord\research\animation_pipeline.md`: sections 1.3 to 1.5 and 5.1 (L4, L7) are your
  spec. Use its recommended defaults: rigid ball joints; only the trunk, neck, collarbones and toes blend; capes, skirts,
  long hair and tails on their own chains.
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`, especially agent I's rig lessons (2026-09-30) and workflow
  item 8.
- Agent I's code: `pipeline\kit\rig.py` and `pipeline\rig_export.py` (`--all`, `--only`, `--out-root`, `--fit`,
  `--check`), and its fit sheets and rest-stance report in `C:\Users\ptm\AppData\Local\Temp\dnd5e_agentI\fit\`.

**Your steps**
1. **L7, soft chains.**
   - Skirts, robes and long coats get a chain of bones, front and back, or a ring. They bend with the legs, with smooth
     weights, so a step lifts the cloth instead of splitting it.
   - Capes and cloaks get a chain from the shoulders, long hair a chain from the head, tails a chain.
   - Put the spring settings (stiffness, damping, gravity) in the extras, as the plan says, for the game to use.
   - Legs under a skirt are painted in the skirt's colour in the rig build only, so a gap never shows skin (the plan's
     hidden-seam dressing).
2. **L4, joints.**
   - Cuts through the joints and hidden cores: ball-joint caps that show solid voxels when a joint bends.
   - Soft bands on the trunk and neck.
   - Fix the rest-stance part breaks agent I listed (the shawl wrapping the arms, waistcoat and bodice armholes on broad
     bodies, the hand notches in skirts, Kolyan's belt cord, the wizard's satchel, Ismark's cloak), but only in rig
     builds.
     - Add a flag the parts can read, for example `c.data['rest_build']`, set by `rig.rest_recipe` or the rig build, so
       the library's approved stills stay identical, voxel for voxel.
     - If a fix needs a small change in a shared kit part, keep it behind that flag and prove the stills are unchanged.
3. **The stress sheet,** for Peter to judge the joints:
   - six builds (fighter, wizard, Ireena, Mad Mary, Parriwimple, Thorn) in eight hard poses: arms overhead, a deep lunge,
     a crouch, the head turned 70 degrees, a big stride, a sword raised, a sit, a reach behind;
   - rendered with your numpy skinning renderer as PNGs;
   - on a page at `pixel3d\reviews\17_rig_stress\index.html`, which says what's rigid, what bends and what's still to do,
     with questions for Peter (ball joints or soft bends? how much skirt swing?).
4. **Re-export** all 19 rigged GLBs, re-run the checks (`--check`), and update the lessons and workflow notes (you may
   add a dated block to `3dpixelartlessons.md`).

**Rules**
- Voxel outputs of the library's approved stills must not change. Regression:
  - the heroes via `python -m kit.build recipes\characters\heroes\<id>.json --out <tmp>` and `kit_diff.py`: 0
    differences;
  - all 16 review 9 figures via `python village_review.py --what figures --out-root <tmp>`, compared npz for npz with
    the library: identical;
  - the two skirted commoner looks via `python -m kit.build recipes\characters\looks\barovian_commoner_f.json --out
    <tmp>`, and `_2`.
- Never run Blender; the main session checks your GLBs in Blender. No downloads.
- Don't edit other agents' files: `kit\props*.py`, `kit\adventuring_gear.py`, `kit\creatures.py`, `kit\v2_loot.py` and
  their review scripts.
- Don't touch `E:\Unity\Projects\Dungine`, never modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`
  (skip it in any search).
- Never mark anything approved.
- Write in plain, direct English in anything Peter reads.

**Final reply:** what's done, the stress sheet path, file paths, the regression results, and doubts.
