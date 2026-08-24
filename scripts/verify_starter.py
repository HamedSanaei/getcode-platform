#!/usr/bin/env python3
from __future__ import annotations
from pathlib import Path
import json, re, sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]
errors=[]

# Generated / vendor / build-artifact directories are never source; scanning
# them is slow and produces false positives (vendor JSON, minified bundles).
EXCLUDED_DIRS={
    '.git','.github/cache','.next','.nuxt','.output','.svelte-kit','.turbo','.vercel',
    '.venv','venv','__pycache__','.pytest_cache','.mypy_cache',
    'bin','obj','artifacts','TestResults','publish',
    'node_modules','coverage','.nyc_output','dist','build','out','storybook-static',
}

def is_source(path: Path) -> bool:
    return not any(part in EXCLUDED_DIRS for part in path.relative_to(ROOT).parts)

def rglob_source(*patterns: str) -> list[Path]:
    found: list[Path] = []
    seen: set[Path] = set()
    for pattern in patterns:
        for p in ROOT.rglob(pattern):
            if p.is_file() and p not in seen and is_source(p):
                seen.add(p)
                found.append(p)
    return found

def err(msg): errors.append(msg)

# JSON validity
for p in rglob_source('*.json'):
    try: json.loads(p.read_text(encoding='utf-8'))
    except Exception as e: err(f'JSON {p.relative_to(ROOT)}: {e}')

# XML project/props validity
for p in rglob_source('*.csproj','*.props','*.targets'):
    try: ET.parse(p)
    except Exception as e: err(f'XML {p.relative_to(ROOT)}: {e}')

# Roadmap task paths
plan_path=ROOT/'docs/roadmap/github-plan.json'
if plan_path.exists():
    plan=json.loads(plan_path.read_text())
    ids=set()
    for task in plan['tasks']:
        if task['id'] in ids: err(f"duplicate task id {task['id']}")
        ids.add(task['id'])
        p=ROOT/task['bodyPath']
        if not p.exists(): err(f"missing task file {task['bodyPath']}")
    mids={m['id'] for m in plan['milestones']}
    graph={task['id']: task['dependsOn'] for task in plan['tasks']}
    for task in plan['tasks']:
        if task['milestone'] not in mids: err(f"unknown milestone for {task['id']}")
        for dep in task['dependsOn']:
            if dep not in ids: err(f"unknown dependency {dep} for {task['id']}")

    visiting=set()
    visited=set()
    def visit(node):
        if node in visiting:
            err(f"task dependency cycle at {node}")
            return
        if node in visited:
            return
        visiting.add(node)
        for dep in graph.get(node, []):
            visit(dep)
        visiting.remove(node)
        visited.add(node)
    for node in graph:
        visit(node)

# Architecture project references
for p in ROOT.glob('backend/src/*/*.csproj'):
    txt=p.read_text()
    name=p.stem
    refs=re.findall(r'ProjectReference Include="([^"]+)"',txt)
    if name=='GetCode.Domain' and refs: err('Domain must have no ProjectReference')
    if name=='GetCode.Application' and any(x in '\n'.join(refs) for x in ['Persistence','Infrastructure','Api','Worker']): err('Application references outer layer')

# Guard accidental hard-coded external primary host in SOURCE files.
# Documentation may reference the audited site historically (audit handoffs).
for p in rglob_source('*.cs', '*.ts', '*.tsx', '*.js', '*.mjs', '*.css'):
    txt=p.read_text(errors='ignore')
    if 'numberland.ir' in txt.lower(): err(f'competitor domain hardcoded in {p.relative_to(ROOT)}')

if errors:
    print('Starter verification FAILED:')
    for e in errors: print(' -',e)
    sys.exit(1)
print('Starter verification OK')
source_count=sum(1 for p in ROOT.rglob('*') if p.is_file() and is_source(p))
total_count=sum(1 for p in ROOT.rglob('*') if p.is_file())
print(f"files={source_count} (skipped {total_count - source_count} generated/vendor files)")
if plan_path.exists():
    print(f"milestones={len(plan['milestones'])} tasks={len(plan['tasks'])}")
