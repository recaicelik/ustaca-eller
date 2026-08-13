#!/usr/bin/env node
// Rasterises the vector artwork into sprites Unity can import.
//
// The SVG modules under art/ are the source; the PNGs are build output. Drawing in
// code rather than in a binary file means the palette is shared, a colour change is
// one edit, and a diff shows what actually changed.
//
// Rendered at 2x the manifest size so the art still holds up on a retina tablet.
//
// Usage: node tools/build-sprites.mjs

import { writeFileSync, mkdirSync, rmSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { sprites, characters } from '../art/kitchen-sprites.mjs';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const OUTPUT = join(ROOT, 'unity/Assets/Resources/Art/kitchen');
const SCALE = 2;

if (spawnSync('rsvg-convert', ['--version'], { encoding: 'utf8' }).status !== 0) {
  console.error('rsvg-convert not found. Install it with: brew install librsvg');
  process.exit(1);
}

rmSync(OUTPUT, { recursive: true, force: true });
mkdirSync(OUTPUT, { recursive: true });

const all = { ...sprites, ...characters };
let count = 0;

for (const [id, markup] of Object.entries(all)) {
  const size = markup.match(/width="(\d+)" height="(\d+)"/);
  if (!size) {
    console.error(`${id}: could not read the sprite size from its SVG`);
    process.exit(1);
  }

  const temporary = join(OUTPUT, `${id}.svg`);
  writeFileSync(temporary, markup);

  const png = join(OUTPUT, `${id}.png`);
  const render = spawnSync('rsvg-convert', [
    temporary,
    '-w', String(Number(size[1]) * SCALE),
    '-h', String(Number(size[2]) * SCALE),
    '-o', png,
  ], { encoding: 'utf8' });

  rmSync(temporary);

  if (render.status !== 0) {
    console.error(`${id}: ${render.stderr.trim()}`);
    process.exit(1);
  }

  console.log(`  ${id}  ${size[1]}x${size[2]} @${SCALE}x`);
  count++;
}

console.log(`\n${count} sprite(s) → unity/Assets/Resources/Art/kitchen`);
