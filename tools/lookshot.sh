#!/usr/bin/env bash
# lookshot.sh <name> <wait-seconds> '<C#>' : runs C# in the playing game (tools/ev.sh), waits, then captures the
# Game view to Assets/Captures/<name>.png. Look test (phase 3) helper; the game must already be in Play mode.
cd "/e/Unity/Projects/dungine.v3"
name=$1; wait=${2:-2}; code=$3
if [ -n "$code" ]; then bash tools/ev.sh "$code"; fi
sleep "$wait"
# capture at the game view's own size, so no pixel is resampled (the default would squeeze it to 1280 x 720)
size=$(bash tools/ev.sh 'return UnityEngine.Screen.width + " " + UnityEngine.Screen.height;')
w=${size%% *}; h=${size##* }
unity command capture_game_view --save_path "Captures/$name.png" --source screen --width "$w" --height "$h" 2>&1 | grep -o '"width":[0-9]*,"height":[0-9]*' | sed "s/^/$name: /"
