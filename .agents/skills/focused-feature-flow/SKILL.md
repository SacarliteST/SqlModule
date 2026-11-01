---
name: focused-feature-flow
description: Implement or revise a SQLModule feature with targeted inspection and tests, reserving expensive Docker sandbox and full integration runs for a justified final gate.
---

# Focused feature flow

1. Read the requirement, check `git status`, and identify the affected feature, contract and acceptance criteria. Read `CONCEPT.md` or `ROADMAP.md` only when the feature crosses an unfamiliar domain.
2. Find one neighboring vertical slice and direct call chain with `rg`. Do not read whole projects or old technical-debt archives without a concrete dependency.
3. Keep the change inside the required layers and repository. Do not modify frontend, platform services, deployment, or generated artifacts unless explicitly included.
4. Implement production behavior in one coherent batch. Add focused tests once the shape is stable; use a reproducing test first only for an actual regression.
5. Test public behavior, state transitions, concurrency boundaries and persistence invariants. Avoid tests that merely restate private methods or create large combinatorial matrices without a requirement.

## Validation ladder

- Pure domain/application change: build the affected project or solution when useful, then run the relevant `UnitTests` filter.
- API/persistence change: run the relevant `IntegrationTests` class/filter after the slice is coherent.
- Sandbox/DBMS change: run only the relevant Docker test class; do not start every DBMS or the full sandbox suite by default.
- On failure: iterate on the failing test only.
- Final gate: format once and build once. Run the full unit/integration suite once only for cross-cutting persistence, authorization, migrations, concurrency, sandbox infrastructure, or when explicitly requested. For an isolated change, targeted green tests are sufficient.

Do not treat `dotnet format` as a reason to rerun all logic tests. Run it at the end, inspect the diff, and separate broad mechanical changes from functional work.

Do not start hosts, containers, Swagger export, migrations, client generation, commits or pushes unless required. All migrations, Swagger/OpenAPI export, Orval and client generation must use `unsandboxed-codegen` outside the sandbox. Stop only processes and containers started by the agent.
