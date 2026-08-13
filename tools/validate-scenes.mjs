#!/usr/bin/env node
// Validates scene manifests: structure (against the schema), cross-references,
// localization keys, and the reference-device performance budget.
//
// Usage: node tools/validate-scenes.mjs [path/to/manifest.json ...]
// With no arguments, every manifest.json under content/scenes is checked.

import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const SCHEMA = readJson(join(ROOT, 'content/schema/scene.schema.json'));
const LOCALES = readJson(join(ROOT, 'content/i18n/locales.json'));
const SOURCE_STRINGS = readJson(join(ROOT, `content/i18n/${LOCALES.sourceLocale}.json`));

function readJson(path) {
  return JSON.parse(readFileSync(path, 'utf8'));
}

// ------------------------------------------------------------- schema engine
// The subset of JSON Schema this project uses: const, type, enum, required,
// properties, additionalProperties:false, items, pattern, minimum, maximum,
// minItems, minLength. Deliberately small — the schema is authored here, so an
// unsupported keyword is a bug in the schema, not a gap to work around.

function validateSchema(value, schema, path, errors) {
  if ('const' in schema && value !== schema.const) {
    errors.push(`${path}: expected ${JSON.stringify(schema.const)}, found ${JSON.stringify(value)}`);
    return;
  }
  if (schema.enum && !schema.enum.includes(value)) {
    errors.push(`${path}: invalid value ${JSON.stringify(value)} — allowed: ${schema.enum.join(', ')}`);
    return;
  }
  if (schema.type && !typeMatches(value, schema.type)) {
    errors.push(`${path}: expected type ${schema.type}, found ${describeType(value)}`);
    return;
  }

  if (schema.type === 'string') {
    if (schema.minLength !== undefined && value.length < schema.minLength) {
      errors.push(`${path}: must be at least ${schema.minLength} characters`);
    }
    if (schema.pattern && !new RegExp(schema.pattern).test(value)) {
      errors.push(`${path}: "${value}" does not match ${schema.pattern}`);
    }
  }

  if (schema.type === 'number' || schema.type === 'integer') {
    if (schema.type === 'integer' && !Number.isInteger(value)) {
      errors.push(`${path}: must be an integer`);
    }
    if (schema.minimum !== undefined && value < schema.minimum) {
      errors.push(`${path}: must be at least ${schema.minimum} (got ${value})`);
    }
    if (schema.maximum !== undefined && value > schema.maximum) {
      errors.push(`${path}: must be at most ${schema.maximum} (got ${value})`);
    }
  }

  if (schema.type === 'array') {
    if (schema.minItems !== undefined && value.length < schema.minItems) {
      errors.push(`${path}: must contain at least ${schema.minItems} item(s)`);
    }
    if (schema.items) {
      value.forEach((item, index) => validateSchema(item, schema.items, `${path}[${index}]`, errors));
    }
  }

  if (schema.type === 'object') {
    for (const key of schema.required ?? []) {
      if (!(key in value)) errors.push(`${path}: missing required field "${key}"`);
    }
    if (schema.additionalProperties === false) {
      for (const key of Object.keys(value)) {
        if (!(key in (schema.properties ?? {}))) errors.push(`${path}: unknown field "${key}"`);
      }
    }
    for (const [key, subSchema] of Object.entries(schema.properties ?? {})) {
      if (key in value) validateSchema(value[key], subSchema, `${path}.${key}`, errors);
    }
  }
}

function typeMatches(value, type) {
  if (type === 'integer' || type === 'number') return typeof value === 'number' && Number.isFinite(value);
  if (type === 'array') return Array.isArray(value);
  if (type === 'object') return value !== null && typeof value === 'object' && !Array.isArray(value);
  return typeof value === type;
}

function describeType(value) {
  if (Array.isArray(value)) return 'array';
  if (value === null) return 'null';
  return typeof value;
}

// -------------------------------------------------------- cross-references
// Everything the schema cannot see: whether an id actually exists, whether a
// declared mechanic is configured, whether the budget is blown.

const MECHANICS = ['glue', 'paint', 'cut', 'build'];

function matches(pattern, id) {
  return pattern.endsWith('*') ? id.startsWith(pattern.slice(0, -1)) : id === pattern;
}

