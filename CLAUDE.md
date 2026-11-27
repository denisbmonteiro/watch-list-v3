# CLAUDE.md

Rules for working in this repository. The README covers what the app does, how to run it, the
API and the data format; read it first. This file covers how to work here.

## Implementing an issue

1. Read the whole issue, including its "Decisions already made" and "Rules for every phase"
   sections. Those decisions are final: don't relitigate them, and don't add anything listed under
   "Out of scope". If the issue is ambiguous or contradicts the code, ask before guessing.
2. If the issue is one of a series of phases, check that the previous phase is merged into
   `main`. If it isn't, stop and say so.
3. Start from an up-to-date `main` on a new branch:
   ```bash
   git switch main && git pull --ff-only && git switch -c <branch>
   ```
   Use the branch name the issue gives (e.g. `feat/database-phase-<n>`). Otherwise use
   `<type>/<short-kebab-description>`, with the same types as the commits.
4. Implement it, run the checks under "Definition of done", then commit and open a PR back to
   `main` with `gh pr create --base main`. Put `Closes #<n>` in the body.
5. **Never merge a PR, and never push to `main`**, unless the owner asks explicitly in that
   message. A push to `main` deploys straight to production (see "Deploy").

Outside of implementing an issue, don't commit unless asked.

## Definition of done

- `dotnet build WatchList.slnx` passes. Warnings are errors (`TreatWarningsAsErrors`,
  `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild`). Fix the code; never turn a rule
  off globally to get around it. If a suppression is really needed, scope it as narrowly as
  possible and say why.
- `dotnet test WatchList.slnx` passes, with tests added for the new behavior. Tests use xUnit v3
  and Shouldly; follow the layout and the `Method_does_something` naming already in `tests/`.
- If the change touches code, configuration, the `Dockerfile` or static files, run
  `bash scripts/verify-docker.sh` and make sure it passes. It needs Docker, and so do the
  Testcontainers tests.
- The README is updated whenever routes, configuration, how to run the app or the data format
  change.
- Production keeps working after the merge. The Presentation is what's deployed; anything that
  isn't ready must not reach it.

## Commits and PRs

- Commits, branch names and PRs are in English.
- Use conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `build:`, `style:`,
  `chore:`. Write the subject in the imperative and lowercase, without a trailing period. Make
  small, focused commits; the tests can go in a commit of their own.
- Commits carry only the owner's name: **no `Co-Authored-By` trailer**. No "Generated with Claude
  Code" footer in PRs either.
- The PR body has a `## Summary` section (what changed and why, as bullets) and a
  `## Verification` section (what was run and what it showed, e.g. the number of tests passing).
  If production is affected, say so explicitly; if it isn't, say that too.

## Code conventions

- Clean Architecture layers: `Domain` ← `Application` ← `Infrastructure` ← `Presentation` / `Api`,
  with `Shared` usable from all of them. Don't add references that point the other way, and don't
  add new projects unless the issue says to.
- Package versions live only in `Directory.Packages.props` (central package management). Don't
  put versions in a `.csproj`.
- `.editorconfig` is the style source: file-scoped namespaces, `var`, primary constructors,
  `_camelCase` private fields, and braces on their own line.
- Comments and XML docs in C# are in English. Config files and scripts that already have
  Portuguese comments stay in Portuguese.
- **No file ends with a newline**: the last byte is the end of the last line. When you write a
  file from the shell, strip the trailing `\n` (e.g. with `printf '%s'` or `truncate -s -1`).

## Don't touch

- `data/*.txt` and `src/WatchList.Presentation/wwwroot/images/`. They're the owner's personal
  lists and covers, changed only in `chore: update watchlist` commits. Tests use
  `tests/WatchList.Infrastructure.Tests/Fixtures/`.
- Covers are updated only through the `/update-covers` skill.
- Never commit secrets (connection strings with real passwords, JWT keys, the admin password).
  For local development, use user-secrets or the dev-only values the issue gives.

## Deploy

Dokploy on a VPS builds the `Dockerfile` and publishes on every push to `main`. There's no CI and
no test step in that pipeline, so the local checks above are the only safety net. The image ships
only `WatchList.Presentation`.