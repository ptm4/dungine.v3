#!/usr/bin/env bash
# Build the dev lineup and capture a 2x2 grid of face close-ups: faces.sh <seed> <i1> <i2> <i3> <i4> [out]
cd "/e/Unity/Projects/dungine.v2"
SEED=${1:-3}; OUT=${6:-grid}
unity command eval "return Dungine.Dev.Lineup($SEED, false);" >/dev/null 2>&1
for i in $(seq 1 30); do
  r=$(unity command eval 'var r = UnityEngine.GameObject.Find("DevLineup"); return r ? r.GetComponentsInChildren<Dungine.Visual.HumanoidRig>().Length.ToString() : "0";' 2>&1 | tail -1)
  echo "$r" | grep -q '"result":"11"' && break
  sleep 2
done
unity command eval 'Dungine.Visual.Atmosphere.I.sun.transform.rotation = UnityEngine.Quaternion.Euler(30, 200, 0); return "ok";' >/dev/null 2>&1
for i in $2 $3 $4 $5; do
  unity command eval "return Dungine.Dev.Closeup($i, 0.8f);" >/dev/null 2>&1
  unity command capture_game_view --width 960 --height 720 --save_path "Captures/close$i.png" >/dev/null 2>&1
done
python -c "
from PIL import Image
ids=[$2,$3,$4,$5]
ims=[Image.open(f'Assets/Captures/close{i}.png') for i in ids]
w,h=ims[0].size
out=Image.new('RGB',(w*2,h*2))
for k,im in enumerate(ims): out.paste(im,((k%2)*w,(k//2)*h))
out.save('Assets/Captures/$OUT.png')"
echo "saved Assets/Captures/$OUT.png"
