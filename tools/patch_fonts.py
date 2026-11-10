#!/usr/bin/env python3
"""Añade a las fuentes del juego los caracteres del castellano que les faltan.

Se ejecuta una sola vez desde la raíz del repo; el resultado (PNG y prefabs) queda subido.
Crea á é í ó ú ñ ü (y mayúsculas) dibujando el acento sobre la letra original, y
¿ ¡ reutilizando ? ! girados 180°. Las fuentes que ya los tienen no se tocan.
"""
import math
import re
import sys

from PIL import Image

ASSETS = "UnityProject/Assets"
FONTS = [
    ("destroy data", "Texture2D/atlas0_5.png", True),
    ("Destroy24Fontdata", "Resources/fonts/destroy24_0.png", True),
    ("UIDemoOldSansBlack data", "Texture2D/atlas0_46.png", False),  # solo ¡ ¿: el atlas está lleno
]
ACCENTED = {
    "á": ("a", "acute"), "é": ("e", "acute"), "í": ("i", "acute"), "ó": ("o", "acute"), "ú": ("u", "acute"),
    "ñ": ("n", "tilde"), "ü": ("u", "dieresis"),
    "Á": ("A", "acute"), "É": ("E", "acute"), "Í": ("I", "acute"), "Ó": ("O", "acute"), "Ú": ("U", "acute"),
    "Ñ": ("N", "tilde"), "Ü": ("U", "dieresis"),
}
TURNED = {"¡": "!", "¿": "?"}
CHAR = re.compile(
    r"  - p0: \{x: (?P<px0>[^,]+), y: (?P<py0>[^,]+), z: [^}]+\}\n"
    r"    p1: \{x: (?P<px1>[^,]+), y: (?P<py1>[^,]+), z: [^}]+\}\n"
    r"    uv0: \{x: (?P<u0>[^,]+), y: (?P<v0>[^,]+), z: [^}]+\}\n"
    r"    uv1: \{x: (?P<u1>[^,]+), y: (?P<v1>[^,]+), z: [^}]+\}\n"
    r"    flipped: (?P<flipped>\d)\n"
    r"    gradientUv:(?P<grad>(?: \[\])?(?:\n    - \{[^\n]*)*)\n"
    r"    advance: (?P<advance>[^\n]+)\n"
    r"    channel: (?P<channel>\d+)\n"
)


def fmt(c):
    return (
        f"  - p0: {{x: {c['px0']}, y: {c['py0']}, z: 0}}\n"
        f"    p1: {{x: {c['px1']}, y: {c['py1']}, z: 0}}\n"
        f"    uv0: {{x: {c['u0']}, y: {c['v0']}, z: 0}}\n"
        f"    uv1: {{x: {c['u1']}, y: {c['v1']}, z: 0}}\n"
        f"    flipped: {c['flipped']}\n"
        f"    gradientUv:{c['grad']}\n"
        f"    advance: {c['advance']}\n"
        f"    channel: {c['channel']}\n"
    )


def glyph_image(atlas, c):
    """Devuelve la letra tal como se ve en pantalla, sea cual sea su orientación en el atlas."""
    W, H = atlas.size
    u0, v0, u1, v1 = (float(c[k]) for k in ("u0", "v0", "u1", "v1"))
    if c["flipped"] == "1":
        w, h = round(abs(v1 - v0) * H), round(abs(u1 - u0) * W)
    else:
        w, h = round(abs(u1 - u0) * W), round(abs(v1 - v0) * H)
    out = Image.new("RGBA", (w, h), (255, 255, 255, 0))
    for sy in range(h):
        for sx in range(w):
            fx, fy = (sx + 0.5) / w, (sy + 0.5) / h
            if c["flipped"] == "1":
                u, v = u1 + (u0 - u1) * fy, v1 + (v0 - v1) * fx
            else:
                u, v = u0 + (u1 - u0) * fx, v0 + (v1 - v0) * fy
            out.putpixel((sx, sy), atlas.getpixel((min(int(u * W), W - 1), min(int((1 - v) * H), H - 1))))
    return out


def remove_i_dot(img):
    """Quita el punto de la i para que no se pise con el acento."""
    a = img.getchannel("A")
    w, h = img.size
    rows = [any(a.getpixel((x, y)) > 40 for x in range(w)) for y in range(h)]
    first = next((y for y, r in enumerate(rows) if r), None)
    if first is None:
        return
    for y in range(first, int(h * 0.55)):
        if not rows[y]:
            for yy in range(0, y):
                for x in range(w):
                    img.putpixel((x, yy), (255, 255, 255, 0))
            return


def accent_rows(glyph_h):
    return max(4, round(glyph_h * 0.24))


