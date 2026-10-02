# Dungine II — build Chapter Two, then keep going until the whole game is finished

You are continuing **Dungine II: The Curse of Strahd**, a Baldur's Gate 3–style party RPG built in Unity at
`E:\Unity\Projects\dungine.v2` (Unity 6000.6.0f1, URP Forward+ with the GPU Resident Drawer, driven live through
the Unity CLI Pipeline: `unity command eval`, `capture_game_view`, `build`, `build_status`). Chapter One is
finished and verified in the editor and in the Windows build.

**Your job:** build Chapter Two to the same standard as Chapter One, then Chapters Three, Four and Five. **Do not
stop until the campaign plays from character creation to Strahd's defeat, an epilogue and credits**, in the editor
and in the built exe. Run as long as it takes. Work in the foreground (no background agents). Don't ask questions
unless you are truly blocked. Make sensible calls and record them in the progress log.

---------------------------------------------------------------------------------------------------------------

## 1. Non-negotiables

- **The party is exactly three player-made characters.** Races are all BG3 races plus Firbolg, across 12 classes.
  Module NPCs never become player-controlled party members. Story allies may *follow* (the `Follower` component,
  as Ireena does) or fight as **AI-controlled guests** (faction Party, not selectable). This covers Ireena, the
  card reading's ally, Van Richten and Ezmerelda.
