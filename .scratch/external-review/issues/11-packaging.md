# 11: Packaging

Status: done

**Recommended model:** `sonnet`

**Blocked by:** None (can start immediately)

**What to build:** The Workshop upload carries the licence, the textures have their own folder, and the project docs for agents are current. Spec, "Packaging and docs".

## Scope

- Add LICENSE to `deploy.ps1` and `DevTests/devtests.json`.
- Move the textures to `Things/Item/LootScavenging/Scrap/` (decision 4): the 12 `texPath` lines, the `Scrap.xml` L5 comment, the folders and `tools/preview_icon.py` L18.
- Bring `AGENTS.md` up to date.
- README line on the csproj `-p:` path overrides.

## Done when

- A deployed copy has LICENSE and the scrap textures load.
