#!/usr/bin/env bash
# lookmeasure.sh <label> '<C# setup>' : runs the setup C#, waits 4 s, then LookTest.Measure(600) and prints the result.
# Look test (phase 3) helper; the game must already be in Play mode.
cd "/e/Unity/Projects/dungine.v3"
label=$1; code=$2
if [ -n "$code" ]; then bash tools/ev.sh "$code" >/dev/null; fi
sleep 4
bash tools/ev.sh 'return Dungine.LookTest.LookTest.Measure(600);' >/dev/null
for i in $(seq 1 120); do
  r=$(bash tools/ev.sh 'return Dungine.LookTest.LookTest.Report;')
  [ "$r" != "measuring" ] && break
  sleep 1
done
echo "$label: $r"
