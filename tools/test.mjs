#!/usr/bin/env node
// Tests for the CI gates themselves. A validator that silently passes everything
// is worse than no validator, and you only find that out by testing it.
//
// Usage: node tools/test.mjs

import { spawnSync } from 'node:child_process';
import { dirname, resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');

function run(script, args = []) {
  const result = spawnSync('node', [join(ROOT, 'tools', script), ...args], { encoding: 'utf8', cwd: ROOT });
  return { code: result.status, output: `${result.stdout}${result.stderr}` };
}

const results = [];

function expect(name, passed, detail = '') {
  results.push({ name, passed, detail });
}

function expectMentions(name, output, needles) {
  const missing = needles.filter((needle) => !output.includes(needle));
  expect(name, missing.length === 0, missing.length ? `not reported: ${missing.join(' | ')}` : '');
}

// ------------------------------------------------------------ scene validator
{
  const { code } = run('validate-scenes.mjs');
  expect('valid scenes pass the validator', code === 0, `exit code ${code}`);
}
{
  const { code, output } = run('validate-scenes.mjs', ['tools/fixtures/bad-scene.json']);
  expect('a broken scene is rejected', code === 1, `exit code ${code}`);
  expectMentions('every scene defect class is reported', output, [
    'unknown field',
    'is not declared',
    'has no "cut" config block',
    'is not listed in atlas',
    'expected "grid"',
    'budget exceeded',
  ]);
  expectMentions('localization defects in a scene are reported', output, [
    'is missing from content/i18n/tr.json',
    'expected "voice"',
  ]);
  expect('a dead accept rule is reported as a warning', output.includes('dead rule'));
  expect('an object placed off-canvas is reported', output.includes('outside the 1920x1080 canvas'));
}

// -------------------------------------------------------------- i18n catalogs
{
  const { code } = run('validate-i18n.mjs');
  expect('shipped catalogs pass the validator', code === 0, `exit code ${code}`);
}
{
  const { code, output } = run('validate-i18n.mjs', ['--root', 'tools/fixtures/bad-i18n']);
  expect('broken catalogs are rejected', code === 1, `exit code ${code}`);
  expectMentions('every catalog defect class is reported', output, [
    'missing translation for',
    'stale key',
    'placeholder mismatch',
    'is empty or not a string',
  ]);
  expect('each defect is reported once', output.split('settings.sound').length - 1 === 1);
}

// ------------------------------------------------------------ compliance gate
{
  const { code } = run('check-compliance.mjs');
  expect('a clean repository passes the compliance gate', code === 0, `exit code ${code}`);
}
{
  const { code, output } = run('check-compliance.mjs', ['--root', 'tools/fixtures/bad-project']);
  expect('a non-compliant project is rejected', code === 1, `exit code ${code}`);
  expectMentions('every violation class is reported', output, [
    'com.unity.purchasing',
    'com.unity.services.analytics',
    'submitAnalytics',
    'deviceUniqueIdentifier',
  ]);
  expect('RevenueCat is not flagged by mistake', !output.includes('com.revenuecat'));
}

// ------------------------------------------------------------------- report
let failedCount = 0;
for (const result of results) {
  if (result.passed) {
    console.log(`  ✓ ${result.name}`);
  } else {
    failedCount++;
    console.log(`  ✗ ${result.name}${result.detail ? `\n      ${result.detail}` : ''}`);
  }
}
console.log(`\n${results.length} test(s) · ${failedCount} failing`);
process.exit(failedCount ? 1 : 0);
