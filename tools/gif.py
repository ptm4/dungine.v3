"""gif.py out.gif name1 [name2 ...] [--labels "A|B"] [--fps 24]
Turns DevCaptures/<name>/f###.png sequences into one GIF (several captures side by side, looped to the longest)."""
import sys, os, glob
from PIL import Image, ImageDraw, ImageFont
args = sys.argv[1:]
out = args.pop(0)
labels = None; fps = 24; scale = 1.0
names = []
i = 0
while i < len(args):
    if args[i] == '--labels': labels = args[i+1].split('|'); i += 2
    elif args[i] == '--fps': fps = float(args[i+1]); i += 2
    elif args[i] == '--scale': scale = float(args[i+1]); i += 2
    else: names.append(args[i]); i += 1
root = os.path.join(os.path.dirname(__file__), '..', 'DevCaptures')
seqs = []
for n in names:
    fs = sorted(glob.glob(os.path.join(root, n, 'f*.png')))
    seqs.append([Image.open(f).convert('RGB') for f in fs])
w, h = seqs[0][0].size
w2, h2 = int(w * scale), int(h * scale)
top = 26 if labels else 0
count = max(len(s) for s in seqs)
try: font = ImageFont.truetype('C:/Windows/Fonts/pala.ttf', 18)
except: font = ImageFont.load_default()
frames = []
for k in range(count):
    fr = Image.new('RGB', (w2 * len(seqs), h2 + top), (20, 16, 14))
    d = ImageDraw.Draw(fr)
    for j, s in enumerate(seqs):
        im = s[k % len(s)]
        if scale != 1: im = im.resize((w2, h2), Image.LANCZOS)
        fr.paste(im, (j * w2, top))
        if labels and j < len(labels): d.text((j * w2 + 8, 4), labels[j], fill=(230, 196, 120), font=font)
    frames.append(fr.quantize(colors=200, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
frames[0].save(out, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, optimize=True)
print(out, len(frames), 'frames', os.path.getsize(out) // 1024, 'KB')
