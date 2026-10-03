# Cast and strike (agent A, 2026-10-03)

Peter's rework of v2's cast and strike, in v3's copy of the animation code only. The page for Peter is `index.html`.

## What changed

- `Assets/Game/Scripts/Visual/HumanoidClips.cs`
  - `StancePose(Grip)`. Unarmed and OneHand use the guard: the left arm is straight out at chest height. The right
    upper arm is close in, at (-25,-10,0); the forearm is level forward, at -85. The hand is at -10 empty-handed, or
    at +80 holding a blade, which aims the blade forward and 25 degrees up. Shield uses the guard's right arm. TwoHand,
    Polearm, Bow and Crossbow keep v2's stances.
  - `Combo(g)` (Slash with Unarmed/OneHand/Shield): wind-up, swing left, swing right, chamber, stab.
    - `HandFor()` turns the hand so that v2's socket (blade square to the hand) puts the blade level along the arm.
    - Measured on the fighter rig, in degrees of yaw from forward (+ is the figure's right):
      - guard: forearm 8, blade 5 (25 up);
      - swing left: arm -71;
      - swing right: arm +71;
      - stab: arm 1, blade -6 (9 down).
  - `Stab(g, lunge)` is used for Thrust and Punch with those grips.
  - `CastThrow` (cast A) is used for CastPoint.
    - The steadying left arm's yaw stays between -2 and +5 through the clip; each key counters the chest's turn.
    - The throwing hand goes from the left armpit (-0.04, 1.34, 0.08) out to yaw -12, then +2.
  - `CastPush` (cast B) is used for CastRaise. The chest squares up; the hands meet at the chest at x ±0.13, then push
    level to z 0.75.
  - `HumanoidClips.Cast = 'B'` forces cast B for CastPoint as well (dev).
- `HumanoidAnimator.cs`
  - The stance comes from `StancePose`.
  - `Anim.Duration/Impact(a, grip)`: the combo lasts 1.9 s, with the impact at 0.74.
  - `InStance`.
  - `HoldForCast()`: for TwoHand or Polearm, during any cast act, the weapon goes to the left socket, upright, and
    blends back at the end.
    - The weapon stays parented to the right hand; only its world pose is set, and it is reset every frame.
    - `LibraryRigDriver.Tick` calls it again after its own arm changes.
- `LibraryRigDriver.cs`: the base-pose arm hold is off while the animator is in the combat stance (`InStance`), so the
  guard isn't overridden.
- `DevCapture.cs`:
  - `<subject>+<weapon>` (for example `lib:fighter+longsword` or `arkus+longsword`);
  - `lib:<id>` subjects;
  - a `game` view (the game camera's 55-degree pitch, figure side-on).

## Captures

`DevCaptures/f_{A,B,S,U}_{lib,v2}_{side,game}`, made with `tools/cap.sh`. The GIFs are built with `tools/gif.py` at a
scale of 0.75.

## Open

- The fighter's half-cape rides the right arm (agent N's review 17 weighting), so it reads as a red sleeve in both casts
  and in the stab. N suggests a short cape chain of its own on the arm.
- CastTouch, Heal and Bless are still v2's clips; only the IK is off during them, so the hands are free.
