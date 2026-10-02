# The animation pipeline: from voxel figures to animated characters

Agent B, 2026-09-30. A proposal for Peter and the main session. Nothing here is decided until Peter says so; the open
choices are in section 6. Spoiler-free: no named module characters, places only as the party first meets them.

## The recommendation in brief

1. **One file per character: a glTF 2.0 skinned mesh** (`.glb`) on a humanoid skeleton of 24 bones named exactly as in
   Unity's and Godot's humanoid profiles (`Hips`, `Spine`, `LeftUpperArm` …). Mixamo and Blender map onto it by a fixed
   table. Any engine loads it; the library stays engine-neutral.
2. **The skeleton is the kit's skeleton**, grown from the joint positions `P` that already shape every body and extended
   from the 15 segments in the new `kit/pose.py`. The rig rests in the kit's hang stance (arms 11° out), which is the same
   rest pose as v2's rig.
3. **Hard things rigid, soft things may bend.** Limbs, hands, feet, the head, armour and props follow one bone per voxel,
   so voxels never stretch. Joints work like an action figure's ball joints: a hidden, rounded core fills the gap when an
   elbow bends. Only the trunk, the neck, the collarbones and the toes blend smoothly, where the angles are small. Capes,
   skirts, long hair and tails blend on bone chains of their own and swing on springs.
4. **A pure-Python exporter writes the file**, so no Blender is needed. It merges faces and moves colour into a small,
   crisp texture, which gives about a third of today's triangle count (the fighter goes from 48,800 to about 17,000).
5. **Weapons and held props are separate files that plug into sockets** on the hands, back, hips, forearm and head, with
   grip points the kit already knows. Carts, doors and chests are hinged node trees (wheels, lids, leaves); they aren't
   skinned.
6. **v2 already animates humanoids in code:** a gait with feet planted by IK, idles, stances for seven grips, keyed attacks,
   casts, hits, falls, look-at, sitting and talking, plus a four-legged animator. The fastest way to see voxel figures move
   is to feed that animator our skeleton. If the engine stays Unity, it's close to a drop-in.
7. **Clips for the long tail, free first:** Mixamo and Quaternius's CC0 libraries for gestures, emotes, interactions and
   village work, plus our own **scripted key-pose clips** for faceless body language and the Barovian reactions to
   non-humans. Agents write these as data, and Peter reviews them in the Asset Library. Buy packs only to fill gaps.
