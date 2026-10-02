#!/usr/bin/env bash
# ev.sh '<C# expression body>' — evaluates C# in the running editor and prints just the result.
cd "/e/Unity/Projects/dungine.v2"
unity command eval "$1" 2>&1 | grep '^eval' | python -c "
import sys,json
l=sys.stdin.read().split('\t')
try: print(json.loads(l[2])['result'])
except Exception as e: print('ERR', l[:3])"
