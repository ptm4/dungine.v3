# Agent A: phase 2, fork v2 into v3 and get it running unchanged

Status: started 2026-10-02.

## Peter's answer (2026-10-02)

"engine plan makes sense": Unity 6, v2 forked into v3, as your `research\engine_decision.md` recommends. His other
engine questions (how the friends play, the look, the character creator, the premade party, fog) are still open; phase 2
doesn't need them.

## What to do

1. **Back up v2 first, without changing it.** v2's own git repo has no commits, so the backup is a copy:
   - zip `E:\Unity\Projects\dungine.v2` to `E:\Backups\dungine.v2_2026-10-02.zip`, excluding only `Library`, `Temp`,
     `Logs`, `obj` and the build output (all regenerable);
   - check the zip opens and its file count matches the source.
   Never write inside v2: no git commands there, no opening it in Unity, nothing.
2. **Copy v2 into v3.** The game files go in `E:\Unity\Projects\dungine.v3\`, beside `coord\`, which stays as it is.
   - Copy without `Library`, the captures and the build (about 2.1 GB, by your audit).
   - Give v3 its own git repo at its root if it has none, with a Unity `.gitignore` that also keeps big captures out.
   - Make a first commit: "Fork of dungine.v2 as of 2026-10-02, unchanged". `coord\` is part of the repo.
3. **Get it running unchanged.**
   - Open the copy with Unity 6000.6.0f1 in batch mode to import and compile. Keep v2's settings (the domain reload
     setting, the pinned packages, the experimental pipeline package version).
   - Check the editor log for errors.
   - Run v2's own command-line tests and dev hooks against the copy, as your audit describes them.
   - Then show that Play works: what Play opens should be exactly what v2's opens. Save a capture of the default screen
     to `coord\research\phase2\` for Peter.
   - The `unity-cli` skill can help with Unity from the command line.
4. **Report differences.** Anything that doesn't behave like v2 (paths baked to v2's folder, product names, absolute
   paths in settings) gets fixed in the copy only, with a note on what and why.

## Rules

- Never modify dungine.v2. Never touch `E:\Unity\Projects\Dungine`.
- Before closing or relaunching any app Peter may have open (a Unity editor, for instance), stop and tell the main
  session; never force-close anything.
- No downloads (Unity and its packages are already installed; if a package must be fetched, stop and ask).
- Don't read `E:\Assets\DND5E\pixel3d\sealed`.
- Write in plain, direct English in anything Peter reads.

**Final reply:**
- the backup's path and size;
- what was copied and what was left out;
- the git commit;
- the compile and test results;
- the capture's path;
- anything that differs from v2;
- what phase 3 (the asset bridge, starting with agent B's G1 and G2) needs next.