function validateReferences(scene, errors, warnings) {
  const layerIds = new Set((scene.layers ?? []).map((layer) => layer.id));
  const objectIds = (scene.objects ?? []).map((object) => object.id);
  const zonesById = new Map((scene.zones ?? []).map((zone) => [zone.id, zone]));
  const atlasesById = new Map((scene.assets?.atlases ?? []).map((atlas) => [atlas.id, atlas]));
  const skeletonIds = new Set((scene.assets?.skeletons ?? []).map((skeleton) => skeleton.id));
  const audioById = new Map((scene.assets?.audio ?? []).map((clip) => [clip.id, clip]));

  reportDuplicateIds(scene.layers, 'layers', errors);
  reportDuplicateIds(scene.objects, 'objects', errors);
  reportDuplicateIds(scene.zones, 'zones', errors);
  reportDuplicateIds(scene.characters, 'characters', errors);
  reportDuplicateIds(scene.assets?.atlases, 'assets.atlases', errors);
  reportDuplicateIds(scene.assets?.skeletons, 'assets.skeletons', errors);
  reportDuplicateIds(scene.assets?.audio, 'assets.audio', errors);

  // Audio kind is load-bearing: 'voice' is localized and shipped per locale,
  // 'sfx' and 'ambience' are shared. Mixing them up silently ships a Turkish
  // clip to English users, so it is an error rather than a warning.
  const requireAudio = (where, id, expectedType) => {
    if (id === undefined) return;
    const clip = audioById.get(id);
    if (!clip) {
      errors.push(`${where}: "${id}" is not declared in assets.audio`);
    } else if (clip.type !== expectedType) {
      errors.push(`${where}: "${id}" is type "${clip.type}", expected "${expectedType}"`);
    }
  };

  if (!(scene.titleKey in SOURCE_STRINGS)) {
    errors.push(`titleKey: "${scene.titleKey}" is missing from content/i18n/${LOCALES.sourceLocale}.json`);
  }
  if (scene.titleKey !== `scene.${scene.id}.title`) {
    errors.push(`titleKey: expected "scene.${scene.id}.title" to match scene id "${scene.id}"`);
  }

  for (const object of scene.objects ?? []) {
    const where = `objects."${object.id}"`;

    if (!layerIds.has(object.layer)) {
      errors.push(`${where}: layer "${object.layer}" is not declared`);
    }

    const [atlasId, spriteName] = object.sprite.split(':');
    const atlas = atlasesById.get(atlasId);
    if (!atlas) {
      errors.push(`${where}: atlas "${atlasId}" is not declared in assets.atlases`);
    } else if (atlas.sprites && !atlas.sprites.includes(spriteName)) {
      errors.push(`${where}: sprite "${spriteName}" is not listed in atlas "${atlasId}"`);
    }

    requireAudio(`${where}.labelVoice`, object.labelVoice, 'voice');

    const declared = new Set(object.mechanics ?? []);
    for (const mechanic of MECHANICS) {
      if (declared.has(mechanic) && !(mechanic in object)) {
        errors.push(`${where}: mechanic "${mechanic}" is declared but has no "${mechanic}" config block`);
      }
      if (!declared.has(mechanic) && mechanic in object) {
        errors.push(`${where}: has a "${mechanic}" block but does not declare it in mechanics`);
      }
    }

    if (object.glue) {
      for (const pattern of object.glue.acceptedBy) {
        const hits = [...zonesById.values()].filter((zone) => matches(pattern, zone.id));
        if (hits.length === 0) {
          errors.push(`${where}.glue.acceptedBy: "${pattern}" matches no zone`);
        } else if (!hits.some((zone) => zone.type === 'snap')) {
          errors.push(`${where}.glue.acceptedBy: "${pattern}" matches no zone of type snap`);
        }
      }
      requireAudio(`${where}.glue.snapSfx`, object.glue.snapSfx, 'sfx');
    }

    if (object.cut) requireAudio(`${where}.cut.cutSfx`, object.cut.cutSfx, 'sfx');

    if (object.paint) {
      requireAudio(`${where}.paint.brushSfx`, object.paint.brushSfx, 'sfx');
      requireAudio(`${where}.paint.fillSfx`, object.paint.fillSfx, 'sfx');
    }

    if (object.build) {
      const zone = zonesById.get(object.build.gridZone);
      if (!zone) {
        errors.push(`${where}.build.gridZone: "${object.build.gridZone}" is not declared`);
      } else if (zone.type !== 'grid') {
        errors.push(`${where}.build.gridZone: "${zone.id}" is type "${zone.type}", expected "grid"`);
      } else if (!zone.grid) {
        errors.push(`${where}.build.gridZone: "${zone.id}" has no grid (columns/rows)`);
      }
      requireAudio(`${where}.build.settleSfx`, object.build.settleSfx, 'sfx');
    }
  }

  for (const zone of scene.zones ?? []) {
    for (const pattern of zone.accepts ?? []) {
      if (!objectIds.some((id) => matches(pattern, id))) {
        warnings.push(`zones."${zone.id}".accepts: "${pattern}" matches no object — dead rule`);
      }
    }
    if (zone.type === 'grid' && !zone.grid) {
      errors.push(`zones."${zone.id}": type is grid but columns/rows are missing`);
    }
  }

  for (const character of scene.characters ?? []) {
    const where = `characters."${character.id}"`;
    if (!skeletonIds.has(character.skeleton)) {
      errors.push(`${where}: skeleton "${character.skeleton}" is not declared in assets.skeletons`);
    }
    for (const [index, reaction] of (character.reactions ?? []).entries()) {
      if (reaction.target && !objectIds.some((id) => matches(reaction.target, id))) {
        warnings.push(`${where}.reactions[${index}].target: "${reaction.target}" matches no object`);
      }
      requireAudio(`${where}.reactions[${index}].voice`, reaction.voice, 'voice');
    }
  }

  requireAudio('audio.ambience', scene.audio?.ambience, 'ambience');

  const usedAudioIds = collectAudioReferences(scene);
  for (const id of audioById.keys()) {
    if (!usedAudioIds.has(id)) {
      warnings.push(`assets.audio."${id}": declared but never referenced`);
    }
  }

  // Anything placed outside the design resolution is invisible on every device.
  // A warning rather than an error: authors legitimately park props just off-screen
  // while blocking out a scene.
  const { width, height } = scene.canvas;
  for (const object of scene.objects ?? []) {
    const { x, y } = object.transform;
    if (x < 0 || x > width || y < 0 || y > height) {
      warnings.push(`objects."${object.id}": (${x}, ${y}) is outside the ${width}x${height} canvas`);
    }
  }
  for (const zone of scene.zones ?? []) {
    const { x, y, width: w, height: h } = zone.shape;
    if (x < 0 || y < 0 || x + w > width || y + h > height) {
      warnings.push(`zones."${zone.id}": extends past the ${width}x${height} canvas`);
    }
  }

  const spriteCount = (scene.objects ?? []).length;
  if (spriteCount > scene.budget.maxActiveSprites) {
    errors.push(`budget exceeded: ${spriteCount} objects, maxActiveSprites is ${scene.budget.maxActiveSprites}`);
  }
  const skeletonCount = (scene.characters ?? []).length;
  if (skeletonCount > scene.budget.maxSkeletons) {
    errors.push(`budget exceeded: ${skeletonCount} characters, maxSkeletons is ${scene.budget.maxSkeletons}`);
  }
}

