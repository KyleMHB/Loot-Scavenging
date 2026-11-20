# Issue tracker: Local Markdown

Issues and specifications live in this repository under `.scratch/`.

## Conventions

- One feature per directory: `.scratch/<feature-slug>/`.
- Canonical specification: `.scratch/<feature-slug>/spec.md`.
- Implementation tickets: `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01`, one file per ticket.
- Record triage state as a `Status:` line near the top, using [triage labels](triage-labels.md). Existing bold `**Status:**` fields remain readable; preserve their format when updating old tickets.
- A ticket may carry a `**Recommended model:**` line under its status: `opus`, `sonnet` or `none`, then a dash and the reason. An orchestrator passes the value as the implementing subagent's model; `none` marks a human-only ticket.
- Append comments and conversation history under `## Comments` at the bottom.
- Fetch a referenced spec or ticket by reading its local file. Resolve a bare number within the named feature; numbers are not unique across features.

When a skill says to publish to the issue tracker, write the appropriate local Markdown file. Applying a label means setting its `Status:` value. GitHub/GitLab publication and a Git remote are not required.

## Wayfinding

- Map: `.scratch/<effort>/map.md`, containing Notes, Decisions-so-far and Fog.
- Child decisions: `.scratch/<effort>/issues/NN-<slug>.md`, with the question and `Type: research`, `prototype`, `grilling` or `task`.
- Dependencies: `Blocked by: NN, NN`. A child is unblocked when all listed children are `resolved`.
- Frontier: open, unblocked, unclaimed children in ascending numeric order.
- Claim by saving `Status: claimed` before work. Resolve by adding `## Answer`, saving `Status: resolved`, and appending the gist and child link to the map's Decisions-so-far.
- Wayfinding states describe decision work; the triage vocabulary describes implementation readiness.
