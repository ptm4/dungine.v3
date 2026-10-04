# Pixel3d swap: inventory (agent A, 2026-10-04)

This lists every v2 asset that shows in the village, on the default screen (what Play opens) and in the HUD, with
its library replacement.

- **Sources:** v2's builders (`AreaVillage`, `Outdoor`, `Nature`, `MenuScene`, `HUD`, `PortraitRenderer`, `Icons`) and a
  census of the live scenes.
- **Library paths** are under `E:\Assets\DND5E\pixel3d\`. "Export" means agent N's `export\game\` has a game file for
  it.
- **Status:**
  - **swapped**: in v3 now, as the library's model;
  - **swap now**: the library has it and this round swaps it in;
  - **needs export**: the library has it, but the game needs a version it can't take from the library's own file. This
    covers a rig, a split of a combined model, or a terrain tile, which have no game file yet.
  - **missing**: nothing in the library yet.
- The reserved plot's house and anything on it stay as v2 has them, and out of every capture. They aren't counted here.

## The village: ground and nature

| v2 asset | Where v2 uses it | Library | Status |
|---|---|---|---|
| Terrain heightfield, splat layers dead grass, dirt, mud, rock, forest floor (`Nature.BuildTerrain`) | the whole valley floor and walls | `environment\terrain\dead_grass`, `dirt`, `mud_road`, `rock`, `forest_floor` (review 43, E02); village examples in `reviews\43_terrain\example_village_*` | needs export (E02 in progress; no game files yet) |
| Mud high street and lanes (`Outdoor.road`, `Lane`) | high street, west road, east road, church path, the lane, manor drive | `environment\terrain\mud_road` (+ verges, slopes) | needs export |
| Paved square (v2 cobble texture, `o.paved`) | the square, 14 m round the well; the manor drive's mouth | `environment\architecture\cobbled_square` (export; 276 placements in the layout) | swapped: v2's ground is lowered 15 cm under the tiles, with a flat floor for walking; full 1.5 cm stones within 16 m of the camera, 3 cm beyond |
| Pines ×520, dead trees ×70 + 3 placed (`Nature.Pine`, `DeadTree`) | forest round the village, graveyard, the lane | none (vegetation, review 44, agent E, next) | missing |
| Bushes ×520, ferns ×1040, grass tufts ×1600 (`Nature.Bush`, `Fern`, `GrassTuft`) | undergrowth and verges | none (review 44) | missing |
| Rocks ×104, stumps ×65, fallen logs ×52 (`Nature.Rock`, `Stump`, `FallenLog`) | forest floor | `environment\terrain\rock` is rock faces (cliff tiles), not boulders; none for stumps and logs | missing |
| The castle on the skyline (`Buildings.CastleRavenloft`) | far backdrop, north-east | none | missing |
| Fog banks (`Kit.FogBank`, `Outdoor.RoadFog`) | along the roads, the exits | an effect, not a model | keep (not an asset) |
| Sky, moon and clouds (`Sky` shader, `Atmosphere`) | the sky | an effect, not a model | keep (ask if Peter counts the sky) |

## The village: buildings and site

| v2 asset | Where | Library | Status |
|---|---|---|---|
| 17 houses, tavern, shop, manor, church (`Buildings.House`, `Mansion`, `Church`) | their plots | the village kit (reviews 33 to 36), N's export | swapped (b49b193) |
| Iron fences, wood fences, stone walls, gateposts | manor yard, between houses, graveyard | kit `iron_fence`, `wood_fence`, `stone_wall`, `gatepost` | swapped |
| Well, notice board, 3 market stalls, statue | the square | kit `village_well`, `notice_board`, `market_stall`, `headless_statue` | swapped |
| Lampposts ×12 (`Props.LampPost`, `Outdoor.Lamp`, `RoadLamp`) | square corners, along the roads | `props\lighting\lamppost` (export) | swapped |
| Barrels ×2 (`Props.Barrel`) | by the tavern door | `items\gear\container\barrel` (export) | swapped |
| Bench (`Props.Bench`) | by the tavern door | `props\furniture\bench` | swapped (look-test bake; game export wanted) |
| Crates ×3 (`Props.Crate`) | by the shop door; the stall crate | `props\furniture\crate` (export) | swapped |
| Cart (`Props.Cart(false)`) | the square | `items\vehicles\drawn\cart` | swapped (bake, 140 k triangles; game export wanted) |
| Broken cart (`Props.Cart(true)`) | by the manor drive | `items\vehicles\drawn\cart_2` is a second look, not broken | needs a broken look (or use the cart) |
| Haybales ×2 (`Props.Haybale`) | the square, the west road | `props\furniture\haybale` | swapped (bake) |
| Woodpiles ×2 (`Props.Woodpile`) | between houses | `props\furniture\woodpile` | swapped (bake, 71 k triangles) |
| Gravestones ×48, four shapes (`Props.Gravestone(0..3)`) | the graveyard | `props\graveyard_and_religion\gravestone_4_shapes` holds all four in one model | needs export (one file per shape) |
| Grave mounds ×18 (`Props.GraveMound`) | the graveyard | `props\graveyard_and_religion\grave_mound` | swapped (bake, 51 k triangles each, 0.9 M for 18) |
| The open grave (`OpenGrave`) | the graveyard | `props\graveyard_and_religion\open_grave` | needs the library's terrain: it is a cut into the ground, and v2's ground covers its opening |
| Closed coffin (`Props.Coffin`) | the burial, when it happens | `props\graveyard_and_religion\coffin` | swapped (bake, 106 k triangles) |
| Garlic over the manor door (`GarlicStrings`) | above the manor door | `props\horror\garlic_strings_on_a_door` is a whole door in its frame | needs the garlic alone (the kit's manor door is already there) |
| Claw marks on the manor door (`Scratches`) | the manor door | `props\horror\claw_marks` is a plank wall section with gouges, not an overlay | needs a door overlay (hidden for captures) |

## The village: people and creatures

| v2 asset | Where | Library | Status |
|---|---|---|---|
| Arkus (v2 procedural humanoid) | the party | `characters\key\arkus` (locked, static `model.glb`) | needs export (a rig) |
| Chai'rn | the party | `characters\key\chairn` (locked, static) | needs export (a rig) |
| Dulandir | the party | none; his design is Peter's to brief | missing |
| The party's weapons: glaive, quarterstaff ×2 | in hand, on the back | `items\weapons\martial_melee\glaive`, `simple_melee\quarterstaff` | swapped (v2's figures carry the library's weapons too) |
| Villagers ×4 ("Barovian Villager", "Gaunt Villager", `Monsters.Villager`) | the square, the high street, the manor drive | `characters\looks\barovian_commoner_m`, `_f` (+ `_2`); static export only | needs export (rigs) |
| Chapter One's people (Ismark, Ireena, Donavich, the burial party) | as v2 places them | review 9 rigs (export) | swapped |
| Night-only enemies (v2's) | the high street at night | none outside the sealed folder that I may read | missing (night only; leave night out of captures) |

## The default screen (what Play opens)

| v2 asset | Library | Status |
|---|---|---|
| Hills (terrain) | terrain tiles (E02) | needs export |
| The castle on the skyline | none | missing |
| The village gates (`Buildings.BaroviaGates`) | none | missing |
| Pines ×70, dead trees ×40, grass ×260 | none (review 44) | missing |
| Lamppost ×1 | `props\lighting\lamppost` (export) | swapped |
| Fog, sky | effects | keep |
| Title, menu buttons, footer text | HUD frames and text | keep (allowed) |

## The HUD

| v2 asset | Library | Status |
|---|---|---|
| Portraits ×3 (live renders of the v2 heroes) | `characters\key\arkus\face.png`, `characters\key\chairn\face.png`; Dulandir none | swapped for Arkus and Chai'rn; Dulandir missing |
| Hotbar action icons (v2 vector icons by kind: sword, fire, ...): the party's 26 actions | `ui\icons\actions\*`, `ui\icons\spells\*` (reviews 27 to 29). Every one the party has is there: main_hand_attack, cleave, lacerate, second_wind, breath_weapon, hidden_step, fire_bolt, ray_of_frost, shocking_grasp, chill_touch, toll_the_dead, magic_missile, shield, sleep, burning_hands, thunderwave, dash, disengage, hide, shove, help, dodge | swapped |
| Hotbar consumables: healing potion, spell scroll, bread | `items\magic\dmg\potions\potion_of_healing`, `items\magic\dmg\scrolls\spell_scroll`, `items\food_drink\bread_loaf` (`sprite.png`) | swapped |
| Top-right buttons: bag, character, journal, camp, settings | none (no UI chrome icons in the library) | missing |
| Condition pips, resource pips, HP bars, frames | HUD frames | keep (allowed) |
| Minimap | draws the scene from above | follows the scene |
| Inventory, journal, character and trade windows: item icons (v2 vector) | items' `sprite.png` per item | later (not on the default screen or in the village walk) |

## Counts

These count kinds of asset, not instances.

- **Swapped:** 22.
  - 7 before this round: the buildings, fences and walls, gateposts, the well, stalls and notice board, the statue,
    and Chapter One's people.
  - 15 this round: the cobbles, lampposts (village and menu), barrels, crates, bench, cart, haybales, woodpiles, grave
    mounds, coffin, the glaive (with every figure's weapons from the library), the portraits of Arkus and Chai'rn,
    the action and spell icons, and the consumable icons.
- **Needs export:** 9. The ground and roads (terrain), Arkus's rig, Chai'rn's rig, the villagers' rigs, the
  gravestones split by shape, the garlic alone, a claw-mark overlay, a broken cart, and the open grave (which needs
  the terrain).
- **Missing:** 9. Trees, undergrowth and grass, rocks/stumps/logs, the castle, the gates, Dulandir, his portrait, the
  night enemies, and the top-right HUD icons.
- **Kept, not assets:** fog, sky, HUD frames and text.
