#!/usr/bin/env python3
"""Idempotently create GetCode labels, milestones and roadmap issues using authenticated GitHub CLI."""
from __future__ import annotations
import argparse, json, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PLAN = ROOT / "docs" / "roadmap" / "github-plan.json"


def gh(*args: str, capture: bool = True) -> str:
    result = subprocess.run(["gh", *args], cwd=ROOT, text=True, capture_output=capture, check=False)
    if result.returncode != 0:
        if capture:
            print(result.stderr, file=sys.stderr)
        raise SystemExit(result.returncode)
    return result.stdout.strip() if capture else ""


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", default=None)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    plan = json.loads(PLAN.read_text(encoding="utf-8"))
    repo = args.repo or plan["repository"]

    labels = {
        "type:task": "1D76DB",
        "type:bug": "D73A4A",
        "agent-ready": "0E8A16",
        "priority:P0": "B60205",
        "priority:P1": "FBCA04",
        "priority:P2": "C5DEF5",
    }
    for milestone in plan["milestones"]:
        labels[f"milestone:{milestone['id']}"] = "5319E7"

    if args.dry_run:
        print(f"Would bootstrap {len(plan['milestones'])} milestones and {len(plan['tasks'])} tasks in {repo}")
        for task in plan["tasks"]:
            print(task["id"], task["title"])
        return

    gh("auth", "status", capture=False)

    # gh label create --force is idempotent.
    for name, color in labels.items():
        gh("label", "create", name, "--repo", repo, "--color", color, "--force", capture=False)

    existing_milestones = json.loads(gh("api", f"repos/{repo}/milestones?state=all&per_page=100"))
    milestone_numbers = {item["title"]: item["number"] for item in existing_milestones}
    for item in plan["milestones"]:
        title = f"{item['id']} — {item['title']}"
        if title not in milestone_numbers:
            created = json.loads(gh("api", f"repos/{repo}/milestones", "-X", "POST", "-f", f"title={title}", "-f", f"description={item['description']}"))
            milestone_numbers[title] = created["number"]

    existing_issues = json.loads(gh("issue", "list", "--repo", repo, "--state", "all", "--limit", "1000", "--json", "title"))
    existing_titles = {item["title"] for item in existing_issues}

    for task in plan["tasks"]:
        issue_title = f"{task['id']}: {task['title']}"
        if issue_title in existing_titles:
            continue
        milestone_title = next(f"{m['id']} — {m['title']}" for m in plan["milestones"] if m["id"] == task["milestone"])
        body = (ROOT / task["bodyPath"]).read_text(encoding="utf-8")
        gh(
            "issue", "create", "--repo", repo,
            "--title", issue_title,
            "--body", body,
            "--milestone", milestone_title,
            "--label", "type:task",
            "--label", "agent-ready",
            "--label", f"priority:{task['priority']}",
            "--label", f"milestone:{task['milestone']}",
            capture=False,
        )

    print("GitHub roadmap bootstrap complete.")


if __name__ == "__main__":
    main()
