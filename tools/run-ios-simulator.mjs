#!/usr/bin/env node
// Builds the game and runs it on a booted iOS simulator.
//
// Three steps, each of which had a non-obvious failure the first time:
//   1. Unity  — must target the Simulator SDK *and* arm64, or the generated Xcode
//               project has no runnable destination on an Apple silicon Mac.
//   2. Xcode  — built with -target rather than -scheme, because Unity's generated
//               scheme resolves no destinations. Needs the iOS platform installed
//               (xcodebuild -downloadPlatform iOS) or the launch storyboards fail.
//   3. simctl — install and launch on whichever simulator is already booted.
//
// The Xcode step compiles IL2CPP's C++ output and takes minutes. It prints a
// heartbeat while it works: a silent command that runs for five minutes is
// indistinguishable from a hung one.
//
// Usage: node tools/run-ios-simulator.mjs [--skip-unity] [--device <udid>]

import { spawn, spawnSync } from 'node:child_process';
import { readFileSync, existsSync, rmSync, mkdirSync, readdirSync, writeFileSync } from 'node:fs';
import { join, dirname, resolve } from 'node:path';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const XCODE_PROJECT = join(ROOT, 'unity/Builds/ios-simulator');
const APP_OUTPUT = join(ROOT, 'unity/Builds/ios-app');
const BUNDLE_ID = 'app.ustacaeller';

const EDITOR_VERSION = readFileSync(join(ROOT, 'unity/ProjectSettings/ProjectVersion.txt'), 'utf8')
  .match(/m_EditorVersion:\s*(\S+)/)?.[1];
const EDITOR = `/Applications/Unity/Hub/Editor/${EDITOR_VERSION}/Unity.app/Contents/MacOS/Unity`;

const skipUnity = process.argv.includes('--skip-unity');
const deviceFlag = process.argv.indexOf('--device');

function seconds(start) {
  return `${Math.round((Date.now() - start) / 1000)}s`;
}

function run(command, args, label) {
  return new Promise((resolvePromise) => {
    const start = Date.now();
    process.stdout.write(`▸ ${label}\n`);

    const child = spawn(command, args, { stdio: ['ignore', 'pipe', 'pipe'] });
    let output = '';
    child.stdout.on('data', (chunk) => { output += chunk; });
    child.stderr.on('data', (chunk) => { output += chunk; });

    // Without this the terminal shows nothing at all for minutes at a time.
    const heartbeat = setInterval(() => {
      process.stdout.write(`  … ${seconds(start)}\n`);
    }, 15000);

    child.on('close', (code) => {
      clearInterval(heartbeat);

      if (code !== 0) {
        const errors = output.split('\n').filter((line) => / error:|error CS/.test(line));
        for (const line of (errors.length ? errors : output.split('\n')).slice(0, 12)) {
          console.log(`  ${line.trim()}`);
        }

        const logPath = join(tmpdir(), `ustaca-${label.replace(/\W+/g, '-')}.log`);
        writeFileSync(logPath, output);
        console.log(`\n${label} failed after ${seconds(start)} (exit ${code}). Full log: ${logPath}`);
        process.exit(1);
      }

      console.log(`  done in ${seconds(start)}`);
      resolvePromise(output);
    });
  });
}

function bootedDevice() {
  if (deviceFlag !== -1) return process.argv[deviceFlag + 1];

  const listed = spawnSync('xcrun', ['simctl', 'list', 'devices'], { encoding: 'utf8' }).stdout ?? '';
  const booted = listed.split('\n').find((line) => line.includes('(Booted)'));
  const udid = booted?.match(/\(([0-9A-F-]{36})\)/)?.[1];

  if (!udid) {
    console.error('No booted simulator. Start one first:');
    console.error('  xcrun simctl boot "iPad Pro 11-inch (M5)"');
    process.exit(1);
  }

  return udid;
}

if (!existsSync(EDITOR)) {
  console.error(`Unity ${EDITOR_VERSION} not found — see unity/README.md.`);
  process.exit(1);
}

const device = bootedDevice();
console.log(`Simulator: ${device}\n`);

if (!skipUnity) {
  await run(EDITOR, [
    '-batchmode', '-nographics',
    '-projectPath', join(ROOT, 'unity'),
    '-buildTarget', 'iOS',
    '-executeMethod', 'UstacaEller.Editor.BuildTools.BuildIosSimulator',
    '-logFile', '-',
  ], 'Unity → Xcode project');
}

rmSync(APP_OUTPUT, { recursive: true, force: true });
mkdirSync(APP_OUTPUT, { recursive: true });

await run('xcodebuild', [
  'build',
  '-project', join(XCODE_PROJECT, 'Unity-iPhone.xcodeproj'),
  '-target', 'Unity-iPhone',
  '-configuration', 'Debug',
  '-sdk', 'iphonesimulator',
  '-arch', 'arm64',
  `CONFIGURATION_BUILD_DIR=${APP_OUTPUT}`,
  'CODE_SIGNING_ALLOWED=NO',
  'CODE_SIGNING_REQUIRED=NO',
], 'Xcode → .app (compiles IL2CPP output, takes a few minutes)');

const app = readdirSync(APP_OUTPUT).find((entry) => entry.endsWith('.app'));
if (!app) {
  console.error(`No .app produced in ${APP_OUTPUT}`);
  process.exit(1);
}

await run('xcrun', ['simctl', 'install', device, join(APP_OUTPUT, app)], `Install ${app}`);
await run('xcrun', ['simctl', 'launch', device, BUNDLE_ID], 'Launch');

spawnSync('open', ['-a', 'Simulator']);

// The app is landscape-only, so on an upright simulated device iOS rotates the
// content and the window looks sideways — which reads as a broken build. simctl
// cannot rotate a device and scripting the keystroke needs Accessibility access,
// so this is the one step that stays manual.
console.log('\nRunning. The app is landscape only — if the window is sideways, press Cmd+← in the Simulator.');
