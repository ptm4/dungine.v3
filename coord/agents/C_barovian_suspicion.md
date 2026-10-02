# Agent C: Barovian suspicion of non-humans (design draft)

Status: done 2026-09-30. Report: `coord\design\barovian_suspicion.md` and `.json` (a draft waiting for Peter).

## Brief (the prompt as given)

**Context**
- Dungine.v3 is a Curse of Strahd party RPG for Peter and friends.
- Peter (2026-09-30): "lets keep it true to the book, lets actually put a good amount of distrust, xenophobia and
  suspicion upon party members who are not "normal" in barovia, scaling for their race (dwarves and elves are weird sure
  but hobgoblin, bugbear obviously are worse)".
- Barovia's natives are human (the Barovians and the Vistani). The dusk elves are the known exception.
- The playable races the art library covers are in two places:
  - `E:\Assets\DND5E\data\race_profiles`: one JSON per race or subrace; aliases point to their parents.
  - The scope plan, `E:\Assets\DND5E\pixel3d\plan\SCOPE_PLAN.md`: races R01 to R07 are kept, and some are cut.
- Peter's own party includes a tiefling (Arkus) and a wood elf (Chai'rn).

**Task:** draft the design as `E:\Unity\Projects\dungine.v3\coord\design\barovian_suspicion.md`, with a
machine-readable draft beside it, `barovian_suspicion.json`.
1. **Tiers of suspicion by race.** Cover every race and subrace in the library's list, grouped, with a short in-world
   reason per tier. For example: human = an outlander, but familiar; dwarves, elves, halflings, gnomes = odd; dragonborn,
   tieflings, half-orcs = feared; goblinoids = monstrous.
2. **What each tier changes in play,** concretely and measurably:
   - shop prices and refusals;
   - dialogue openers and options;
   - how guards and crowds behave;
   - doors and shutters;
   - rumours spreading.

   Show how it scales with what's visible: hoods and disguises, a human companion vouching, reputation earned by helping
   a town. Show how the whole party's makeup combines (does the worst member count most?), and give Peter knobs to tune.
3. **The villager reaction animations** it needs, named, to feed the animation list.
4. **Open questions for Peter,** with options.

Keep it spoiler-free: Peter will play the campaign. No module secrets, hidden identities or plot twists; general
Barovian attitudes only. Mark it a DRAFT for Peter's approval.

**Rules**
- Write only those two files. Read-only everywhere else.
- Don't touch `E:\Unity\Projects\Dungine`, don't modify dungine.v2, and don't read `E:\Assets\DND5E\pixel3d\sealed`.
- No downloads.
- Write in plain, direct English.

**Final reply:** 5 lines: the tiers and the file paths.
