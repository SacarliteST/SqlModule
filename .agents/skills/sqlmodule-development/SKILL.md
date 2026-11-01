---
name: sqlmodule-development
description: Develop SQLModule backend features across training, schema authoring, SQL analysis, sandbox pools, platform integrations, OpenAPI, EF Core, and tests while preserving clean layer boundaries.
---

# SQLModule backend development

Start with `CONCEPT.md`; consult `ROADMAP.md` for phase boundaries and root `../../PLATFORM.md` only for platform integration. Inspect the current implementation because older planning documents may describe superseded behavior.

## Layer routing

- `Domain`: entities, value rules and state transitions.
- `Application`: CQRS use cases and interfaces; no Web or infrastructure clients.
- `Contracts`: public DTOs, enums and API routes. DTOs belong in feature-oriented DTO folders.
- `Data`: EF Core context, configurations and migrations.
- `Web`: Minimal API endpoints, validators, authorization, mappings and composition only.
- `Sandbox`: execution and warm-pool infrastructure.
- `SqlAnalysis`: dialect parsing and SQL/AST analysis.
- `Integrations/*`: each external HTTP client, Kafka publisher, contracts and abstractions in the appropriate dedicated project; integration clients do not live in `Web`.
- `Client`: typed SQLModule API client.
- `UnitTests`, `IntegrationTests`, `TestsCommon`: tests and shared fixtures.

Use one type per file. Group files by feature; interfaces and exceptions belong in explicit feature folders. Use ordinary constructors, not primary constructors. Do not fill constructors with redundant null guards or validation already guaranteed by DI/options validation. Add Russian XML comments to public contracts and meaningful internal types; logs should be in Russian and must not expose SQL infrastructure secrets or raw unsafe errors.

## Important invariants

Preserve the `SqlTask` to reference query one-to-one model and existing platform-session scoping. A JWT with `session_id` can access only its bound task; standalone behavior is separate. Student DTOs must not expose reference SQL, raw DB errors, infrastructure names or another user's data.

Use optimistic concurrency or the established transactional mechanism for shared state. Do not introduce raw SQL merely for ordinary EF CRUD; use it only when a database primitive is genuinely required and document the invariant.

Public API changes require matching OpenAPI metadata, typed client updates and contract tests. Generate migrations, Swagger and clients through `unsandboxed-codegen`; never hand-edit generated frontend clients.

Follow `$focused-feature-flow` and avoid repeated full Docker/integration runs.