8. **UniMate (Peter's question) is worth a short trial, not a place in the pipeline.** It needs our rig first. It could
   fill in motion between our key poses and draft reactions that stock packs lack. Its licence is a grey zone and it is
   brand new research code. See section 3.
9. **The first playable slice needs about 80 clips.** About 40 already exist in v2's code or come from runtime IK and
   look-at, about 25 come from free libraries, and about 16 we script.
10. **Effort:** about 15–22 agent-days on the library side (part fixes included) and 18–27 on the game side. The first
   moving voxel party can be on Play after about 9–13 of those days.

---

## 1. From voxel figure to animated character

### 1.1 What the library exports

One `.glb` per character build, written next to today's `model.glb` (for example as `rig.glb`). It contains:

- **One skinned mesh.** It holds the figure's visible voxel faces, merged per bone, with flat normals. It uses one
  material with point-sampled textures: base colour, emission (for glows and runes) and metal/roughness. Nearest-neighbour
  sampling keeps each voxel face a crisp block of colour.
- **The skeleton.** It has 24 humanoid bones plus the extra chains the figure needs (cape, hair, skirt, tail). The rest
  pose is the bind pose, and no bone carries a rotation at rest.
- **Sockets.** These are plain nodes under the bones, where props attach (section 1.6).
- **Extras**, as JSON inside the file and also written out as `rig.json`. They hold the voxel size, the recipe id, each
  bone's role (rigid, soft band or chain), the spring settings of the chains, the socket frames, and the T-pose rotations
  for tools that want a T-pose. They also hold the figure's base pose from its brief, such as a stoop or a bowed head.
  The game applies that pose as a layer, so the figure keeps the posture Peter approved.
- **No animations.** Clips live in their own files (section 2.5), so every figure shares them.

Units and axes follow glTF: metres (1 voxel = 0.015 m), +Y up, and the figure facing +Z. The kit's grid maps across as
(x, z, −y) × 0.015. That is a proper rotation, so nothing gets mirrored.

**Stills and the game use the same skeleton for two jobs.** The library's posed stills stay re-voxelised, as `pose.py`
does now, so their cubes stay on the grid. In the game the cubes turn with their bones, as in Hytale.

### 1.2 The skeleton: names and hierarchy

The names are Godot's `SkeletonProfileHumanoid` names, which are also Unity's `HumanBodyBones` names. So both engines
map them automatically, and Blender mirrors them, because it flips names that begin with "Left" or "Right". The right
side mirrors the left. "Placed at" means the kit joint the bone grows from; the fighter's heights are in voxels.

| Bone (ours = Godot) | Parent | Placed at (from `P`) | Unity Humanoid | Mixamo | Blender (Rigify metarig) | v2 rig |
|---|---|---|---|---|---|---|
| Root | – | the ground under the pelvis | model root (not mapped) | – (root motion rides on Hips) | root | Root |
| Hips | Root | `hips_c` (64) | Hips *(required)* | mixamorig:Hips | spine | Hips |
| Spine | Hips | the kit's stoop pivot, just above `hips_c` (66) | Spine *(required)* | mixamorig:Spine | spine.001 | Spine |
| Chest | Spine | `waist_c` (75) | Chest | mixamorig:Spine1 | spine.002 | Chest |
| UpperChest | Chest | top of the kit's stoop ramp (86) | UpperChest *(optional)* | mixamorig:Spine2 | spine.003 | – (shares Chest) |
| Neck | UpperChest | `neck_a` (97.5) | Neck | mixamorig:Neck | spine.004 | Neck |
| Head | Neck | `neck_b` (105.5) | Head *(required)* | mixamorig:Head | spine.006 | Head |
| LeftShoulder | UpperChest | new: 2 voxels off the centre line, just under shoulder height | LeftShoulder | mixamorig:LeftShoulder | shoulder.L | ClavL |
| LeftUpperArm | LeftShoulder | `shoulder_L` | LeftUpperArm *(required)* | mixamorig:LeftArm | upper_arm.L | UpperArmL |
| LeftLowerArm | LeftUpperArm | `elbow_L` | LeftLowerArm *(required)* | mixamorig:LeftForeArm | forearm.L | LowerArmL |
| LeftHand | LeftLowerArm | `wrist_L` (`hand_L` is the grip) | LeftHand *(required)* | mixamorig:LeftHand | hand.L | HandL |
| LeftUpperLeg | Hips | `hip_L` | LeftUpperLeg *(required)* | mixamorig:LeftUpLeg | thigh.L | ThighL |
| LeftLowerLeg | LeftUpperLeg | `knee_L` | LeftLowerLeg *(required)* | mixamorig:LeftLeg | shin.L | ShinL |
| LeftFoot | LeftLowerLeg | `ankle_L` | LeftFoot *(required)* | mixamorig:LeftFoot | foot.L | FootL |
| LeftToes | LeftFoot | new: the ball of the foot, about 60% of the way from `ankle_L` to `toe_L` | LeftToes *(optional)* | mixamorig:LeftToeBase | toe.L | ToeL |

```
Root
└─ Hips
   ├─ Spine ─ Chest ─ UpperChest
   │                  ├─ Neck ─ Head ─ (Hair0-2, sockets HeadTop and LookFrom)
   │                  ├─ LeftShoulder ─ LeftUpperArm ─ LeftLowerArm ─ LeftHand ─ (SocketHandL)
   │                  │                                └─ (SocketShield)
   │                  ├─ RightShoulder ─ … ─ RightHand ─ (SocketHandR)
   │                  └─ (Cape0-3, SocketBack)
   ├─ LeftUpperLeg ─ LeftLowerLeg ─ LeftFoot ─ LeftToes
   ├─ RightUpperLeg ─ … ─ RightToes
   └─ (Tail0-3, skirt panels, SocketHipL, SocketHipR)
```

Notes:
- **Spine, Chest and UpperChest sit evenly along the kit's stoop ramp.** `pose.py` bends the belly between about height 66
  and 86 on the fighter. If each of the three turns a third of a stoop, the game repeats the library's bend.
- **No finger bones.** The kit's hands are mittens. Finger tracks in stock clips are simply ignored.
- **Extras not in any profile:** the chains (`Tail0-3`, `Cape0-3`, `Hair0-2`, skirt panels) and the sockets. Unity's
  Humanoid ignores them and Godot keeps them. Their motion comes from springs or from our own clips. The tail and cape
  names match v2's.
- **Unity's 15 required bones are all present:** Hips, Spine, Head and the arm and leg chains. The rest are optional
  there, and all of them map.
- **Rigify:** its generated deform rig splits each limb into two twist segments. Our rigid limbs don't want that, so for
  keying in Blender use our skeleton directly and add IK controls (section 2.4).

### 1.3 Rigid or blended skinning

"Skinning" is how a mesh follows its bones. Each vertex has one or more bones and a weight for each.

| | Rigid (one bone per voxel) | Blended (a voxel shares several bones) |
|---|---|---|
| The voxels | stay perfect cubes | near joints they shear and stretch into slanted blocks, a rubbery look |
| At the joints | gaps and overlaps open when a joint bends, unless treated (section 1.4) | smooth, no gaps |
| The look | toy-like and crisp: Hytale, Minecraft | a smooth low-poly body with a pixel texture |
| Cost | the cheapest skinning there is: one influence per vertex | up to four influences |
| Best for | limbs, head, hands, feet, armour plates, props | soft things (cloth, hair, tails) and small bends (the trunk, the neck) |

**How blocky games do it.**

| Game | Bodies | Joints | Animation |
|---|---|---|---|
| Minecraft (Java) | six boxes: head, body, two arms, two legs | none at elbows or knees; boxes overlap at the shoulders and hips to hide gaps | code: limbs swing by a cosine of the distance walked |
| Minecraft (Bedrock), Blockbench | boxes grouped under bones | overlap | keyframes in JSON, small expressions for procedural touches, state machines |
| Hytale | cubes and quads only, under named bones; limbs in two parts (shoulder → elbow → hand) | no weight painting; parts overlap, and a box may be stretched 0.7–1.3× to fit | keyframes stored as offsets from the rest pose (`.blockyanim` JSON); clothes and cosmetics are separate pieces on the bones |
| Veloren, Cube World (voxel RPGs) | each body part its own voxel model; hands and feet float free | none | all in code (Veloren's animation crate) |

All of them are rigid. They hide gaps with overlap, and their style comes from snappy key poses, not motion capture.
Code-driven animation is normal in the genre.

**Recommendation:** rigid for hard parts, blended only where bends are small or the material is soft. Our figures are one
sculpted voxel mass, not separate boxes, so the export cuts the mass into rigid parts and treats the joints as described
next.

### 1.4 Keeping voxels crisp at the joints

1. **Cut at the joint.** Each limb voxel belongs to exactly one bone, and the cut between two bones is a plane through
   the joint. That's better than the jagged nearest-bone boundary, because a straight cut reads as a deliberate joint
   line, as on an action figure.
2. **The kit's limbs are already ball-jointed.** Each limb is a capsule whose rounded ends are centred on its joints, and
   the shoulder has a sphere on the joint. A sphere turning about its centre stays in place, so the bare body never opens
   a gap. Clothes and armour painted over it can.
3. **A hidden core fills what clothes open.** At each elbow, knee, shoulder and hip, the parent side gets a sphere of
   extra voxels one voxel inside the surface, coloured from the surface around it (the sleeve, not the skin). At rest
   it's hidden, and nothing overlaps at the surface, so nothing flickers. When the joint bends, a rounded knuckle in the
   garment's colour shows instead of a hole.
4. **Soft bands only where angles are small.** The trunk (Hips to UpperChest), the neck, the collarbones and the balls of
   the feet get a band a few voxels wide with smooth weights, meshed with shared corners. A 30° stoop spread over the
   trunk distorts voxels by only a few per cent. The rest of the figure stays rigid.
5. **Dress the hidden seams.** Motion exposes places that were hidden in the build: armpits, inner thighs, the crotch,
   under the chin and under hair. Voxels there take the colour of the nearest visible surface of the same part. Without
   this, a raised arm shows a skin-coloured patch on the side of a shirt.
6. **A rest stance with clearance, and limits.** The hang stance keeps the hands clear of the thighs. The animation layer
   keeps elbows and knees under about 140° and head turns within about ±70°. No bone ever scales.
7. **Crisp textures.** Point sampling with no blur between voxel faces. Each face keeps its own flat colour, even when the
   cube is turned.
8. **Rejected:** smooth-skinning the whole figure (rubbery voxels), and re-voxelising every frame in the game (heavy, and
   voxels flicker as they snap).

### 1.5 Hair, skirts, cloaks, capes and tails

These are soft, so they may bend: smooth weights along a short bone chain, and a spring at runtime.

| What | Bones | Weights | Motion |
|---|---|---|---|
| Short hair, beards, ears, horns | Head | rigid | with the head |
| Long hair on the back | a band between Head and UpperChest (as `pose.py` does now); `Hair0-2` for braids and ponytails later | smooth | the band bends when the head turns; chains swing on springs |
| Mantles, short capes | `Cape0-3` down the back, under UpperChest (as v2) | smooth along the chain | spring, with colliders on the back and legs |
| Long cloaks | two chains, `CapeL0-3` and `CapeR0-3`, so the hem can twist | smooth | spring and colliders (later) |
| Skirts, robes, long coats | first: smooth weights to Hips and both thighs by position; later: 4–8 panel bones on Hips | smooth | panels follow the thighs, plus a spring |
| Tails (tieflings) | `Tail0-3` on Hips (as v2) | smooth | spring, or keyed sway |

The spring settings (stiffness, drag, gravity, collider radius) travel in the file's extras and use the parameter names
of VRM's spring bones, a published glTF extension. Godot's `SpringBoneSimulator3D` reproduces that same spring. In Unity
it's a short script, or Magica Cloth 2 (€32, often half price). The library only describes the springs, so it stays
engine-neutral.

### 1.6 Held props, weapons and rigid props

**Held things are separate files, never painted into the body.** The kit already separates them: `pose.py` lists the held
and back-worn parts (`held_weapon`, `basket_held`, `clay_pipe_held`, `open_spellbook`, `walking_stick`, `shield_arm` and
the rest), and every weapon is built along a frame with its grip at the origin. The export writes each prop as its own
`.glb`. The origin sits in the fist, +Y points along the business end (the kit's `ax`) and +X along the edge (`sd`).
Attaching a prop is then just parenting it to a socket.

| Socket | On bone | What goes there |
|---|---|---|
| SocketHandR, SocketHandL | RightHand, LeftHand | weapons, staves, tools, torches, cups, pipes, books |
| SocketShield | LeftLowerArm | a shield strapped to the forearm |
| SocketBack | UpperChest | two-handers, bows and shields when stowed |
| SocketHipL, SocketHipR | Hips | one-handed weapons when sheathed |
| HeadTop | Head | hats and helmets, so the "show helmet" option is one attach |
| LookFrom | Head | only a point that look-at aims from; the figures stay faceless |

The names match v2's sockets, except LookFrom (v2 calls it Eyes).

- **Two-handers.** The main hand holds the weapon by its socket, and the off hand reaches the weapon's second grip node
  by IK. The kit's `TWO_HAND` table already says where each hand holds each weapon; the export writes those points as
  `GripMain` and `GripOff` nodes. Carrying the weapon across the chest is then an idle pose plus that IK. v2 already
  keeps the off hand on two-handers this way.
- **Bows.** The bow sits in the left hand. The draw is a clip, and the string can follow the right hand for a few frames,
  or the bow swaps between two or three stages of draw.
- **Sheathed weapons.** Split what the kit paints today into the scabbard, which is worn (body mesh), and the weapon,
  which is a prop on the hip or back socket. Drawing a weapon moves it to the hand and leaves an empty scabbard.
- **Worn gear** (belts, pouches, quivers, packs, bedrolls, baldrics) stays in the body mesh, rigid on Hips or UpperChest.
- **Rigid props** (the handcart, wheelbarrows, doors, chests, shutters, wells) are node trees, not skinned. A cart is a
  body plus `WheelL` and `WheelR` (which spin by the distance travelled), plus `GripL`, `GripR` and `PushFrom` nodes. The
  pusher plays a push-walk, IK puts the hands on the grips, and the cart follows the pusher's root. A door is a leaf on a
  `Hinge`, with a `Handle`. A chest is a `Lid` with a `LidGrip`. The library builds hinged parts as labelled sub-parts
  and exports them as nodes. That is the same labelling machinery as bones, reused.

### 1.7 Mesh size

These were measured on saved library builds, without their ground disc. Nothing was changed.

| Figure | Voxels | Visible faces | Triangles as exported today | Triangles with merged faces and a texture | Extra for cutting by bone |
|---|---|---|---|---|---|
| Fighter | 41,921 | 24,400 | 48,800 | 16,224 | +6% |
| Ranger | 30,418 | 17,490 | 34,980 | 12,154 | +7% |
| Orc wizard | 85,990 | 39,854 | 79,708 | 21,378 | +9% |
| Commoner | 35,192 | 16,502 | 33,004 | 9,496 | – |

- **Why a texture is needed.** Today every face is its own quad with a vertex colour. Voxel colours vary slightly from
  voxel to voxel, so merging faces requires moving colour into a small texture; 512×512 covers any figure.
- **The cost of cutting by bone is small.** Cutting the merge by bone, which rigid skinning needs, adds under 10%.
- **Crowds are affordable.** Forty villagers come to under a million triangles, well within a modern GPU's reach, and
  distant crowds can take a half-resolution version later if needed.

---

## 2. Where the animations come from

### 2.1 What v2 already has

v2 uses no Mecanim clips or avatars at all; it animates everything in code. All of it was read only.

| v2 code | What it does |
|---|---|
| `Assets\Game\Scripts\Visual\HumanoidAnimator.cs` (867 lines) | A gait with feet planted by leg IK and heel-to-toe roll, pelvis bob, sway and counter-turning shoulders. Also weight-shifting idles, sitting, talking, look-at (head 60%, neck 40%, chest 15%), downed and dead states, cape swing, and an off hand that stays on two-handers. |
| `HumanoidClips.cs` | Keyed actions as poses on a timeline, joined by smooth curves, with a wind-up and a follow-through. The actions are slash (two variants), overhead, thrust, punch, bow, crossbow, throw, three casts, heal, bless, drink, hit, dodge, cheer, interact, shove and draw. There are stances for seven grips: unarmed, one-hand, shield, two-hand, polearm, bow and crossbow. |
| `HumanoidBuilder.cs` | A 30-bone skeleton: Root, Hips, Spine, Chest, Neck, Head, clavicles, arms, legs, toes, a four-bone tail and a four-bone cape. It has sockets for the hands, shield, back, hips, head top and a look point. Bones rest with no rotation, and the arms hang 11° out, the same as the kit's hang stance. |
| `BeastAnimator.cs` | Four-legged gaits (walk, trot, gallop) with paw IK, a tail and ears on springs, bites, hits and howls. |

So v3 doesn't start from zero. If our export rests the way v2's rig does (no rotation at rest, arms at 11°), v2's
animator can drive a voxel figure through a bone-name map (the last column of the table in 1.2). The one real difference
is UpperChest, which v2 lacks: it takes a share of v2's chest rotation. If the engine becomes Godot, this C# code has to
be ported, but the glTF rig works there as it is.

### 2.2 Mixamo

- **Cost:** free; it needs an Adobe account.
- **Terms:** characters and animations are royalty-free for personal and commercial projects, games included, and no
  credit is required. The one hard rule is that raw clip files may not be passed on outside the team as standalone
  assets. A private game among friends is well inside the terms. Adobe's community FAQ also says Mixamo content may not
  be used to train machine-learning models; that matters for UniMate (section 3).
- **What it has:** thousands of motion-captured clips. Its strengths are everyday motion, gestures, sitting and kneeling,
  sword-and-shield, great sword, bow and magic sets, hits and deaths. It has no quadrupeds.
- **Retargeting to our rig.** Download clips "without skin" on the stock Mixamo character, at 30 fps, and "in place" for
  locomotion. Mixamo's names differ from ours, so a fixed table maps them (1.2).
  - Unity: set the clip's rig to Humanoid and play it on our figure's Humanoid avatar; retargeting is automatic.
  - Godot: import with a BoneMap on `SkeletonProfileHumanoid`; the auto-mapper knows Mixamo's names, and our names
    already are the profile's.
  - Mixamo rests in a T-pose while ours hangs the arms; both engines handle that, and the export carries T-pose data
    for Unity's avatar builder.
- **Risks:** motion capture can look floaty on blocky figures, so speed clips up 10–20% and hold key poses. Clips made
  for human proportions can push hands into big gnome heads or arms into dwarf bellies (risk 4). Mixamo is a free side
  service Adobe could change or close at any time, so download the needed set once rather than relying on it staying up.

### 2.3 Free and paid packs

| Source | Cost | Licence | Worth it for |
|---|---|---|---|
| Quaternius Universal Animation Library 1 and 2 | Free: 45 of the first library's 120+ clips, and most of the second's 130+. The rest through Patreon, $15–75 a month | CC0 (public domain) | locomotion, combat and combos, sitting, deaths, emotes, farming and fishing, all on one humanoid rig with glTF files |
| Kevin Iglesias, Unity Asset Store | Human Melee $23, Spellcasting $28; the Human Mega Animations Pack $130 (often $65), which includes villager and crafting sets; free samplers too | Asset Store standard EULA | hand-keyed, stylised; villagers and crafting fit our NPC list |
| Explosive, "RPG Character Mecanim Animation Pack" | $120 (often $60), 1,437 clips across 15 weapon styles | Asset Store standard EULA | the widest weapon-family coverage (two-handed sword, spear, bow, crossbow and more) |
| Synty ANIMATION packs | $50–70 each (Base Locomotion $70, Sword Combat $60, Idles $50), all of them for $410, or a $30-a-month plan | Synty EULA; check its exclusions | polished and consistent |
| Mocap Online | $3–60 a pack (often on sale); FBX, BVH, Blender | standard licence up to 1M end users | medieval sword and magic mocap |
| ActorCore (Reallusion) | $1.50–12 a clip, $59–200 a pack; 32 free | its export licence | buying just the clips needed |

- **Retargeting.** All of these are humanoid, so they retarget exactly as Mixamo does.
- **Engine terms.** The Unity Asset Store's standard EULA doesn't tie assets to Unity, so they can go into Godot, unless
  an asset is marked "restricted". Fab's standard licence allows any engine, except items marked "UE-only".
- **Where they live.** Keep all third-party clips in the game project, never in the library. The library stays Peter's
  own and licence-clean.

### 2.4 Hand keyframing in Blender

- **Cost:** Blender is free. The cost is a person's time: roughly an hour for a simple gesture, and a day or more for a
  polished walk or attack, for someone practised; much longer for a beginner. Agents never run Blender, so this is Peter
  or a hired animator.
- **Setup:** import `rig.glb` and key our bones directly (FK), with a few IK constraints on hands and feet, then export
  the clip as glTF. Rigify control rigs work too, but must be baked back onto our bones.
- **Best for:** a few hero moments. For everything else the scripted route below is cheaper.

### 2.5 Scripted key poses (our own clips)

This is what v2's `HumanoidClips` already does, moved into data.

- **The clip format.** A clip is a short JSON list of keys: a time, bone turns in degrees from the rest pose, a hips
  offset, optional IK targets ("left hand to the door handle"), and events such as the impact, footsteps or a door
  closing.
- **The compiler.** A Python compiler writes the clip as a glTF animation on our skeleton. The engine loads it by name,
  and the same JSON can drive v2's animator at runtime.
- **Review.** Clips are reviewed on a library page, where each clip plays on four builds side by side (human, dwarf,
  gnome, orc). The Asset Library's page viewer (Google's model-viewer, already in the library) plays glTF clips. Peter
  comments, and agents change the JSON. No Blender, and no downloads.
- **Cost:** agent time; Peter reviews.
- **Fit:** the best stylistic fit, because the clips are made for these figures. It's ideal for faceless body language,
  the reactions to non-humans, NPC loops and anything stock packs don't have. It's weaker than motion capture for long,
  weighty full-body motion.

### 2.6 Procedural: IK and look-at

- **Cost:** free. Both engines have it: Unity's Animation Rigging is now a core package (two-bone IK, aim, damped
  transforms), and Godot has `LookAtModifier3D` (4.4), `SpringBoneSimulator3D` (4.5) and `TwoBoneIK3D` and other IK
  nodes (4.6). v2 has its own two-bone IK and look-at already. Final IK ($90, often $45) isn't needed.
- **Use it for:**
  - heads that follow the party, which is the cheapest and most telling suspicion cue;
  - feet on stairs and slopes;
  - hands on door handles, chest lids, cart grips and the off-hand grip of two-handers;
  - springs on soft parts;
  - posture layers: the brief's stoop, an old person's bent back, a flinch added on top of any clip.

### 2.7 Other options worth knowing

- **Video motion capture.** Rokoko Vision is free with one camera, takes clips up to 15 seconds and exports FBX. Peter
  could act out the warding sign on a webcam and retarget it. It needs cleanup.
- **Cascadeur.** AI-assisted keyframing. The free version can't export FBX; Indie is about $8 a month.

### 2.8 What each costs Peter

| Source | Money | Peter's time | Fit to the voxel look | Private-game licence | Use for |
|---|---|---|---|---|---|
| v2's code animator | $0 | none | good (stylised, keyed) | ours | locomotion, combat, casting, hits, falls, look-at |
| Mixamo | $0 | 1–2 h to choose and download the first set | medium | fine | gestures, emotes, interactions, sitting and kneeling |
| Quaternius (CC0) | $0, or Patreon for the rest | an hour to choose | good | public domain | locomotion, work, combat fill-ins |
| Paid packs | $25–130 a pack | choosing and buying | good (hand-keyed ones) | fine (watch "restricted" and "UE-only") | weapon breadth, villager work |
| Scripted key poses | $0 | reviews only | best | ours | Barovian reactions, faceless gestures, NPC loops |
| Hand keying in Blender | $0, or a hired animator | hours per clip | best | ours | hero moments |
| IK and look-at | $0 | none | best | – | heads, feet, hands on props, soft parts |
| UniMate | $0 | none (agent time); an experiment | unknown | grey zone | drafts and in-betweens (section 3) |

---

## 3. UniMate (Peter's question)

`github.com/Friedrich-M/UniMate`: "UniMate: One Unified Model to Animate Diverse Skeletons", SIGGRAPH Asia 2026, from
Princeton, UC Berkeley, MIT and others. Read online only; nothing was downloaded or installed.

**What it is.** A research model that writes motion from a text prompt for an already-rigged character with any
skeleton. The recommended checkpoint takes 5 to 70 joints. It can also:
- fill in motion between key poses (in-betweening);
- re-animate chosen joints under a new prompt while pinning the rest (editing);
- chain prompts into a longer sequence.

Output is motion data (`.npy`, local joint rotations) and, through its data pipeline, an animated GLB or FBX. Each clip
is 60 frames at 30 fps: two seconds.

**Where it fits.** Only after our rig export exists (steps L1 to L3 in section 5), because it animates rigged
characters. It needs:
- **The skeleton in a T-pose.** Our export can write a T-posed copy; rigid parts turn cleanly, and the arms rise level.
- **The rig turned into its "feature directory"** by its preprocessing scripts. These run Blender 3.2+ (only its final
  skinning step has a NumPy path), so under the one-Blender rule that is the main session's job.
- **At least one existing clip on our rig.** The model card says there is no skeleton-only input. One of our scripted
  clips, or a retargeted walk, will do.
- **Joint names mapped to its vocabulary.** Its rule-based cleaner works offline; its language-model variant calls an
  outside API by default, so use the offline one.

Its output is rotations for our own bones, and turning them into glTF clips on our rest pose is a small script.

**What it could make for us.**
- **Drafts of the reactions and faceless gestures stock packs lack,** from prompts: "steps back two paces, raising one
  hand", "turns away and hunches", "makes a sign with the right hand toward someone".
- **In-betweens: the most promising use.** We keep control of the key poses, which are the part that carries the style,
  and it fills in the motion between them.
- **Edits:** keep a walk's legs, and change the arms to "carrying a basket".
- **Later, beasts** (wolves, bats, horses, ravens). Much of its training is animals, where Mixamo has nothing.

What it can't do:
- **Contact with props.** Its captions were rewritten to body-only motion, so "shuts a door" becomes a gesture without
  a door; IK and our key poses have to do the contact.
- **Guaranteed loops.**
- **Long clips in one go.**

**Quality and risk.**
- **It's brand new.** The weights went up three days ago and had 27 downloads last month. Expect rough edges, bash
  scripts and Linux-first tooling.
- **Its human motion is narrow.** The paper says it beats other methods, but the project page shows no numbers, and I
  found no comparison with models made only for humans. Everything it knows about humans comes from Mixamo: about a
  quarter of its training samples, cut down to a 22-joint humanoid by default. Our 24-bone set is a close match.
- **Its motion needs cleanup.** Generated motion usually slides its feet and jitters. Budget a cleanup pass per clip:
  foot locking by IK, and smoothing.

**The licence, for a private game.**
- **Code:** MIT.
- **Weights:** the Hugging Face page now shows an MIT licence, as a tag and a LICENSE file. The page's metadata was
  updated on 30 September 2026 at 00:25 UTC; the first read of it found no licence.
- **Training data keeps its own terms:**
  - Mixamo: Adobe's community FAQ says Mixamo content may not be used to train machine-learning models;
  - Objaverse-XL: a licence per object, some of them non-commercial;
  - Truebones: a commercial licence that forbids redistribution.
- **So the weights sit in a grey zone.** For a game Peter plays privately with friends, the practical risk is low. For
  anything public or sold, clear the terms first, or replace UniMate-made clips.

**Will it run on this machine?** Probably yes, for making clips:
- **The hardware is enough.** This machine has an RTX 4070 Ti with 12 GB, 32 GB of RAM and a Ryzen 7 5800X, and its
  driver supports the CUDA 12.4 build of PyTorch 2.5.1 that UniMate pins.
- **Memory isn't the limit.** The model is small: a 74-million-parameter denoiser plus the flan-T5-base text encoder.
- **The friction is Windows.** The scripts are bash with conda, and it pins Python 3.10 and `bpy` 4.0. WSL2, which runs
  Linux on Windows, is the smoother route.
- **Training is out of scope.**

**Verdict: a trial, not a foundation.** After the rig export works, spend one or two agent-days:
- generate 10 of the suspicion reactions, 5 gestures and a few in-betweens between our key poses;
- put them on the review page next to our scripted versions;
- keep only what reads well.

Nothing else in this plan depends on UniMate.

---

## 4. The animation set

"P1" is the first playable slice. I've assumed it means the party walks the village, talks with villagers, is met with
suspicion, enters a house (doors, chests, pick-ups) and fights a battle or two against people. "P2" is the rest of
Chapter One, and "P3" is later.

The sources are:
- **v2:** already in v2's code animator;
- **IK:** runtime look-at and IK;
- **Lib:** Mixamo, Quaternius or a pack;
- **Ours:** scripted key poses.

### 4.1 Moving and standing

| Clip | Loop | When | Source |
|---|---|---|---|
| idle (breathing, weight shifts) | yes | P1 | v2 |
| walk, run | yes | P1 | v2 (feet planted by IK) |
| turn on the spot | – | P1 | v2 |
| stairs and slopes | – | P1 | IK (v2's leg IK) |
| sit (chair, bench); sitting down and standing up | yes | P1 sit, P2 the moves in and out | v2 seats; Lib |
| kneel down, kneel, stand up | yes | P1 | Lib |
| idle fidgets (look round, stretch, adjust the belt) | – | P2 | Lib |
| sneak idle, sneak walk | yes | P2 | Lib |
| carrying walk (a sack or crate in both arms) | yes | P2 | Ours, Lib |
| an old, stooped walk with a stick | yes | P2 | v2 style plus the brief's stoop as a layer |
| lie down, sleep, get up (long rest) | yes | P2 | Lib |
| sprint, walk backwards, sidestep, climb, jump | – | P3 | Lib |

### 4.2 Combat by weapon family

| Family | Library weapons | Kit grip | Clips |
|---|---|---|---|
| Unarmed | – | – | punch, shove: P1, v2. Kick, grapple: P3 |
| One-handed | club, dagger, handaxe, light hammer, mace, sickle, battleaxe, flail, longsword, morningstar, rapier, scimitar, shortsword, war pick, warhammer | hang | ready, forehand and backhand slash, overhead, thrust (rapier, dagger, shortsword): P1, v2 |
| One-handed with shield | the above, plus a shield | hang, forearm | block: P1, Ours or Lib. Shield bash: P2 |
| Two-handed heavy | greatclub, greataxe, greatsword, maul, and versatile weapons held in two hands | lean; carried across the chest | across-the-chest idle and walk, overhead chop, sweep: P1, v2 plus IK |
| Polearm and staff | quarterstaff, spear, glaive, halberd, pike, trident, lance | upright | thrust, sweep: P1 for the spear and the staff (v2), P2 for the rest |
| Bow | shortbow, longbow | bow | ready, draw-aim-loose: P1, v2 |
| Crossbow | light, heavy and hand crossbows | hang or across | aim and shoot: P1, v2. Reload: P2, Ours |
| Thrown | dagger, handaxe, javelin, light hammer, dart, spear, trident | – | overhand throw: P2, v2 |
| The odd ones | whip, sling, net, blowgun | – | lash, whirl and loose, net throw, blow: P3 |
| Two weapons | two light weapons | – | alternating strikes: P3 |

Common to every family:

| Clip | When | Source |
|---|---|---|
| dodge | P1 | v2 |
| block or parry with the weapon | P1 | Ours, Lib |
| hit (light); hit (heavy, a stagger) | P1 | v2, plus a flinch layer |
| fall unconscious at 0 hit points, lie dying, get up when healed | P1 | v2 (downed state) |
| death | P1 | v2 |
| draw and sheathe | P2 | v2 |
| knocked prone, get up | P2 | Lib |
| victory cheer | P2 | v2 |

### 4.3 Spellcasting and reactions

| Clip | When | Source |
|---|---|---|
| cast at a target (bolts, rays) | P1 | v2 |
| cast raised (areas, summons, party blessings) | P1 | v2 |
| cast by touch | P1 | v2 |
| heal | P1 | v2 |
| holy symbol raised | P1 | v2 |
| drink a potion | P1 | v2 |
| hold concentration | P2 | Ours |
| quick reaction cast (the shield spell, counterspell): a fast cast plus a flinch | P2 | Ours |
| read a scroll | P2 | Ours, Lib |
| ritual (kneeling, long) | P3 | Ours |
| play an instrument | P3 | Lib |

Reactions in the D&D sense mostly reuse clips: an opportunity attack is the family's attack, the shield spell is a block
plus its effect, and a dodge is a dodge.

### 4.4 Interactions

| Clip | When | Source |
|---|---|---|
| open a door (push, and pull) | P1 | Ours, with hand IK to the handle; the door turns on its hinge node |
| knock at a door | P1 | Ours |
| open a chest (kneel, lift the lid) | P1 | Ours, with IK; the lid is a node |
| pick up from the floor | P1 | Lib |
| search or loot (kneel, rummage) | P1 | Lib |
| pick up from a table, put down | P2 | Lib |
| give and receive an item | P2 | Ours |
| pull a lever or chain | P2 | Ours |
| pick a lock, disarm a trap | P2 | Lib, Ours |
| push a cart | P2 | Lib, with IK to the grips |
| light a torch, raise a lantern | P2 | Ours |
| climb a ladder | P3 | Lib |

Until the P1 interaction clips exist, v2's generic "interact" reach stands in for them.

### 4.5 Emotes and conversation

The figures are faceless, so the body carries every conversation. This group matters more than in most games.

| Clip | When | Source |
|---|---|---|
| talk calmly, talk with emphasis (loops) | P1 | v2 talking, Lib |
| listen (small nods) | P1 | Ours, IK |
| nod yes, shake head no | P1 | Ours |
| shrug, point, wave, beckon | P1 | Lib |
| bow (formal) | P1 | Lib |
| arms crossed, hands on hips | P1 | Lib |
| laugh, grieve (head down, hands to the face), cower, plead (hands clasped), wave someone away | P1 | Lib, Ours |
| curtsey, salute, clap, think (hand to the chin), anger (a fist, leaning in), surprise (starting back) | P2 | Lib |

### 4.6 Village life

| Clip | When | Source |
|---|---|---|
| lean on a wall, sit on a bench, stand guard (spear upright), patrol | P1 | Lib, v2 |
| pray standing, pray kneeling | P1 | Lib |
| sweep, carry a sack, chop wood | P1 | Lib (work and farming sets) |
| tavern: drink while seated, a barkeep wiping the counter | P1 if the slice has a tavern, else P2 | Lib |
| dig, hammer at an anvil, draw water, stir a pot, knead dough, hang washing, feed animals, call out wares, push a cart | P2 | Lib, Ours |
| preach with a raised symbol, light a candle, mourn at a grave | P2 | Ours, Lib |
| toast, pour, cards or dice at a table, a drunk's sway, asleep at a table, a musician | P2–P3 | Lib |
| children at play | P3 | Lib |

### 4.7 Villagers meeting non-human party members

Peter wants distrust scaled by race. Agent C is drafting the tiers (`coord\design\barovian_suspicion.md`); these clips are
the vocabulary the tiers draw on. A human outlander might get only a glance. A goblinoid might get the door shut in their
face.

| Clip | When | Source |
|---|---|---|
| glance (the head follows the stranger for a moment) | P1 | IK (look-at) |
| stare (head and shoulders turn to track) | P1 | IK, v2 look-at |
| whisper to a neighbour (turn, hand to mouth) | P1 | Ours |
| step back (one or two steps, hands half raised) | P1 | Ours (a UniMate candidate) |
| turn away (a shoulder turned, head down) | P1 | Ours |
| the warding sign (Peter picks the gesture: section 6) | P1 | Ours |
| clutch a charm or holy symbol | P1 | Ours |
| hurry away (a fast walk, a glance back) | P1 | v2 walk plus look-at |
| shut the door (step inside, pull it to; it slams) | P1 | Ours, plus the door's hinge node |
| refuse service (arms crossed, head shaking) | P1 | Ours |
| close the shutters at a window | P2 | Ours, plus shutter nodes |
| call the guard (point, shout) | P2 | Ours |
| brandish a tool (a pitchfork or club raised defensively) | P2 | Ours |
| grudging acceptance (shoulders drop, a short nod), when the party has earned it | P2 | Ours |
| flee (a run with the arms up) | P2 | v2 run plus a layer |
| pull a child close, shoo them inside | P3 | Ours |

### 4.8 Counts

| Group | P1 clips | Of which v2 or IK | Library | Ours |
|---|---|---|---|---|
| Moving and standing | 9 | 6 | 3 | – |
| Combat | 25 | 23 | 1 | 1 |
| Spellcasting | 6 | 6 | – | – |
| Interactions | 6 | – | 2 | 4 |
| Emotes and conversation | 17 | 2 | 11 | 4 |
| Village life | 9–11 | 2 | 7–9 | – |
| Reactions to non-humans | 10 | 3 | – | 7 |
| **Total** | **about 83** | **about 42** | **about 25** | **about 16** |

Creatures (the beasts and mounts batch) come later. They get their own skeleton templates and v2's four-legged animator,
with UniMate as a possible source of extra motion.

---

## 5. The plan

These are agent-days: a day of one agent's work, tests included. Peter's reviews come on top.

The library side comes first and doesn't depend on the engine decision. The game side assumes Unity, because v2 is
Unity. If Godot wins, import is native (glTF), but add one to two weeks to port v2's animator.

### 5.1 Library side (what the kit and the export must add)

| Step | What | Effort | Done when |
|---|---|---|---|
| L1 | **Rig data.** Grow the 24-bone skeleton from `P` (new: the collarbone roots, UpperChest, the balls of the feet). Extend `pose.py`'s 15 segments to one bone per voxel, and keep its owner rules: head parts to Head, held parts out, the ground and world props out. Save the bone per voxel, the owner per voxel (the grid already records it) and a `rig.json` with each build. | 1–1.5 | every hero and village figure carries its rig; unposed builds stay voxel-identical (`kit_diff`) |
| L2 | **Game builds.** A build mode with the arms in the hang stance and no base pose (the brief's pose goes to the extras instead). It drops the ground disc, mannequins and world props, builds each held prop on its own with its grip frame, and splits sheathed weapons into scabbard and weapon. Add bind-stance fit sheets as numpy flat projections. | 1–2, plus 1–3 to fix parts that assume a pose (for example zone-painted shawls) | the fit sheet shows every Chapter One figure clean in the rest stance |
| L3 | **The glTF writer, in pure Python.** Merged faces per bone, the texture (colour, emission, metal/roughness), the skin, the sockets, the extras and the T-pose data. Check files with the Khronos glTF validator, and add a library page that shows them in model-viewer. | 3–4 | a validated `rig.glb` for the three heroes and the village figures |
| L4 | **Joints.** Cuts through the joints, hidden cores, soft bands on the trunk and neck, and hidden-seam dressing. Add a stress sheet: six builds by eight hard poses (arms overhead, a deep lunge, a crouch, the head turned 70°, and more), as flat projections. | 2–3 | Peter approves the stress sheet |
| L5 | **Props.** Weapon GLBs with `GripMain` and `GripOff` (from `TWO_HAND`), shields and held props. Rigid-prop node trees (cart wheels and grips, chest lids, door and shutter hinges) come with the environment and prop batches. | 2, then per batch | a sword, a greataxe, a bow, a shield, the cart and a door export cleanly |
| L6 | **Our clips.** The JSON clip format, the compiler to glTF animations, and a review page that plays each clip on four builds. Then the first 15 clips: the P1 reactions and gestures. | 3–4 | Peter reviews the first 15 |
| L7 | **Soft chains.** Cape, long hair, skirt and tail chains with smooth weights, and the spring settings in the extras. | 2 | capes and skirts bend on their chains in the viewer |

Library total: about 14–19 agent-days, plus 1–3 for the part fixes found in L2.

### 5.2 Game side (each step ends visible on Play)

| Step | What | Effort | Visible on Play |
|---|---|---|---|
| G1 | **Import.** UnityGLTF, which can build Humanoid avatars, or Unity's glTFast plus a small avatar script. A URP voxel material with point sampling and emission, and a prefab per character. | 2–3 | a voxel villager standing in a v2 scene |
| G2 | **v2's animator on the voxel rig.** An adapter that fills v2's `HumanoidRig` from our bones and sockets, including UpperChest. | 2–4 | the party walks, fights, casts, falls and gets up as voxel figures |
| G3 | **Attachments.** Weapons, shields, sheathed weapons, and the off hand on two-handers by IK. | 2 | weapons in hand; two-handers carried across the chest |
| G4 | **A clip layer** for gestures, emotes and work loops (Humanoid clips from Mixamo, packs and ours). Upper-body masks keep it from fighting the code animator, which runs last and adds on top. | 4–6 | talking villagers gesture; workers sweep and chop |
| G5 | **Soft parts.** Springs for capes, hair, skirts and tails, from the extras. | 2–3 | capes and hair swing |
| G6 | **Village life and suspicion.** Ambient loops, stares by look-at, and reactions picked by agent C's tiers. | 4–6 | villagers glance, whisper, step back or shut their doors as the party passes |
| G7 | **Performance and look.** Forty villagers plus the party in the village, and the camera's pixel look (agent A's call). | 2–3 | a full village at a steady frame rate |

Game total: about 18–27 agent-days.

The first moving voxel party appears after L1, a first cut of L2 and L3, then G1 and G2: about 9–13 agent-days.
L4 then improves the joints in place.

### 5.3 Risks

| # | Risk | What we do about it |
|---|---|---|
| 1 | Rigid joints look broken in big bends | hidden cores and angle limits; the L4 stress sheet before anything is committed; if Peter dislikes the knuckles, try soft bands on elbows and knees too |
| 2 | Hidden surfaces show when limbs move (armpits, inner thighs, under hair) | hidden-seam dressing; checked on the stress sheet |
| 3 | Kit parts that assume a pose (shawls painted in a zone, hands painted over hilts) | fit sheets in the rest stance, and fixes part by part. The size of this is unknown until L2 runs |
| 4 | Stock clips made for human proportions push hands into big gnome heads or arms into dwarf bellies | per-family correction layers; every clip reviewed on a line-up of builds (human, dwarf, gnome, halfling, orc) |
| 5 | Motion capture looks floaty on blocky figures | prefer hand-keyed packs, v2's keyed clips and ours; play clips 10–20% faster and hold the key poses |
| 6 | The code animator and a clip layer fight over the same bones | one owner per bone at a time: masks, with the code layer running last and adding on top |
| 7 | The engine decision is still open | the library side is engine-neutral; a Godot choice adds one to two weeks for porting v2's animator |
| 8 | Licences | third-party clips stay in the game project and never enter the library; keep a licence list; never use Fab "UE-only" or Asset Store "restricted" items outside their engine; UniMate stays private-only |
| 9 | Mixamo disappears | download the needed set once, early |
| 10 | Too many triangles in a crowd | merged faces (about a third of today's triangles), one material, and a half-resolution version for distant crowds if needed |
| 11 | The one-Blender rule slows exports | the exporter is pure Python; Blender only renders |
| 12 | In-game cubes turn with their bones while the library's stills stay on the grid | accepted, as in Hytale. The camera's pixel treatment (agent A) decides how much it shows |

---

## 6. Decisions for Peter

1. **The rest stance of the rig.**
   - (a) The kit's hang stance, arms 11° out, the same as v2's rig. *(Recommended.)*
   - (b) An A-pose with the arms 30° out: cleaner armpits, but v2's poses need an offset.
2. **How bent joints look.**
   - (a) Ball joints: rigid parts with a rounded core, the Hytale way. *(Recommended.)*
   - (b) Soft bends everywhere: smooth, but voxels stretch.
   - (c) Mixed, joint by joint, after the stress sheet.
3. **Party gear.**
   - (a) Baked: each outfit is a whole figure build, swapped as a whole; builds are cheap scripts. *(Recommended to
     start.)*
   - (b) Modular armour pieces on one skeleton: more flexible, more work.
4. **The warding sign** (or Peter's own idea).
   - (a) Touch the brow, then the heart, then hold the palm out toward the stranger.
   - (b) Two fingers pointed at the stranger, with the head turned away.
   - (c) Grab a charm at the throat and spit to the side.
5. **Clip budget.**
   - (a) Free first: v2, Mixamo, Quaternius and ours, then buy only for gaps. *(Recommended.)*
   - (b) Buy a big RPG pack now ($60–130).
6. **UniMate.**
   - (a) A one- or two-day trial once the rig export works. *(Recommended.)*
   - (b) Skip it for now.
7. **Where our clips live in the library.** A new asset-type folder, `pixel3d\animations\`, with review pages under
   `reviews\<nn>_animation`; or somewhere else Peter prefers.

---

## Sources

Read-only local sources:
- `E:\Assets\DND5E\pixel3d\pipeline\kit\pose.py`, `core.py`, `bodygen.py`, `refs.py` and `weapons.py`
- `E:\Assets\DND5E\pixel3d\pipeline\body.py`, `vox.py` and `render_vox.py`
- `E:\Assets\DND5E\pixel3d\pipeline\3dpixelartlessons.md`
- `E:\Unity\Projects\dungine.v2\Assets\Game\Scripts\Visual\HumanoidAnimator.cs`, `HumanoidClips.cs`,
  `HumanoidBuilder.cs` and `BeastAnimator.cs`
- The mesh counts in 1.7 came from a scratch script run on the saved `.npz` builds.

Web sources (read 2026-09-30):
- UniMate: [repository](https://github.com/Friedrich-M/UniMate),
  [data pipeline](https://github.com/Friedrich-M/UniMate/blob/main/data_process/README.md),
  [project page](https://linzhanmou.com/unimate/), [weights](https://huggingface.co/Linzhan/UniMate),
  [paper](https://arxiv.org/abs/2609.05415)
- Mixamo: [Adobe community FAQ on licensing](https://community.adobe.com/questions-696/mixamo-faq-licensing-royalties-ownership-eula-and-tos-589400),
  [licence summary](https://www.licenseorg.com/guide/3d-assets/mixamo)
- Quaternius: [Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html),
  [UAL 2](https://quaternius.com/packs/universalanimationlibrary2.html),
  [OpenGameArt listing](https://opengameart.org/content/universal-animation-library)
- Packs: [Kevin Iglesias](https://assetstore.unity.com/packages/3d/animations/human-mega-animations-pack-162341),
  [Explosive RPG pack](https://assetstore.unity.com/packages/3d/animations/rpg-character-mecanim-animation-pack-63772),
  [Synty animation](https://syntystore.com/collections/animation), [Mocap Online](https://mocaponline.com/),
  [ActorCore](https://actorcore.reallusion.com/)
- Licences: [Asset Store assets in other engines](https://gamefromscratch.com/using-asset-store-assets-in-other-engines-is-it-legal/),
  [Fab UE-only content](https://forums.unrealengine.com/t/fab-ue-only-content-licensing/2082870)
- Engines: [Godot SkeletonProfileHumanoid](https://docs.godotengine.org/en/stable/classes/class_skeletonprofilehumanoid.html),
  [IK returns to Godot 4.6](https://godotengine.org/article/inverse-kinematics-returns-to-godot-4-6/),
  [Godot retargeting](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/retargeting_3d_skeletons.html),
  [Unity Animation Rigging changelog](https://docs.unity3d.com/Packages/com.unity.animation.rigging@6.6/changelog/CHANGELOG.html),
  [Unity avatar mapping](https://docs.unity3d.com/Manual/class-Avatar.html),
  [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF), [glTFast avatars](https://github.com/atteneder/glTFast/issues/391),
  [VRM spring bones](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.md)
- Blocky games: [Hytale: making models](https://hytale.com/news/2025/12/an-introduction-to-making-models-for-hytale),
  [Hytale Blockbench plugin](https://github.com/JannisX11/hytale-blockbench-plugin),
  [Veloren animation crate](https://docs.veloren.net/veloren_voxygen_anim/character/struct.CharacterSkeleton.html)
- Tools: [Final IK](https://assetstore.unity.com/packages/tools/animation/final-ik-14290),
  [Magica Cloth 2](https://assetstore.unity.com/packages/tools/physics/magica-cloth-2-242307),
  [Rokoko Vision](https://www.rokoko.com/products/vision), [Cascadeur plans](https://cascadeur.com/plans)
