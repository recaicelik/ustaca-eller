#!/usr/bin/env node
// Kids Category / COPPA compliance gate.
//
// Why this exists: Apple's Kids Category bans third-party analytics outright. In
// Unity that becomes a concrete trap — Unity Analytics collects device and
// advertising identifiers, and the Unity IAP package cannot be enabled without
// Analytics by default. So payments go through RevenueCat and com.unity.services.*
// never enters the project. This gate makes that mechanical instead of a habit.
//
// Runs on every pull request. No merge without green.
//
// Usage: node tools/check-compliance.mjs [--root <dir>]
//        --root is for tests only; defaults to the repository root.

import { readFileSync, readdirSync, statSync, existsSync } from 'node:fs';
import { join, dirname, resolve, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const REPO = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const rootFlagIndex = process.argv.indexOf('--root');
const ROOT = rootFlagIndex === -1 ? REPO : resolve(process.argv[rootFlagIndex + 1]);
const UNITY_DIR = join(ROOT, 'unity');

// Package name prefix -> why it is banned
const BANNED_PACKAGES = [
  ['com.unity.services.', 'Unity Gaming Services — collects analytics and device identifiers'],
  ['com.unity.purchasing', 'Unity IAP — cannot be enabled without Analytics; use RevenueCat'],
  ['com.unity.ads', 'Ad SDK — banned in the Kids Category'],
  ['com.unity.analytics', 'Unity Analytics — a documented Kids Category rejection cause'],
  ['com.google.firebase', 'Firebase — third-party analytics'],
  ['com.google.play.', 'Play services — device identifier risk'],
  ['com.appsflyer', 'Attribution SDK — third party'],
  ['com.adjust', 'Attribution SDK — third party'],
  ['io.branch', 'Attribution SDK — third party'],
  ['com.facebook', 'Meta SDK — third-party tracking'],
  ['com.amplitude', 'Third-party analytics'],
  ['com.mixpanel', 'Third-party analytics'],
];

// Symbol that must not appear in source -> why
const BANNED_SYMBOLS = [
  ['deviceUniqueIdentifier', 'Device identifier — counts as children\'s personal data'],
  ['advertisingIdentifier', 'IDFA/AAID — banned in the Kids Category'],
  ['ASIdentifierManager', 'IDFA access — banned in the Kids Category'],
  ['RequestAdvertisingIdentifierAsync', 'IDFA access — banned in the Kids Category'],
  ['AppTrackingTransparency', 'ATT — should never be requested in a kids app'],
  ['ATTrackingManager', 'ATT — should never be requested in a kids app'],
  ['UnityEngine.Analytics', 'Unity Analytics — a documented Kids Category rejection cause'],
  ['Unity.Services.', 'Unity Gaming Services — must not enter the project'],
  ['Microphone.', 'Microphone — biometric data risk, out of scope for v1'],
  ['WebCamTexture', 'Camera — biometric data risk, out of scope for v1'],
];

const violations = [];
const notes = [];

// -------------------------------------------------------------- package list
const packageManifestPath = join(UNITY_DIR, 'Packages/manifest.json');
if (existsSync(packageManifestPath)) {
  let dependencies = {};
  try {
    dependencies = JSON.parse(readFileSync(packageManifestPath, 'utf8')).dependencies ?? {};
  } catch (error) {
    violations.push(`unity/Packages/manifest.json could not be parsed: ${error.message}`);
  }
  for (const name of Object.keys(dependencies)) {
    for (const [prefix, reason] of BANNED_PACKAGES) {
      if (name.startsWith(prefix)) {
        violations.push(`banned package: ${name}\n            reason: ${reason}`);
      }
    }
  }
  notes.push(`${Object.keys(dependencies).length} package(s) scanned`);
} else {
  notes.push('unity/Packages/manifest.json not found — Unity project not set up yet, package check skipped');
}

// ----------------------------------------------------------- project settings
const projectSettingsPath = join(UNITY_DIR, 'ProjectSettings/ProjectSettings.asset');
if (existsSync(projectSettingsPath)) {
  const settings = readFileSync(projectSettingsPath, 'utf8');
  if (/^\s*submitAnalytics:\s*1\s*$/m.test(settings)) {
    violations.push('ProjectSettings: submitAnalytics is on — hardware stats are being sent, must be 0');
  }
  notes.push('ProjectSettings scanned');
} else {
  notes.push('unity/ProjectSettings not found — settings check skipped');
}

// ---------------------------------------------------------------- source scan
// fixtures intentionally contain violations; the tests point --root at them.
// Builds holds generated Xcode/Gradle output — not ours to police line by line;
// what matters there is what actually compiles, checked separately below.
const SKIP_DIRS = new Set(['node_modules', '.git', 'Library', 'Temp', 'Logs', 'obj', 'Build', 'Builds', 'docs', 'fixtures']);
const SCANNED_EXTENSIONS = ['.cs', '.json', '.mm', '.m', '.swift', '.java', '.kt', '.gradle', '.plist'];

function collectSourceFiles(dir, found = []) {
  for (const entry of readdirSync(dir)) {
    if (SKIP_DIRS.has(entry)) continue;
    const full = join(dir, entry);
    if (statSync(full).isDirectory()) collectSourceFiles(full, found);
    else if (SCANNED_EXTENSIONS.some((extension) => entry.endsWith(extension))) found.push(full);
  }
  return found;
}

const selfPath = fileURLToPath(import.meta.url);
let scannedFileCount = 0;

for (const file of collectSourceFiles(ROOT)) {
  if (file === selfPath) continue; // this file lists the banned symbols
  scannedFileCount++;
  readFileSync(file, 'utf8').split('\n').forEach((line, index) => {
    if (line.trimStart().startsWith('//')) return;
    for (const [symbol, reason] of BANNED_SYMBOLS) {
      if (line.includes(symbol)) {
        violations.push(`banned call: ${symbol}\n            ${relative(ROOT, file)}:${index + 1}\n            reason: ${reason}`);
      }
    }
  });
}
notes.push(`${scannedFileCount} source file(s) scanned`);

// ------------------------------------------------------- generated iOS project
// Unity's iOS trampoline ships IDFA and App Tracking Transparency code in
// Classes/Unity/DeviceSettings.mm. Reading that as a violation would be wrong: it
// sits behind #if UNITY_USES_IAD and does not compile unless something turns it on.
// What Apple actually sees is whether it compiled and whether AdSupport is linked,
// so that is what gets checked — and it flips the moment an ad SDK is added.
const buildsRoot = join(ROOT, 'unity/Builds');
if (existsSync(buildsRoot)) {
  for (const entry of readdirSync(buildsRoot)) {
    const preprocessor = join(buildsRoot, entry, 'Classes/Preprocessor.h');
    const projectFile = join(buildsRoot, entry, 'Unity-iPhone.xcodeproj/project.pbxproj');
    if (!existsSync(preprocessor)) continue;

    if (!/#define\s+UNITY_USES_IAD\s+0/.test(readFileSync(preprocessor, 'utf8'))) {
      violations.push(
        `iOS build "${entry}": UNITY_USES_IAD is not 0\n` +
        '            reason: compiles Unity\'s IDFA and ATT code into the binary',
      );
    }

    if (existsSync(projectFile) && readFileSync(projectFile, 'utf8').includes('AdSupport')) {
      violations.push(
        `iOS build "${entry}": AdSupport.framework is linked\n` +
        '            reason: advertising identifier access, banned in the Kids Category',
      );
    }

    notes.push(`iOS build "${entry}" checked: no IDFA/ATT code compiled in`);
  }
}

// --------------------------------------------------------------------- report
console.log('Kids Category / COPPA compliance gate\n');
for (const note of notes) console.log(`  · ${note}`);
console.log('');

if (violations.length) {
  for (const violation of violations) console.log(`  ✗ ${violation}`);
  console.log(`\n${violations.length} violation(s) — merge blocked.`);
  process.exit(1);
}

console.log('  ✓ no violations');
console.log('\nNote: this gate only catches what a machine can see. Network traffic capture');
console.log('and the privacy disclosure audit are manual steps in Phase 5.');
process.exit(0);
