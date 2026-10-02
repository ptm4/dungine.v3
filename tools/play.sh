#!/usr/bin/env bash
# play.sh <capture-name> [wait-seconds] [csharp-to-eval-after-boot] [seconds-after-eval]
# Enters Play mode (if needed), optionally runs C#, waits, captures the Game view to Assets/Captures/<name>.png,
# and prints any new console errors.
cd "/e/Unity/Projects/dungine.v3"
name=${1:-shot}; wait1=${2:-8}; code=$3; wait2=${4:-10}
st=$(unity command editor_status 2>&1 | tail -1)
if ! echo "$st" | grep -q '"isPlaying":true'; then
  unity command clear_console >/dev/null 2>&1
  unity command editor_play >/dev/null 2>&1
fi
sleep "$wait1"
# make sure the game has booted (domain reload can make entering Play slow)
for i in $(seq 1 30); do
  r=$(unity command eval 'return Dungine.Game.I != null && Dungine.Game.I.mode != Dungine.GameMode.Boot ? "up" : "boot";' 2>&1 | grep -o '"result":"[a-z]*"')
  if echo "$r" | grep -q up; then break; fi
  sleep 1
done
if [ -n "$code" ]; then
  unity command eval "$code" 2>&1 | tail -1 | cut -c1-600
  sleep "$wait2"
fi
unity command capture_game_view --save_path "Captures/$name.png" --source screen 2>&1 | tail -1 | cut -c1-160
unity command console --level error --tail 30 2>&1 | tail -1 | python -c "
import sys,json
l=sys.stdin.read().split('\t')
try:
  d=json.loads(l[2]); seen=set()
  for e in d['entries']:
    m=e['message'].strip()
    k=m.split('\n')[0]
    if k in seen: continue
    seen.add(k); print('ERR:', '\n  '.join(m.split('\n')[:6])[:900])
except Exception as ex: print(l[:3])"
