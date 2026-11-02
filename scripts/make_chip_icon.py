# SPDX-License-Identifier: MIT
# Copyright (c) 2026 Rapidfire
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parents[1] / 'assets'
scale = 4
image = Image.new('RGBA', (256*scale, 256*scale), (0,0,0,0))
draw = ImageDraw.Draw(image)
def box(rect,fill,radius=0,outline=None,width=1):
    rect=tuple(int(v*scale) for v in rect)
    if radius:
        draw.rounded_rectangle(rect,radius=radius*scale,fill=fill,outline=outline,width=width*scale)
    else:
        draw.rectangle(rect,fill=fill,outline=outline,width=width*scale)

bg=(17,19,16,255); lime=(194,243,107,255); edge=(83,105,54,255)
box((12,12,244,244),bg,46,outline=(55,65,45,255),width=3)
# Eight pins on each side give the silhouette of a DIP memory IC.
for y in range(65,202,19):
    box((28,y,60,y+10),lime,3)
    box((196,y,228,y+10),lime,3)
box((54,45,202,215),(37,44,29,255),17,outline=lime,width=5)
box((65,56,191,204),(23,28,19,255),10,outline=edge,width=2)
# Package notch and orientation dot.
draw.ellipse(tuple(v*scale for v in (113,38,143,69)),fill=bg)
draw.arc(tuple(v*scale for v in (113,38,143,69)),start=0,end=180,fill=lime,width=4*scale)
draw.ellipse(tuple(v*scale for v in (77,76,85,84)),fill=lime)
# A simple memory-cell matrix stays recognisable at small icon sizes.
for row in range(3):
    for col in range(3):
        x=85+col*31;y=101+row*29
        box((x,y,x+22,y+18),lime if (row+col)%2==0 else (116,151,72,255),3)
image=image.resize((256,256),Image.Resampling.LANCZOS)
image.save(out/'BurnModern-chip.png')
image.save(out/'BurnModern-chip.ico',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
with Image.open(out/'BurnModern-chip.ico') as icon:
    expected={(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)}
    assert expected.issubset(icon.ico.sizes())
print('Created PNG and ICO with seven verified icon sizes.')
