#!/usr/bin/env bash
# tour.sh area:spawn [area:spawn ...] — starts the premade party in the first area, then visits each in turn,
# capturing Assets/Captures/tour_<area>.png and printing new console errors after each.
cd "/e/Unity/Projects/dungine.v3"
first=1
for pair in "$@"; do
  area=${pair%%:*}; spawn=${pair##*:}
  if [ $first = 1 ]; then
    bash tools/play.sh "tour_$area" 8 "Dungine.Game.I.DevStart(\"$area\",\"$spawn\"); return \"ok\";" 16 | grep -v "^eval"
    first=0
  else
    unity command clear_console >/dev/null 2>&1
    bash tools/play.sh "tour_$area" 0 "Dungine.Game.I.GoToArea(\"$area\",\"$spawn\"); return \"ok\";" 16 | grep -v "^eval"
  fi
done
