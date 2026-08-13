#!/usr/bin/env node
// Runs the Unity EditMode tests headlessly and reports the result readably.
//
// Kept out of `npm test` on purpose: it needs a licensed Unity install and takes
// minutes, while the gate and core suites take seconds. Run it before pushing
// anything that touches unity/.
//
// Usage: node tools/run-unity-tests.mjs [--platform EditMode|PlayMode]

import { spawnSync } from 'node:child_process';
import { readFileSync, existsSync, mkdtempSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const EDITOR_VERSION = readFileSync(join(ROOT, 'unity/ProjectSettings/ProjectVersion.txt'), 'utf8')
  .match(/m_EditorVersion:\s*(\S+)/)?.[1];

const EDITOR = `/Applications/Unity/Hub/Editor/${EDITOR_VERSION}/Unity.app/Contents/MacOS/Unity`;

const platformFlag = process.argv.indexOf('--platform');
const platform = platformFlag === -1 ? 'EditMode' : process.argv[platformFlag + 1];

if (!existsSync(EDITOR)) {
  console.error(`Unity ${EDITOR_VERSION} not found at ${EDITOR}`);
  console.error('Install it through Unity Hub — see unity/README.md.');
  process.exit(1);
}

const workDir = mkdtempSync(join(tmpdir(), 'ustaca-unity-'));
const resultsPath = join(workDir, 'results.xml');
const logPath = join(workDir, 'unity.log');

console.log(`Running ${platform} tests in Unity ${EDITOR_VERSION}…`);

const run = spawnSync(EDITOR, [
  '-batchmode', '-nographics',
  '-projectPath', join(ROOT, 'unity'),
  '-runTests', '-testPlatform', platform,
  '-testResults', resultsPath,
  '-logFile', logPath,
], { encoding: 'utf8' });

const log = existsSync(logPath) ? readFileSync(logPath, 'utf8') : '';
const compileErrors = log.split('\n').filter((line) => /error CS\d+/.test(line));

if (compileErrors.length) {
  console.log(`\n${compileErrors.length} compile error(s):\n`);
  for (const line of compileErrors.slice(0, 15)) console.log(`  ${line.trim()}`);
  console.log(`\nFull log: ${logPath}`);
  process.exit(1);
}

if (!existsSync(resultsPath)) {
  console.log(`\nUnity produced no test results (exit ${run.status}). Full log: ${logPath}`);
  process.exit(1);
}

const results = readFileSync(resultsPath, 'utf8');
const attribute = (name) => results.match(new RegExp(`\\b${name}="(\\d+)"`))?.[1] ?? '0';
const total = attribute('total');
const passed = attribute('passed');
const failed = attribute('failed');

for (const match of results.matchAll(/<test-case[^>]*\bname="([^"]+)"[^>]*\bresult="(?!Passed)([^"]+)"/g)) {
  console.log(`  ✗ ${match[1]} — ${match[2]}`);
}

console.log(`\n${total} test(s) · ${passed} passed · ${failed} failing`);
if (failed !== '0') console.log(`Full log: ${logPath}`);

process.exit(failed === '0' ? 0 : 1);
