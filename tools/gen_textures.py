"""Generate the channeling VFX textures without any image libraries.

  rune_ring.png  512x512 RGBA  white ring + tick marks + rune-ish glyphs, alpha-only art
  mote.png        32x32  RGBA  soft radial dot for the particle system

Run: python3 tools/gen_textures.py   (writes into src/Resources/)
"""
import math, random, struct, zlib, pathlib

OUT = pathlib.Path(__file__).resolve().parent.parent / "src" / "Resources"

def write_png(path, w, h, rgba_rows):
    raw = b"".join(b"\x00" + bytes(row) for row in rgba_rows)
    def chunk(tag, data):
        c = struct.pack(">I", len(data)) + tag + data
        return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9))
    png += chunk(b"IEND", b"")
    path.write_bytes(png)

def smooth(x):  # 0..1 -> 0..1 smoothstep
    x = max(0.0, min(1.0, x)); return x * x * (3 - 2 * x)

def ring(r, r0, r1, soft):
    """1 inside [r0,r1], soft falloff of width `soft` outside."""
    if r < r0: return smooth(1 - (r0 - r) / soft)
    if r > r1: return smooth(1 - (r - r1) / soft)
    return 1.0

def gen_ring(size=512, seed=7):
    rnd = random.Random(seed)
    c = size / 2
    R = size * 0.48
    # glyph "strokes" in polar space: (angle_deg, radial_frac_0..1, length_deg, thickness)
    glyphs = []
    n_glyphs = 24
    for i in range(n_glyphs):
        a = i * 360 / n_glyphs + rnd.uniform(-2, 2)
        kind = rnd.choice(["bar", "bar", "chevron", "dot", "fork"])
        glyphs.append((a, kind, rnd.uniform(0.75, 0.83)))
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            dx, dy = x - c, y - c
            r = math.hypot(dx, dy) / R            # 0 center .. 1 edge
            ang = (math.degrees(math.atan2(dy, dx)) + 360) % 360
            a = 0.0
            # outer band + inner band
            a = max(a, 0.95 * ring(r, 0.93, 0.965, 0.02))
            a = max(a, 0.80 * ring(r, 0.865, 0.885, 0.015))
            a = max(a, 0.70 * ring(r, 0.60, 0.615, 0.015))
            # 72 tick marks on the outer band
            t = (ang % 5.0)
            if 0.905 < r < 0.93 and (t < 0.7 or t > 4.3):
                a = max(a, 0.9)
            # 8 long ticks
            t8 = (ang % 45.0)
            if 0.62 < r < 0.86 and (t8 < 0.6 or t8 > 44.4):
                a = max(a, 0.55)
            # glyphs between the middle bands
            for (ga, kind, gr) in glyphs:
                da = (ang - ga + 180) % 360 - 180   # signed angular distance
                rr = (r - gr) / 0.10                # -1..1 across the glyph band
                if abs(da) > 6 or abs(rr) > 1.2: continue
                v = 0.0
                if kind == "bar":
                    v = 1.0 if abs(da) < 0.9 and abs(rr) < 1.0 else 0.0
                elif kind == "chevron":
                    v = 1.0 if abs(abs(da) - (1 - abs(rr)) * 3.5) < 0.9 else 0.0
                elif kind == "dot":
                    v = smooth(1 - (math.hypot(da / 1.4, rr / 0.35)))
                elif kind == "fork":
                    v = 1.0 if (abs(da) < 0.8 and rr > -1.0) or (abs(abs(da) - 2.4) < 0.8 and rr > 0.2) else 0.0
                a = max(a, 0.85 * v)
            # faint inner glow disc so the ring reads as "lit" on the ground
            a = max(a, 0.12 * smooth(1 - r / 0.6))
            v8 = int(255 * max(0.0, min(1.0, a)))
            row += [255, 255, 255, v8]
        rows.append(row)
    return rows

def gen_mote(size=32):
    c = (size - 1) / 2
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            d = math.hypot(x - c, y - c) / (size / 2)
            a = smooth(1 - d) ** 2
            row += [255, 255, 255, int(255 * a)]
        rows.append(row)
    return rows

OUT.mkdir(parents=True, exist_ok=True)
write_png(OUT / "rune_ring.png", 512, 512, gen_ring())
write_png(OUT / "mote.png", 32, 32, gen_mote())
print("wrote", OUT / "rune_ring.png", OUT / "mote.png")

# --- Thunderstore icon: the ring on a dark background, tinted like the in-game default.
def gen_icon(size=256):
    ring = gen_ring(size)
    rows = []
    for y in range(size):
        row = []
        for x in range(size):
            a = ring[y][x * 4 + 3] / 255
            # dark blue-black background with a faint vignette glow toward the center
            d = math.hypot(x - size / 2, y - size / 2) / (size / 2)
            bg = (14 + int(18 * smooth(1 - d)), 18 + int(24 * smooth(1 - d)), 30 + int(34 * smooth(1 - d)))
            tint = (0x7F, 0xD7, 0xFF)
            row += [int(bg[i] * (1 - a) + tint[i] * a) for i in range(3)] + [255]
        rows.append(row)
    return rows

write_png(OUT.parent.parent / "package" / "icon.png", 256, 256, gen_icon())
print("wrote", OUT.parent.parent / "package" / "icon.png")
