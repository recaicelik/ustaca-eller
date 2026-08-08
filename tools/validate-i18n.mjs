#!/usr/bin/env node
// Validates the localization catalogs in content/i18n.
//
// Why this is a gate and not a chore: in this product almost nothing on screen is
// text — the age group cannot read. The localized assets that matter are voice-over
// and the parent zone. That makes translation gaps easy to miss by playing the app,
// so they have to be caught mechanically.
//
// Usage: node tools/validate-i18n.mjs [--root <dir>]
//        --root is for tests only; defaults to the repository root.

import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const REPO = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const rootFlagIndex = process.argv.indexOf('--root');
const ROOT = rootFlagIndex === -1 ? REPO : resolve(process.argv[rootFlagIndex + 1]);
const I18N_DIR = join(ROOT, 'content/i18n');
const SCENES_DIR = join(ROOT, 'content/scenes');

const KEY_PATTERN = /^[a-z][a-zA-Z0-9]*(\.[a-z][a-zA-Z0-9]*)+$/;
const PLACEHOLDER_PATTERN = /\{([a-zA-Z0-9_]+)\}/g;

const errors = [];
const warnings = [];

function readJson(path) {
  return JSON.parse(readFileSync(path, 'utf8'));
}

function placeholders(text) {
  return new Set([...text.matchAll(PLACEHOLDER_PATTERN)].map((match) => match[1]));
}

const config = readJson(join(I18N_DIR, 'locales.json'));
const sourceLocale = config.sourceLocale;
const sourcePath = join(I18N_DIR, `${sourceLocale}.json`);

if (!existsSync(sourcePath)) {
  console.log(`✗ source locale catalog missing: content/i18n/${sourceLocale}.json`);
  process.exit(1);
}

const source = readJson(sourcePath);
const sourceKeys = Object.keys(source).filter((key) => key !== '$comment');

// ------------------------------------------------------- source catalog rules
// Key naming is only meaningful for the source catalog — every other locale is
// checked against these keys. Value checks live in the per-locale loop below,
// which covers the source locale too.
for (const key of sourceKeys) {
  if (!KEY_PATTERN.test(key)) {
    errors.push(`${sourceLocale}: key "${key}" does not follow the dotted lowerCamel convention`);
  }
}

// --------------------------------------------------------- per-locale checks
const summaries = [];

for (const locale of config.locales) {
  const path = join(I18N_DIR, `${locale.code}.json`);
  if (!existsSync(path)) {
    errors.push(`locale "${locale.code}" is declared in locales.json but content/i18n/${locale.code}.json is missing`);
    continue;
  }

  const catalog = readJson(path);
  const keys = Object.keys(catalog).filter((key) => key !== '$comment');
  const missing = sourceKeys.filter((key) => !(key in catalog));
  const orphaned = keys.filter((key) => !sourceKeys.includes(key));

  // A shipping locale must be complete. A non-shipping one may lag behind — the
  // fallback chain covers it — but the gap is reported so it never surprises us.
  for (const key of missing) {
    const message = `${locale.code}: missing translation for "${key}"`;
    if (locale.shipping) errors.push(message);
    else warnings.push(message);
  }

  for (const key of orphaned) {
    errors.push(`${locale.code}: "${key}" does not exist in the source locale (${sourceLocale}) — stale key`);
  }

  for (const key of keys) {
    if (typeof catalog[key] !== 'string' || catalog[key].trim() === '') {
      errors.push(`${locale.code}: "${key}" is empty or not a string`);
      continue;
    }
    if (!(key in source)) continue;

    const expected = placeholders(source[key]);
    const actual = placeholders(catalog[key]);
    const dropped = [...expected].filter((name) => !actual.has(name));
    const invented = [...actual].filter((name) => !expected.has(name));
    if (dropped.length || invented.length) {
      errors.push(
        `${locale.code}: "${key}" placeholder mismatch` +
        `${dropped.length ? ` — missing {${dropped.join('}, {')}}` : ''}` +
        `${invented.length ? ` — unexpected {${invented.join('}, {')}}` : ''}`,
      );
    }
  }

  const translated = sourceKeys.length - missing.length;
  const percent = sourceKeys.length ? Math.round((translated / sourceKeys.length) * 100) : 100;
  summaries.push(`${locale.code}${locale.shipping ? ' (shipping)' : ''}: ${translated}/${sourceKeys.length} — ${percent}%`);
}

if (!config.locales.some((locale) => locale.code === sourceLocale)) {
  errors.push(`sourceLocale "${sourceLocale}" is not listed in locales`);
}
if (!config.locales.some((locale) => locale.shipping)) {
  errors.push('no locale is marked as shipping — at least one is required');
}

// ------------------------------------------- keys referenced by scene content
function findManifests(dir) {
  if (!existsSync(dir)) return [];
  const found = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) found.push(...findManifests(full));
    else if (entry === 'manifest.json') found.push(full);
  }
  return found;
}

const referencedByScenes = new Set();
for (const file of findManifests(SCENES_DIR)) {
  const scene = readJson(file);
  if (scene.titleKey) referencedByScenes.add(scene.titleKey);
}

// Only scene.* keys can be checked for use here; parent-zone keys are referenced
// from C# and would produce noise until the Unity project exists.
for (const key of sourceKeys) {
  if (key.startsWith('scene.') && !referencedByScenes.has(key)) {
    warnings.push(`${sourceLocale}: "${key}" is not referenced by any scene manifest`);
  }
}

// ------------------------------------------------------------------- report
console.log('Localization catalogs\n');
for (const line of summaries) console.log(`  · ${line}`);
console.log('');

for (const message of errors) console.log(`  ✗ ${message}`);
for (const message of warnings) console.log(`  warn ${message}`);

if (errors.length) {
  console.log(`\n${errors.length} error(s) — blocked.`);
  process.exit(1);
}

console.log(`${warnings.length ? '\n' : '  ✓ no problems\n'}Voice-over files are not checked here — they are resolved at runtime as audio/<locale>/<file>.`);
process.exit(0);
