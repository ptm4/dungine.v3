#!/usr/bin/env bash
# say.sh [pick ...] — for each argument, picks that option index in the active dialogue (c = continue = 0),
# waits for the next node to finish typing, and prints the dialogue state. With no args, just prints the state.
cd "/e/Unity/Projects/dungine.v3"
state() { unity command eval 'return Dungine.Dialogue.DialogueRunner.I.DevState();' 2>&1 | grep '^eval' | python -c "
import sys,json
l=sys.stdin.read().split('\t')
try: print(json.loads(l[2])['result'][:900])
except Exception as e: print(l[:3])"; }
if [ $# -eq 0 ]; then state; exit 0; fi
for p in "$@"; do
  [ "$p" = "c" ] && p=0
  unity command eval "Dungine.Dialogue.DialogueRunner.I.DevPick = $p; return \"ok\";" >/dev/null 2>&1
  sleep ${SAY_WAIT:-4}
  state
done
