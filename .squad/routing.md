# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Product scope & architecture | Morpheus | Feature slicing, design trade-offs, review gates, cross-cutting changes |
| Blazor UI & user flow | Trinity | Components, pages, forms, styling, client-side interaction |
| API, domain logic & persistence | Tank | ASP.NET endpoints, validation, data access flow, shared models |
| Platform & delivery | Dozer | Aspire AppHost, Docker, deployment config, CI/build plumbing |
| Testing & regression review | Switch | Test plans, edge cases, integration checks, bug repros |
| Code review | Morpheus | Review PRs, check quality, suggest improvements |
| Testing | Switch | Write tests, find edge cases, verify fixes |
| Scope & priorities | Morpheus | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Work Intake (Local-only)

This repo uses Squad for local coordination only. GitHub `squad` labels, auto-triage, and assignment workflows are intentionally disabled.

Use these signals to route work:

1. A direct user request or handoff in chat
2. An issue or PR link with explicit human guidance
3. A note in `.squad/decisions.md` or `.squad/decisions/inbox/`

If a GitHub issue exists, labels and assignees are optional human-managed metadata, not automation triggers.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Manual routing** — route work from explicit requests or written handoffs; do not rely on `squad:{member}` labels or GitHub automation.
