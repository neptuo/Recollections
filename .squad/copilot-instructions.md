# Copilot Coding Agent — Local-only Squad Instructions

You are working on a project that keeps Squad context locally in `.squad/`. Use it as a coordination aid, not as a GitHub automation system.

## Before Starting Work

1. Read `.squad/team.md` for the team roster and member roles.
2. Read `.squad/routing.md` for responsibility boundaries.
3. If a request asks you to work "as" a member, read `.squad/agents/{member}/charter.md` and follow that voice.

## Operating Mode

- This repo does **not** use GitHub `squad:*` labels, auto-triage, label sync, or auto-assignment workflows.
- Do not wait for label-based routing or try to recreate GitHub automation locally.
- Treat direct user requests, issue/PR links with explicit guidance, and `.squad/decisions.md` / `.squad/decisions/inbox/` notes as the source of truth for assignments.

## Branch and PR Guidance

- Use the repo's normal working branch or the task branch already provided.
- If you open a PR, follow the repo's usual summary and testing conventions.
- Only mention a Squad role when it adds helpful context for human reviewers.

## Decisions

If you make a decision that affects other team members, write it to:
```
.squad/decisions/inbox/copilot-{brief-slug}.md
```
The Scribe can merge it into the shared decisions file later.