- **Premade party** (the user's own cast; never rename them):
  - Arkus: high elf fighter, glaive, green-grey plate look.
  - Chairn: blue dragonborn sorcerer.
  - Dulandir: firbolg wizard.
  - Never use the names Brann, Sylwen or Pip.
- **Every asset is original and generated in code**, including meshes, textures, audio, icons and UI. No imported
  models, textures or sounds. Only OS fonts are used at runtime.
- **Use Curse of Strahd's places, people and plot beats, but write every line yourself.** Never copy or closely
  paraphrase the published adventure: no read-aloud text, NPC descriptions, handouts or letters. Invent connective
  scenes in the same gothic, wry tone as Chapter One's dialogue.
- **Monster and spell stats** follow D&D 5e rules (SRD-style numbers). Where a module creature isn't in the SRD,
  write your own balanced stat block.
- **Never distribute a build.** Building `Builds/DungineII/DungineII.exe` locally is fine. Before anything ships
  to anyone, ask the user where the five friends are.
- **Show the work where the user looks.** The user judges progress by what pressing Play opens. Every milestone
  must be reachable from the main menu (New Game, or Continue on a finished chapter). Capture that screen at each
  milestone.

## 2. Read first

1. `C:\Users\ptm\.claude\projects\E--Unity-Projects-Dungine\memory\dungine-v2-project.md` covers the tools, dev
   hooks and traps. Read the memory index next to it too.
2. The Chapter One campaign, in `Assets/Game/Scripts/Campaign/`. It is your pattern library.
   - `Campaign.cs`: hooks, rests, `CanLongRest`, `restInterrupted`, `WaitForCombat`.
   - `CampaignRegistry.cs`: area and dialogue registration, `ItemTaken`, `OnLongRested`, and
     `EndChapter`/`EndChapterRoutine`.
   - `AreaRoad.cs`, `AreaVillage.cs`, `AreaTown.cs`, `AreaManor.cs` (includes a night-attack scripted fight),
     `AreaDeathHouse.cs` (a four-floor dungeon with a collapse escape).
   - `AreaTserPool.cs`: Madam Eva's reading, with **original card tables** and flags `eva_blade`, `eva_book`,
     `eva_symbol`, `eva_ally` and `eva_lair`.
   - `DialoguesVillage.cs`, `DialoguesManor.cs`, and `CampaignCast.cs` (NPC looks).
3. World building.
   - `World/Area.cs` defines `AreaDef` and the `AreaContext` helpers: `Spawn`, `NPC`, `Monster` (encounter
     groups and sight range), `Door`, `Trigger`, `Chest`, `Stash`, `Note`, `Use`, `Prop`, `Obstacle`, `Marker`.
   - `World/Outdoor.cs` covers terrain, roads, lanes, clearings, flats, blockers, forest density and road lamps.
   - The rest: `World/Indoor.cs`, `World/Interiors.cs`, `World/Buildings.cs`, `World/Nature.cs`, `World/Props.cs`,
     `World/Kit.cs`, and `Visual/AtmosphereSystem.cs` (presets: BaroviaDay, BaroviaNight, Dusk, Mists, Interior,
     DeathHouse, Dungeon).
4. Rules.
   - `Rules/RulesData.cs`: classes, races and XP. **`MaxLevel = 5` today.**
   - `Rules/ActionLibrary.cs`: weapons and spells, **up to 3rd level today**.
   - `Rules/Monsters.cs` (20 monsters), `Rules/Items.cs`, `Combat/*` (turn-based 5e, AI with damage-type
     awareness).
5. Visuals and animation.
   - `Visual/HumanoidBuilder.cs`, `HeadBuilder.cs`, `HumanoidArmor.cs` (plate as separate pieces, jointed hands).
   - `Visual/HumanoidAnimator.cs` and `HumanoidClips.cs`: planted-foot IK gait, keyed actions, and two-handed
     weapons animated in weapon space.
   - `Visual/Beast.cs` and `BeastAnimator.cs`: wolves, bears and rats with real gaits.
   - `Visual/CreatureBodies.cs`: swarms, the mound, the broom, the grick.
6. UI Toolkit, built in code, in `UI/*`. `Core/Game.cs` handles modes, `NewGame`, `GoToArea`, `DevStart`, the
   `-devstart` and `-devview` launch options, and save/load (`Core/SaveSystem.cs`, JsonUtility).

## 3. Dev loop (use it constantly)

Each tool, with its usage:
- `bash tools/compile.sh`: stops Play, refreshes and prints compile errors.
- `bash tools/play.sh <name> <wait> '<C#>' <wait2>`: enters Play, waits for boot, runs C#, captures to
  `Assets/Captures/<name>.png`, and prints errors.
- `bash tools/ev.sh '<C#>'`: prints just the eval result.
- `bash tools/say.sh <option indices|c>`: drives dialogue. Set `Dungine.Dialogue.DialogueRunner.DevFast = true`
  first.
- `bash tools/tour.sh area:spawn ...`
- `bash tools/cap.sh <subject> <action> <name> [view] [frames]` records animation reviews into
  `DevCaptures/<name>/`.
  - Subject is a premade name or a monster id.
  - Action is idle, turn, walk, jog, run, combat, die, or any `AnimAct` value.
- `python tools/gif.py out.gif a b --labels "A|B"` turns captures into side-by-side GIFs.
- `powershell tools/runbuild.ps1 -Wait 34 -Out <png> -Extra "-devstart area:spawn -devview x:z:yaw:zoom"` runs
  the built exe and screenshots it.

Useful hooks from eval:
- `Dungine.Game.I.DevStart(area, spawn)`
- `Dungine.Combat.CombatManager.DevAutoPCs = true`: the AI plays the party, which is how you balance-test.
- `Dungine.Dev.PartyAt(x, z)`, `Dungine.Dev.CombatState()`, `Dungine.CameraRig.I.SetView(yaw, zoom)`
- `Dungine.Dev.HeadSheet(...)`, `Dungine.Dev.SimDrag(...)`, and `DialogueRunner.I.DevState()` / `DevPick`.

**Verification protocol for every area and quest:**
1. Compile clean.
2. DevStart into the area and screenshot it. Look at the screenshot; don't just check that the file exists.
3. Walk every quest branch with `say.sh`.
4. Run every fight with `DevAutoPCs`, and tune until an AI-played party at the intended level usually wins but
   takes real damage.
5. Check the console for errors after each run.
6. Build the exe and screenshot the key areas through `runbuild.ps1`.

## 4. Lessons from Chapter One (traps you will hit otherwise)

- **Never edit scripts while in Play mode**: a recompile wipes state. `compile.sh` stops Play for you.
- **Keep "Enter Play Mode Options" OFF** (domain reload on). With it on, the second Play shows fallback fonts and
  checker terrain.
- **eval has a 5-second main-thread limit.** Put anything long in a coroutine and return straight away.
  - Code inside eval needs fully-qualified names, and extension methods must be called statically, for example
    `UnityEngine.UIElements.UQueryExtensions.Q<T>(el, name)`.
  - Filter eval output with `grep -o '"result":"[^"]*"'`, because compiler warnings can push the result off the
    last line.
- **Only call Dev and DevCapture helpers in Play mode.** In edit mode they leave objects in the scene that break
  later runs.
- **Never pass inline scripts with quotes through a bash heredoc**: `\n` becomes real newlines and apostrophes
  break quoting. Write the script to a file with the Write tool, then run it.
- **Use `MathX.Smoothstep`, never `Mathf.SmoothStep`.** Unity's version interpolates rather than doing GLSL
  smoothstep.
- **Fonts:** UI Toolkit text needs `Font.CreateDynamicFontFromOSFont` plus `FontDefinition.FromFont`. A FontAsset
  built from a file path breaks every label.
- **Builds strip shader variants for materials that only exist at runtime.**
  - Keep material templates under `Resources/Mat`.
  - Custom shaders in `Resources/Shaders` must `multi_compile` the keywords they need.
  - Terrain must not draw instanced.
  - URP post-processing variant stripping stays off.
- **GPU Resident Drawer quirks:**
  - A custom fragment wrapper must call `UNITY_SETUP_INSTANCE_ID(input)` before reading any Lit material
    property, or it reads 0.
  - Moved mesh renderers only update once per engine frame, so render one capture per frame.
- **`MeshBuilder.AddLoft` now orients its surface itself.** Limbs built top-down had always been inside out. Pass
  `inward: true` for linings.
- **Occlusion is automatic once registered.** Register buildings with `Occluders.RegisterGroup(buildingRoot)` and
  trees with `Occluders.Register(renderer, true)`. They then dither a see-through hole around the party, and fade
  whole when the camera is inside.
- **Scripted fights:**
  - Start combat, then `yield return Campaign.WaitForCombat()`.
  - Set `Campaign.restInterrupted` for night attacks.
  - Mark haunted interiors `RestUnsafe`.
- **Dialogue style:**
  - Narration nodes use speaker `""`; cinematic cuts use `.Actor("strahd")`.
  - Put a manual "[SKILL]" prefix on check options.
  - Use `.Once()` / `.If()` / `.Inspire(background)`, and write journal entries with `journal.Update` /
    `AddLore`.
  - Keep lines short and characterful; the Chapter One files show the voice.
- **Balance lessons:** AI-played test parties get downed easily at level 1–2. Tune enemy HP to party level (Doru
  and the night attack scale with `6 × party level`), watch damage-type immunities, and give rest opportunities
  between big fights.
- **The user's playtest notes to honour going forward:**
  - Major towns must feel populated. Vallaki and Krezk need crowds with ambient barks and simple routines.
  - Objects must never pop out of view.
  - The minimap shows the camera view.
  - Portraits have name plates.
  - Hair must not clip through faces.
  - Faces are due a full overhaul once the user supplies reference designs. Don't block on them.
- **Visual bar:** Chapter One's remodel gave plate armour separate layered pieces and every character proper hands.
  - Give the other armour types (leather, hide, mail, robes, cloth) the same "separate pieces over a body"
    treatment when you build Vallaki's crowd and new enemies.
  - New creatures go through `cap.sh` reviews before you call them done.

## 5. The campaign from here

Chapter One ends at Tser Pool, and `EndChapterRoutine` currently returns to the main menu. Turn chapter endings
into **continuous play**:
- The Chapter Two title card follows the Chapter One summary, then an autosave, then the first Chapter Two area.
- A Chapter One save (flag `chapter1_done`) opened with Continue must also lead into Chapter Two.
- Add `Game.DevStartChapter(n)` and a `-devchapter n` launch option. They start the premade party at that
  chapter's level, with plausible flags, gear and reading results, so every chapter can be tested alone.

**The card reading must resolve.** Every card in `AreaTserPool.cs`'s tables names a place or ally that must exist
in the game:
- **Treasures:** the Sunsword, the Tome of Strahd and the Holy Symbol of Ravenkind are placed by `eva_blade`,
  `eva_book` and `eva_symbol`.
- **Ally:** `eva_ally` names the guest who joins the final battle.
- **Lair:** `eva_lair` decides where Strahd makes his stand in Castle Ravenloft.

Read the tables, list every destination, and build each one. Destinations include:
- the castle crypts, dining hall, study, highest tower, chapel/organ hall, audience hall and the grave he visits
- the old windmill above the Vallaki road
- the bell tower and saint's tomb of Vallaki's church
- a wealthy Vallaki family's storerooms
- the druids' fire grove
- a raven nest at the edge of the Svalich woods
- a ruined abbey's armory on the mountainside
- an unfinished-wedding chapel
- a frozen lake at the top of the valley
- the allies: a horseless knight, a monster hunter, a mad wizard and a grieving priest

Also add a **map of Barovia**. It shows discovered locations on the road network, allows travel between them, and
rolls road encounters by region and time: wolves, Strahd zombies, revenants, Vistani and blights. Strahd himself
should shadow the party between chapters with short, unsettling scenes.

**Progression.** Raise `MaxLevel` to 10 with the 5e XP table.
- Implement levels 6–10 for all 12 classes and their subclasses.
- Add 4th- and 5th-level spells, ability score improvements and feats.
- Do this before Chapter Three needs it.
- Rough level targets: Chapter Two takes the party from 3 to 5, Chapter Three from 5 to 7, Chapter Four from 7
  to 9, and Chapter Five from 9 to 10.

### Chapter Two: the walled town (build this first, completely)
- **The Old Svalich Road west from Tser Pool:** a crossroads with a gallows, and a road encounter.
- **The old windmill** on the hill (the hags' bakery, their dream pastries, the missing children). It's optional
  but a full dungeon, and a possible card destination.
- **Vallaki**, a populated hub town behind a palisade:
  - The burgomaster's mansion, his brute enforcer, and a forced-cheer festival that goes wrong.
  - The church of St. Andral, whose stolen bones leave it open to a vampire-spawn attack unless recovered.
  - The coffin maker's shop, where the bones are hidden and spawn are nesting.
  - The inn run by the family of wereravens.
  - The noblewoman who secretly serves Strahd, and her cellar.
  - The toymaker.
  - The carnival wagon of the disguised monster hunter.
  - A Vistani camp outside the walls whose leader's daughter is missing (the lake rescue).
- **Ireena's arc:** her safety in Vallaki and Strahd's growing pressure.
- **Ending:** the chapter closes on the town's crisis resolved (or lost) and the road north to Krezk opening.
- **Scale:** at least as many areas, NPCs, quests and fights as Chapter One, with multiple outcomes that set flags
  later chapters read.

### Chapter Three: wine, water and the abbey
- The winery under siege by druids and blights, with the stolen seeds.
- Yester Hill's ritual and the great tree blight.
- Krezk and its abbey (the abbot and his "bride" project), and the sacred pool (Ireena's past).
- Van Richten's tower and its trapped wagon.
- The werewolf den.

### Chapter Four: the order and the temple
- Argynvostholt: the revenant knights and the beacon.
- Berez and the witch's creeping hut.
- The mountain pass and the Amber Temple: dark gifts from sealed vestiges, and the dusk elf's quest for his
  sister.

### Chapter Five: the castle
- Castle Ravenloft as a multi-level dungeon: gate, halls, chapel, towers, crypts.
- The treasures and the ally from the reading, and Strahd's final fight in the carded lair.
- Strahd's boss features: legendary actions and resistance, regeneration, charm, mist form, children of the
  night, and lair actions.
- **Endings:** victory, with dawn over Barovia and Ireena's fate resolved; or defeat, as a game-over screen with
  reload.
- An epilogue and credits.

## 6. Definition of done (each chapter)

- **Playable end to end** with a fresh premade party, and from `-devchapter n`. No console errors in any run.
- **Every area** has been screenshotted and looked at. Areas feel lived-in, lit to match the module's mood, and
  hit the ~150+ fps Chapter One gets in the village.
- **Every fight** has been balance-tested with `DevAutoPCs` at the intended level.
- **Every quest** has a journal trail and at least one alternative resolution.
- **New creatures and NPCs** have been reviewed with `cap.sh` GIFs: idle, move, attack, hit, death.
- **Saves:** save and load work mid-chapter, and Continue lands in the right place.
- **Build:** the exe builds with 0 errors and has been checked through `runbuild.ps1`.
- **Records:**
  - Chapter notes are appended to `Plans/PROGRESS.md`: what exists, decisions, known issues.
  - A local git checkpoint commit exists in the dungine.v2 repo. Never push.
  - The memory file `dungine-v2-project.md` is updated with any new tools or traps.

## 7. Working rules

- Keep `Plans/PROGRESS.md` current as you go, with a checklist per chapter, so the work survives context
  compaction. Re-read it and this prompt after any compaction.
- Prefer finishing a chapter completely over sketching several.
- Only go on once the chapter meets the definition of done, and then go on.
- When a chapter is done, keep going with the next one straight away. When the whole campaign is done, do a full
  playthrough pass (dev hooks plus spot-checks), fix what it turns up, and rebuild.
- **Finish with a short report** for the user:
  - what was built
  - how to play it
  - what was verified and how
  - known issues
  - one screenshot per chapter
