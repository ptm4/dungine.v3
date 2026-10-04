# Pixel3d swap: inventory (agent A; round 1 2026-10-04, round 2 the same day)

This lists every v2 asset that shows in the village, on the default screen (what Play opens) and in the HUD, with
its library replacement.

- **Library paths** are under `E:\Assets\DND5E\pixel3d\`. "Export" means agent N's `export\game\` file.
- **Status:**
  - **swapped**: in v3 now as the library's model;
  - **needs export**: the library has it, but there's no game file for it yet;
  - **missing**: nothing in the library yet.
- The reserved plot's house and anything on it stay as v2 has them, and out of every capture. They aren't counted.

## The village: ground and nature

| v2 asset | Where v2 uses it | Library | Status |
|---|---|---|---|
| Terrain heightfield and its splat layers (dead grass, dirt, mud, rock, forest floor) | the valley floor | the ground (review 43): `village_ground.json`, 22,264 tiles of 38 terrain modules, 99 far meshes | swapped (round 2). v2's terrain no longer draws; it keeps the library's heights as the collider |
| Mud high street and lanes | the roads | the ground's `mud_road` tiles and verges | swapped (round 2) |
| Paved square | the square | `cobbled_square` (export, texture AO since round 2) | swapped |
| The manor's hill, the church's rise | (v2: a 1.6 m bump for the church only) | the ground's levels: the manor on 3 m, the church on 1.5 m | swapped (round 2) |
| Rocks ×104 (`Nature.Rock`) | the forest floor | the ground's 49 rock outcrops are the library's; v2's scattered rocks are still placed too | missing (review 44: rocks, stumps, logs) |
| Pines ×520, dead trees ×73 | the forest, the graveyard, the lane | none | missing (review 44) |
| Bushes ×520, ferns ×1,040, grass tufts ×1,600 | undergrowth and verges | none (the ground tiles carry grass relief, but not tufts) | missing (review 44) |
| Stumps ×65, fallen logs ×52 | the forest floor | none | missing (review 44) |
| The castle on the skyline | far backdrop | none | missing |
| Fog, sky | | effects | kept |

## The village: buildings and site

| v2 asset | Library | Status |
|---|---|---|
| 17 houses, tavern, shop, manor, church | the kit (reviews 33 to 36); round 2's layout puts the manor on its hill and the church on its rise, with the darker second looks | swapped |
| Fences, walls, gateposts, well, stalls, notice board, statue | the kit | swapped |
| Lampposts ×12 | `props\lighting\lamppost` (export) | swapped |
| Barrels ×2, crates ×3 | `items\gear\container\barrel`, `props\furniture\crate` (export) | swapped |
| Bench, cart, haybales ×2, woodpiles ×2, grave mounds ×18, coffin | export (round 2, the new LOD rule) | swapped |
| Broken cart | (review 32: built, export to come) | needs export |
| Gravestones ×48 | the four shapes apart (review 11: export to come) | needs export |
| The open grave | `open_grave` is a cut into the ground; the ground has no hole for it yet | needs export (a ground hole, or the grave as a ground tile) |
| Garlic over the manor door, claw marks on it | garlic strings and claw marks for doors (review 14: export to come) | needs export |

## The village: people

| v2 asset | Library | Status |
|---|---|---|
| Arkus | `characters\key\arkus\rig` (export, round 2) | swapped |
| Chai'rn | `characters\key\chairn\rig` (export, round 2) | swapped |
| Arkus's rune blade, Chai'rn's terrarium staff (held, in the library's designs) | left out of the rigs; "its own prop file on a hand socket" (plan step L5) | needs export |
| The party's weapons: glaive, quarterstaves | `glaive` (export), `quarterstaff` (look-test bake) | swapped |
| Dulandir | none; Peter hasn't decided | missing |
| Villagers ×4 | `characters\looks\barovian_commoner_m`, `_f`, `_m_2`, `_f_2` rigs (export, round 2) | swapped |
| Chapter One's people | review 9 rigs | swapped |
| Night-only enemies | none I may read | missing (night only) |

## The default screen (what Play opens)

| v2 asset | Library | Status |
|---|---|---|
| Hills (its own terrain, not the village's) | terrain tiles exist; there's no ground layout for the title screen | needs a ground layout (agent E or N) |
| The castle on the skyline | none (agent M's castle set, after Peter's review) | missing |
| The village gates | none | missing |
| Pines ×70, dead trees ×40, grass ×260 | none | missing (review 44) |
| Lamppost | export | swapped |
| Title, menu buttons, footer text; fog, sky | | kept |

## The HUD

| v2 asset | Library | Status |
|---|---|---|
| Portraits: Arkus, Chai'rn | their face renders | swapped |
| Portrait: Dulandir | none | missing |
| Hotbar action and spell icons (26 for the party) | `ui\icons\actions`, `ui\icons\spells` | swapped |
| Hotbar consumables (potion, scroll, bread) | the items' sprites | swapped |
| Top-right buttons: bag, character, journal, camp, settings | `ui\icons\hud\hud_*` (approved by Peter) | swapped (round 2) |
| Frames, pips, bars, text; the minimap (draws the scene) | | kept |
| Window item icons (inventory, trade, journal) | the items' sprites | later (not on the default screen or in the village) |

## Counts (kinds of asset)

- **Swapped: 33.**
  - The 22 from round 1, now including the ground's cobbles.
  - Round 2 adds 11: the ground (tiles, outcrops, far meshes), the roads, the hill and rise, Arkus, Chai'rn, the four
    villagers (one kind), the seven props as game exports (counted once), the cheaper cobbles, the HUD buttons, the
    relaid village, and the party's glaive from the export.
- **Needs export: 6.**
  - The broken cart, the gravestones apart, and the garlic and claw marks are all built and come next from agent N.
  - The open grave (it needs the ground), the heroes' held items, and the title screen's ground layout have no file
    yet.
- **Missing: 8.** Trees, undergrowth and grass, rocks/stumps/logs (review 44), the castle, the gates, Dulandir and his
  portrait, and the night enemies.
- **Kept, not assets:** fog, sky, HUD frames and text.
