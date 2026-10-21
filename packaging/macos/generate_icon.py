#!/usr/bin/env python3
"""Generate a tiny, scalable pixel-grid app icon using only Python's standard library."""
import math
import struct
import sys
import zlib

SIZE = 1024

def rounded_box(x, y, left, top, right, bottom, radius):
    dx = max(left + radius - x, 0, x - (right - radius))
    dy = max(top + radius - y, 0, y - (bottom - radius))
    return dx * dx + dy * dy <= radius * radius

def pixel(x, y):
    if not rounded_box(x, y, 48, 48, 976, 976, 210):
        return (0, 0, 0, 0)
    gradient = int((x + y) / (2 * SIZE) * 32)
    blue = (max(10, 38 - gradient), max(60, 111 - gradient // 2), max(160, 249 - gradient // 3), 255)
    # White enclosure with eight OLED pixels, scaled for the high-density dock icon.
    if rounded_box(x, y, 166, 326, 858, 698, 62):
        blue = (236, 244, 255, 255)
    if rounded_box(x, y, 190, 350, 834, 674, 44):
        blue = (26, 50, 100, 255)
    for col in range(8):
        for row in range(4):
            left = 237 + col * 73
            top = 390 + row * 68
            if rounded_box(x, y, left, top, left + 48, top + 43, 10):
                accent = 122 + row * 10
                return (accent, min(250, 185 + col * 6), 255, 255)
    return blue

def chunk(name, data):
    return struct.pack("!I", len(data)) + name + data + struct.pack("!I", zlib.crc32(name + data) & 0xffffffff)

def main(path):
    rows = bytearray()
    for y in range(SIZE):
        rows.append(0)
        for x in range(SIZE):
            rows.extend(pixel(x, y))
    raw = zlib.compress(bytes(rows), 9)
    png = b"\x89PNG\r\n\x1a\n"
    png += chunk(b"IHDR", struct.pack("!IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
    png += chunk(b"IDAT", raw)
    png += chunk(b"IEND", b"")
    with open(path, "wb") as file:
        file.write(png)

if __name__ == "__main__":
    main(sys.argv[1])
