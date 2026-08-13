#!/usr/bin/env node
// Draws an annotated map of a scene straight from its manifest.
//
// Two audiences. Whoever is reviewing a blockout needs to know which grey box is
// the counter and which is a biscuit. And an illustrator needs a brief: every
// object that must be drawn, at the size it occupies, with the mechanics it has to
// support — a cuttable shape needs a clean silhouette, a paintable one needs flat
// fillable regions.
//
// Colours match SceneBuilder's, so the map lines up with a greybox screenshot.
//
// Usage: node tools/scene-map.mjs [sceneId] > map.svg

import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const sceneId = process.argv[2] ?? 'kitchen';

const scene = JSON.parse(readFileSync(join(ROOT, `content/scenes/${sceneId}/manifest.json`), 'utf8'));
const strings = JSON.parse(readFileSync(join(ROOT, 'content/i18n/tr.json'), 'utf8'));

const { width, height } = scene.canvas;
const MARGIN = 40;
const LEGEND_HEIGHT = 150;

// Same hash and palette as SceneBuilder.ColourFor, so a prop is the same colour here
// as it is on screen.
function colourFor(id) {
  let hash = 17;
  for (const character of id) hash = (Math.imul(hash, 31) + character.charCodeAt(0)) | 0;
  return hsvToRgb(Math.abs(hash % 360) / 360, 0.45, 0.85);
}

function hsvToRgb(h, s, v) {
  const i = Math.floor(h * 6);
  const f = h * 6 - i;
  const p = v * (1 - s);
  const q = v * (1 - f * s);
  const t = v * (1 - (1 - f) * s);
  const [r, g, b] = [
    [v, t, p], [q, v, p], [p, v, t], [p, q, v], [t, p, v], [v, p, q],
  ][i % 6];
  const channel = (value) => Math.round(value * 255).toString(16).padStart(2, '0');
  return `#${channel(r)}${channel(g)}${channel(b)}`;
}

const ZONE_STYLE = {
  snap: { fill: '#3399ff', label: 'yapıştır' },
  grid: { fill: '#ffbf1a', label: 'ızgara' },
  paintArea: { fill: '#33d966', label: 'boyama' },
};

const MECHANIC_LABEL = { cut: 'kes', glue: 'yapıştır', paint: 'boya', build: 'inşa et' };

const escape = (text) => String(text).replace(/[<>&]/g, (c) => ({ '<': '&lt;', '>': '&gt;', '&': '&amp;' }[c]));

const parts = [];

parts.push(`<rect x="0" y="0" width="${width + MARGIN * 2}" height="${height + MARGIN * 2 + LEGEND_HEIGHT}" fill="#1b1b1b"/>`);
parts.push(`<g transform="translate(${MARGIN},${MARGIN})">`);
parts.push(`<rect x="0" y="0" width="${width}" height="${height}" fill="#efece4"/>`);

for (const zone of scene.zones ?? []) {
  const style = ZONE_STYLE[zone.type] ?? { fill: '#888888', label: zone.type };
  const { x, y, width: w, height: h } = zone.shape;
  parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${style.fill}" fill-opacity="0.16" stroke="${style.fill}" stroke-width="3" stroke-dasharray="12 8"/>`);
  parts.push(`<text x="${x + 8}" y="${y + 26}" font-family="system-ui,sans-serif" font-size="20" font-weight="600" fill="${style.fill}">${escape(zone.id)} · ${style.label}${zone.grid ? ` ${zone.grid.columns}×${zone.grid.rows}` : ''}</text>`);
}

const ordered = [...(scene.objects ?? [])].sort((a, b) => {
  const order = (id) => (scene.layers.find((layer) => layer.id === id)?.order ?? 0);
  return order(a.layer) - order(b.layer);
});

for (const object of ordered) {
  const w = object.placeholderSize?.width ?? 120;
  const h = object.placeholderSize?.height ?? 120;
  const x = object.transform.x - w / 2;
  const y = object.transform.y - h / 2;
  const mechanics = (object.mechanics ?? []).map((m) => MECHANIC_LABEL[m] ?? m).join(', ');

  parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" fill="${colourFor(object.id)}" stroke="#00000033" stroke-width="2"/>`);
  parts.push(`<text x="${x + w / 2}" y="${y + h / 2 - (mechanics ? 6 : -6)}" text-anchor="middle" font-family="system-ui,sans-serif" font-size="21" font-weight="600" fill="#1b1b1b">${escape(object.id)}</text>`);
  if (mechanics) {
    parts.push(`<text x="${x + w / 2}" y="${y + h / 2 + 20}" text-anchor="middle" font-family="system-ui,sans-serif" font-size="17" fill="#1b1b1bcc">${escape(mechanics)}</text>`);
  }
}

for (const character of scene.characters ?? []) {
  const size = 180;
  const x = character.transform.x - size / 2;
  const y = character.transform.y - size / 2;
  parts.push(`<rect x="${x}" y="${y}" width="${size}" height="${size}" fill="#ffffff" fill-opacity="0.75" stroke="#1b1b1b" stroke-width="3" stroke-dasharray="10 6"/>`);
  parts.push(`<text x="${x + size / 2}" y="${y + size / 2}" text-anchor="middle" font-family="system-ui,sans-serif" font-size="22" font-weight="600" fill="#1b1b1b">${escape(character.id)}</text>`);
  parts.push(`<text x="${x + size / 2}" y="${y + size / 2 + 24}" text-anchor="middle" font-family="system-ui,sans-serif" font-size="17" fill="#1b1b1bcc">karakter · ${(character.reactions ?? []).length} tepki</text>`);
}

parts.push('</g>');

const title = strings[scene.titleKey] ?? scene.id;
let legendY = height + MARGIN + 46;
parts.push(`<text x="${MARGIN}" y="${legendY}" font-family="system-ui,sans-serif" font-size="30" font-weight="700" fill="#f5f2ea">${escape(title)} — ${escape(scene.id)} · ${width}×${height}</text>`);

legendY += 34;
parts.push(`<text x="${MARGIN}" y="${legendY}" font-family="system-ui,sans-serif" font-size="20" fill="#c9c4b8">${scene.objects.length} nesne · ${(scene.zones ?? []).length} bölge · çizilecek her kutu bir sanat işi</text>`);

legendY += 40;
let legendX = MARGIN;
for (const [type, style] of Object.entries(ZONE_STYLE)) {
  parts.push(`<rect x="${legendX}" y="${legendY - 18}" width="26" height="26" fill="${style.fill}" fill-opacity="0.16" stroke="${style.fill}" stroke-width="3"/>`);
  parts.push(`<text x="${legendX + 36}" y="${legendY}" font-family="system-ui,sans-serif" font-size="20" fill="#c9c4b8">${style.label}</text>`);
  legendX += 200;
}

const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${width + MARGIN * 2}" height="${height + MARGIN * 2 + LEGEND_HEIGHT}" viewBox="0 0 ${width + MARGIN * 2} ${height + MARGIN * 2 + LEGEND_HEIGHT}">${parts.join('')}</svg>`;

const output = join(ROOT, 'screenshots', `${sceneId}-map.svg`);
mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, svg);
console.log(output);