function collectAudioReferences(scene) {
  const referenced = new Set();
  JSON.stringify(scene, (key, value) => {
    if (key.endsWith('Sfx') || key === 'ambience' || key === 'labelVoice' || key === 'voice') {
      referenced.add(value);
    }
    return value;
  });
  return referenced;
}

function reportDuplicateIds(items, where, errors) {
  const seen = new Set();
  for (const item of items ?? []) {
    if (seen.has(item.id)) errors.push(`${where}: id "${item.id}" is used more than once`);
    seen.add(item.id);
  }
}

// --------------------------------------------------------------------- run

function findManifests(dir) {
  const found = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) found.push(...findManifests(full));
    else if (entry === 'manifest.json') found.push(full);
  }
  return found;
}

const targets = process.argv.slice(2).length
  ? process.argv.slice(2)
  : findManifests(join(ROOT, 'content/scenes'));

let failedCount = 0;
let warningCount = 0;

for (const file of targets) {
  const relativePath = file.replace(`${ROOT}/`, '');
  const errors = [];
  const warnings = [];

  let scene;
  try {
    scene = readJson(file);
  } catch (error) {
    console.log(`✗ ${relativePath}\n    could not parse JSON: ${error.message}`);
    failedCount++;
    continue;
  }

  validateSchema(scene, SCHEMA, 'scene', errors);
  // Cross-reference checks run even when the schema failed, so a content author
  // sees every problem in one pass instead of fixing one typo at a time. Missing
  // or malformed fields can throw here; the schema errors are already reported.
  try {
    validateReferences(scene, errors, warnings);
  } catch (error) {
    if (errors.length === 0) throw error;
    errors.push(`cross-reference check aborted (fix the schema errors above first): ${error.message}`);
  }

  warningCount += warnings.length;
  if (errors.length) {
    failedCount++;
    console.log(`✗ ${relativePath}`);
    for (const message of errors) console.log(`    ERROR ${message}`);
  } else {
    console.log(`✓ ${relativePath}  (${(scene.objects ?? []).length} objects, ${(scene.characters ?? []).length} characters)`);
  }
  for (const message of warnings) console.log(`    warn  ${message}`);
}

const summary = `${targets.length} scene(s) · ${failedCount} failing · ${warningCount} warning(s)`;
console.log(failedCount ? `\n${summary}` : `\n${summary} — all passed`);
process.exit(failedCount ? 1 : 0);
