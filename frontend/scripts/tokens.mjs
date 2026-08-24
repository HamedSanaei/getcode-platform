// M01-004 design token bridge.
//
// Usage:
//   node scripts/tokens.mjs generate   # regenerate src/styles/tokens.css from the snapshot
//   node scripts/tokens.mjs check      # validate snapshot schema + drift against committed CSS
//
// The generator is deterministic: same snapshot bytes => same CSS bytes.
// CI runs `check` so an edited Penpot export that was not regenerated fails the build,
// and a hand-edited tokens.css diverging from the snapshot fails too.

import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = dirname(fileURLToPath(import.meta.url));
const frontendDir = resolve(scriptDir, '..');
const repoRoot = resolve(frontendDir, '..');
const snapshotPath = resolve(repoRoot, 'design/tokens/getcode-tokens.v1.1.rev104.json');
const outputPath = resolve(frontendDir, 'src/styles/tokens.css');

const ALLOWED_SETS = ['GetCode/Core', 'GetCode/Brand/GetCode', 'GetCode/Brand/PlusPremium'];
const BRAND_SCOPE = {
  'GetCode/Core': ':root',
  'GetCode/Brand/GetCode': ':root,\n[data-brand="getcode"]',
  'GetCode/Brand/PlusPremium': '[data-brand="pluspremium"]',
};
const ALLOWED_TYPES = new Set([
  'color',
  'spacing',
  'borderRadius',
  'borderWidth',
  'fontFamilies',
  'fontSizes',
  'fontWeights',
  'opacity',
]);
const PIXEL_TYPES = new Set(['spacing', 'borderRadius', 'borderWidth', 'fontSizes']);
const HEX_RE = /^#[0-9A-Fa-f]{6}$/;
const NAME_RE = /^(color|space|radius|border|font|opacity)([.][A-Za-z0-9]+)+$/;

function fail(message) {
  console.error(`token bridge: ${message}`);
  process.exit(1);
}

function validateSnapshot(snapshot) {
  const problems = [];
  const meta = snapshot?.meta;
  if (!meta) problems.push('missing meta block');
  if (!snapshot?.sets || typeof snapshot.sets !== 'object') problems.push('missing sets block');

  if (!problems.length) {
    for (const field of ['penpotFileId', 'namedVersion', 'fileRevision', 'exportedAtUtc']) {
      if (!meta[field]) problems.push(`meta.${field} is required`);
    }
    for (const setName of ALLOWED_SETS) {
      if (!snapshot.sets[setName]) problems.push(`missing token set '${setName}'`);
    }
    for (const key of Object.keys(snapshot.sets)) {
      if (!ALLOWED_SETS.includes(key)) problems.push(`unknown token set '${key}'`);
    }
    // Counts recorded in metadata must match reality (drift guard).
    for (const [setName, count] of Object.entries(meta.tokenCounts ?? {})) {
      const actual = snapshot.sets[setName]?.tokens?.length;
      if (actual !== count) problems.push(`meta.tokenCounts['${setName}']=${count} but found ${actual}`);
    }
  }

  if (!problems.length) {
    for (const [setName, set] of Object.entries(snapshot.sets)) {
      const seen = new Set();
      for (const token of set.tokens) {
        const label = `${setName}/${token.name}`;
        if (!NAME_RE.test(token.name)) problems.push(`${label}: invalid token name`);
        if (seen.has(token.name)) problems.push(`${label}: duplicate token name`);
        seen.add(token.name);
        if (!ALLOWED_TYPES.has(token.type)) problems.push(`${label}: unknown type '${token.type}'`);
        if (token.type === 'color') {
          const value = typeof token.value === 'string' ? token.value : '';
          if (!HEX_RE.test(value)) problems.push(`${label}: color must be #RRGGBB, got '${value}'`);
        }
        if (PIXEL_TYPES.has(token.type)) {
          const n = Number(token.value);
          if (!Number.isFinite(n) || n < 0) problems.push(`${label}: '${token.type}' must be a non-negative number`);
        }
        if (token.type === 'fontWeights' && !/^\d{3}$/.test(String(token.value))) {
          problems.push(`${label}: fontWeights must be a 3-digit weight`);
        }
        if (token.type === 'opacity') {
          const n = Number(token.value);
          if (!(n >= 0 && n <= 1)) problems.push(`${label}: opacity must be between 0 and 1`);
        }
        if (token.type === 'fontFamilies') {
          const list = Array.isArray(token.value) ? token.value : null;
          if (!list || list.length === 0 || list.some((f) => typeof f !== 'string' || f.length === 0)) {
            problems.push(`${label}: fontFamilies must be a non-empty string array`);
          }
        }
      }
    }
  }

  return problems;
}

export function renderCss(snapshot) {
  const lines = [
    '/*',
    ' * GENERATED FILE - do not edit by hand.',
    ` * Source: design/tokens/getcode-tokens.v1.1.rev104.json`,
    ` * Penpot file: ${snapshot.meta.penpotFileId} (rev ${snapshot.meta.fileRevision})`,
    ` * Design system: ${snapshot.meta.designSystemVersion} - ${snapshot.meta.namedVersion}`,
    ` * Exported: ${snapshot.meta.exportedAtUtc}`,
    ' */',
    '',
  ];

  for (const setName of ALLOWED_SETS) {
    const set = snapshot.sets[setName];
    if (!set) continue;
    lines.push(`/* ${setName} */`);
    lines.push(`${BRAND_SCOPE[setName]} {`);
    for (const token of [...set.tokens].sort((a, b) => a.name.localeCompare(b.name))) {
      lines.push(`  ${cssVarName(token.name)}: ${cssValue(token)};`);
    }
    lines.push('}', '');
  }

  return `${lines.join('\n')}\n`;
}

function cssVarName(tokenName) {
  return `--gc-${tokenName.toLowerCase().replaceAll('.', '-')}`;
}

function cssValue(token) {
  if (token.type === 'color') return String(token.value).toUpperCase();
  if (PIXEL_TYPES.has(token.type)) return `${Number(token.value)}px`;
  if (token.type === 'fontWeights' || token.type === 'opacity') return String(Number(token.value));
  if (token.type === 'fontFamilies') {
    return token.value.map((family) => `'${family.replaceAll("'", "\\'")}'`).join(', ');
  }
  return String(token.value);
}

const command = process.argv[2] ?? 'check';
if (command !== 'generate' && command !== 'check') {
  fail(`unknown command '${command}'. Use 'generate' or 'check'.`);
}

let snapshot;
try {
  snapshot = JSON.parse(readFileSync(snapshotPath, 'utf8'));
} catch (error) {
  fail(`cannot read snapshot ${snapshotPath}: ${error.message}`);
}

const problems = validateSnapshot(snapshot);
if (problems.length > 0) {
  for (const problem of problems) console.error(`schema problem: ${problem}`);
  process.exit(1);
}

const css = renderCss(snapshot);

if (command === 'generate') {
  mkdirSync(dirname(outputPath), { recursive: true });
  writeFileSync(outputPath, css, 'utf8');
  console.log(`tokens.css written (${css.length} bytes)`);
} else if (!existsSync(outputPath)) {
  fail(`missing ${outputPath}; run 'npm run tokens:generate'.`);
} else {
  const current = readFileSync(outputPath, 'utf8');
  if (current !== css) {
    fail('tokens.css drifted from the approved snapshot; run npm run tokens:generate and commit.');
  }
  console.log('tokens ok: schema valid, no drift.');
}
