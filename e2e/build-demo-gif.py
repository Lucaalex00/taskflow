"""Assembles the frames from capture-demo-frames.mjs into docs/screenshots/demo.gif.

Uses PIL rather than ffmpeg so regenerating the README animation needs nothing beyond what
the repo already implies (Python + Pillow). Tuned for a file small enough to sit at the top of
a README without punishing anyone's connection: half-width, a shared adaptive palette, and
identical consecutive frames collapsed into a longer delay for the one before them.

    cd e2e && node capture-demo-frames.mjs && python build-demo-gif.py
"""
from pathlib import Path

from PIL import Image, ImageChops

FRAME_DIR = Path(__file__).parent / ".demo-frames"
OUTPUT = Path(__file__).parent.parent / "docs" / "screenshots" / "demo.gif"

TARGET_WIDTH = 760
FRAME_MS = 110
PALETTE_COLORS = 96
# Below this fraction of changed pixels a frame is treated as a repeat of the one before it,
# which collapses the deliberate pauses into a single long-delay frame instead of a dozen
# near-identical ones. Purely a size optimisation — the animation looks the same.
STILL_THRESHOLD = 0.002


def load_frames():
    paths = sorted(FRAME_DIR.glob("f*.png"))
    if not paths:
        raise SystemExit(f"No frames in {FRAME_DIR}. Run: node capture-demo-frames.mjs")

    for path in paths:
        with Image.open(path) as image:
            frame = image.convert("RGB")
            height = round(frame.height * TARGET_WIDTH / frame.width)
            yield frame.resize((TARGET_WIDTH, height), Image.LANCZOS)


def changed_fraction(a, b):
    diff = ImageChops.difference(a, b).convert("L")
    changed = sum(count for value, count in enumerate(diff.histogram()) if value > 12)
    return changed / (diff.width * diff.height)


def main():
    kept, durations = [], []

    for frame in load_frames():
        if kept and changed_fraction(kept[-1], frame) < STILL_THRESHOLD:
            durations[-1] += FRAME_MS  # hold the previous frame longer instead of storing a twin
            continue
        kept.append(frame)
        durations.append(FRAME_MS)

    # One palette for the whole animation: per-frame palettes make colours shimmer between
    # frames, which is very visible on flat UI surfaces.
    palette = kept[0].quantize(colors=PALETTE_COLORS, method=Image.MEDIANCUT)
    quantized = [f.quantize(palette=palette, dither=Image.FLOYDSTEINBERG) for f in kept]

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    quantized[0].save(
        OUTPUT,
        save_all=True,
        append_images=quantized[1:],
        duration=durations,
        loop=0,
        optimize=True,
        disposal=2,
    )

    size_mb = OUTPUT.stat().st_size / 1_000_000
    print(f"Wrote {OUTPUT} — {len(quantized)} frames, {size_mb:.2f} MB")


if __name__ == "__main__":
    main()
