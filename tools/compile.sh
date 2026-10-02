#!/usr/bin/env bash
# Refresh assets (triggers compile), wait for the compiler, print compile errors (dungine.v2)
cd "/e/Unity/Projects/dungine.v2"
unity command editor_stop >/dev/null 2>&1
unity command clear_console >/dev/null 2>&1
unity command eval 'UnityEditor.AssetDatabase.Refresh(); return "ok";' >/dev/null 2>&1
sleep 4
for i in $(seq 1 90); do
  s=$(unity command console_status 2>&1 | tail -1)
  if echo "$s" | grep -q '"compiling":false'; then break; fi
  sleep 2
done
sleep 2
s=$(unity command console_status 2>&1 | tail -1)
if echo "$s" | grep -q '"compilationFailed":true'; then
  echo "COMPILE FAILED"
  unity command console --level error --tail 40 2>&1 | tail -1 | python -c "
import sys,json
line=sys.stdin.read().split('\t')
try:
  d=json.loads(line[2])
  seen=set()
  for e in d.get('entries',[]):
    m=e['message'].split('\n')[0]
    if m not in seen: seen.add(m); print(m)
except Exception as ex: print(line)"
else
  echo "COMPILE OK"
fi
