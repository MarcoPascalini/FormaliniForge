from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

out = Path(__file__).resolve().parents[1] / 'FormaliniForge' / 'Assets'
out.mkdir(parents=True, exist_ok=True)
s = 1024
im = Image.new('RGBA', (s, s), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
for y in range(s):
    t = y / s
    d.line((0, y, s, y), fill=(13 + int(10*t), 27 + int(16*t), 49 + int(27*t), 255))
mask = Image.new('L', (s, s), 0)
ImageDraw.Draw(mask).rounded_rectangle((28, 28, 996, 996), radius=205, fill=255)
im.putalpha(mask)
d = ImageDraw.Draw(im)
d.rounded_rectangle((72, 72, 952, 952), radius=170, outline=(91, 130, 181, 150), width=12)
d.polygon([(253, 246), (756, 246), (700, 342), (373, 342), (373, 475), (641, 475), (586, 570), (373, 570), (373, 781), (253, 781)], fill=(215, 238, 255, 255))
d.polygon([(720, 434), (809, 434), (723, 591), (821, 591), (553, 842), (641, 643), (548, 643)], fill=(50, 196, 237, 255))
d.ellipse((785, 191, 836, 242), fill=(71, 209, 245, 255))
im.save(out / 'FormaliniForge.ico', format='ICO', sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
im.resize((256,256), Image.Resampling.LANCZOS).save(out / 'FormaliniForge.png')
