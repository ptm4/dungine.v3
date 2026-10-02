#!/usr/bin/env bash
# cap.sh <subject> <action> <name> [view] [frames] [fps] [size] [zoom] : runs DevCapture in Play mode and waits for it
cd "/e/Unity/Projects/dungine.v2"
subj=$1; act=$2; name=$3; view=${4:-side}; frames=${5:-48}; fps=${6:-24}; size=${7:-420}; zoom=${8:-1}
rm -f "DevCaptures/$name/done.txt"
unity command eval "return Dungine.DevCapture.Run(\"$subj\", \"$act\", \"$name\", \"$view\", $frames, ${fps}f, $size, ${zoom}f);" 2>&1 | grep -o '"result":"[^"]*"\|error[^,]*' | head -2
for i in $(seq 1 90); do [ -f "DevCaptures/$name/done.txt" ] && break; sleep 1; done
echo "$name: $(cat DevCaptures/$name/done.txt 2>/dev/null)"