def draw_mark(img, kind, top, rows, cx):
    w, _ = img.size
    white = (255, 255, 255, 255)
    thick = max(2, round(rows * 0.45))

    def blot(x, y):
        for yy in range(y, y + thick):
            for xx in range(x, x + thick):
                if 0 <= xx < w and top <= yy < top + rows:
                    img.putpixel((xx, yy), white)

    if kind == "acute":
        span = max(3, rows)
        for i in range(rows * 3):
            t = i / max(1, rows * 3 - 1)
            blot(round(cx - span / 2 + span * t - thick / 2), round(top + (rows - thick) * (1 - t)))
    elif kind == "tilde":
        span = max(6, int(w * 0.75))
        for i in range(span * 3):
            t = i / max(1, span * 3 - 1)
            y = top + (rows - thick) * (0.5 - 0.5 * math.sin(t * 2 * math.pi))
            blot(round(cx - span / 2 + span * t - thick / 2), round(y))
    else:
        gap = max(thick + 2, int(w * 0.34))
        for x in (cx - gap // 2 - thick // 2, cx + gap // 2 - thick // 2 + 1):
            for yy in range(top, top + min(rows, thick + 1)):
                for xx in range(x, x + thick):
                    if 0 <= xx < w:
                        img.putpixel((xx, yy), white)


def find_shelf_space(atlas, sizes):
    """Coloca los glifos en filas totalmente vacías del atlas."""
    W, H = atlas.size
    alpha = atlas.getchannel("A")
    empty = [not any(alpha.getpixel((x, y)) for x in range(W)) for y in range(H)]
    max_h = max(h for _, h in sizes) + 2
    start = None
    run = 0
    bands = []
    for y in range(H + 1):
        if y < H and empty[y]:
            if start is None:
                start = y
            run += 1
        else:
            if start is not None and run >= max_h + 2:
                bands.append((start + 1, y - 1))
            start, run = None, 0
    if not bands:
        sys.exit("No hay filas vacías en el atlas")
    band = iter(bands)
    y0, y1 = next(band)
    x, y = 2, y0
    places = []
    for w, h in sizes:
        if x + w + 2 > W:
            x, y = 2, y + max_h
        if y + h + 1 > y1:
            y0, y1 = next(band)
            x, y = 2, y0
        places.append((x, y))
        x += w + 2
    return places


def patch(prefab_name, atlas_rel, draw):
    path = f"{ASSETS}/GameObject/{prefab_name}.prefab"
    text = open(path, encoding="utf-8", newline="").read()
    start = text.index("  chars:\n") + len("  chars:\n")
    end = text.index("  charDictKeys:", start)
    chars = [m.groupdict() for m in CHAR.finditer(text[start:end])]
    if fmt(chars[0]) != text[start:start + len(fmt(chars[0]))].replace(": 0}", ": 0}"):
        pass
    if len(chars) >= 256:
        sys.exit(f"{prefab_name} ya está parcheada")
    empty = dict(chars[0])
    chars += [dict(empty) for _ in range(256 - len(chars))]

    for new, base in TURNED.items():
        c = dict(chars[ord(base)])
        c["u0"], c["u1"] = c["u1"], c["u0"]
        c["v0"], c["v1"] = c["v1"], c["v0"]
        chars[ord(new)] = c

    if draw:
        atlas_path = f"{ASSETS}/{atlas_rel}"
        atlas = Image.open(atlas_path).convert("RGBA")
        W, H = atlas.size
        new_glyphs = []
        for ch, (base, kind) in ACCENTED.items():
            c = chars[ord(base)]
            img = glyph_image(atlas, c)
            if base == "i":
                remove_i_dot(img)
            w, h = img.size
            bbox = img.getchannel("A").getbbox() or (0, 0, w, h)
            rows = accent_rows(h)
            gap = 1
            room = max(0, rows + gap - bbox[1])  # filas extra por encima de la letra
            canvas = Image.new("RGBA", (w, h + room), (255, 255, 255, 0))
            canvas.paste(img, (0, room))
            cx = (bbox[0] + bbox[2]) // 2
            draw_mark(canvas, kind, max(0, bbox[1] + room - rows - gap), rows, cx)
            new_glyphs.append((ch, c, canvas, room))
        places = find_shelf_space(atlas, [g[2].size for g in new_glyphs])
        for (ch, c, canvas, room), (x, y) in zip(new_glyphs, places):
            atlas.paste(canvas, (x, y))
            cw, ch_px = canvas.size
            base_h = ch_px - room
            py0, py1 = float(c["py0"]), float(c["py1"])
            per_px = abs(py0 - py1) / base_h
            n = dict(c)
            n["py0"] = repr(round(py0 + room * per_px, 6))
            n["u0"], n["v0"] = repr(x / W), repr(1 - y / H)
            n["u1"], n["v1"] = repr((x + cw) / W), repr(1 - (y + ch_px) / H)
            n["flipped"] = "0"
            chars[ord(ch)] = n
        atlas.save(atlas_path)

    text = text[:start] + "".join(fmt(c) for c in chars) + text[end:]
    open(path, "w", encoding="utf-8", newline="").write(text)
    print(prefab_name, "OK")


if __name__ == "__main__":
    for font in FONTS:
        patch(*font)
