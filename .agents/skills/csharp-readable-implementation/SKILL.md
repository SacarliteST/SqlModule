---
name: csharp-readable-implementation
description: Write readable C# and .NET code in this repository using ordinary constructors, feature-oriented files, explicit application flow, restrained LINQ, and framework-aware validation.
---

# Readable C# implementation

Apply `$human-readable-code` together with the repository development skill.

## Types and layout

- Use one meaningful type per file and match the file name to the type.
- Place code in the owning feature folder. Put DTOs, interfaces and exceptions in the repository's designated feature subfolders rather than in unrelated common buckets.
- Use ordinary constructors for services and handlers, not primary constructors.
- Constructor bodies should normally only assign dependencies. Do not add repeated null guards for services created by dependency injection.
- Validate options once through the project's options validator or startup validation. Do not repeat configuration-range checks in every consumer constructor.
- Use records for immutable contracts or value-like data, not automatically for services or stateful domain entities.

## Control flow and data access

- Keep handlers as readable use-case orchestration. Move domain decisions to domain behavior and infrastructure mechanics behind existing interfaces.
- Use `Result` for expected business failures where the repository already follows that pattern; reserve exceptions for exceptional or infrastructure failures.
- Keep LINQ readable. Split a long query or projection into named stages when that exposes intent, but do not materialize data merely to simplify syntax.
- Use `AsNoTracking()` for read-only EF queries and pass `CancellationToken` through asynchronous calls.
- Prefer EF Core for ordinary CRUD and queries. Use raw SQL only for a database primitive EF cannot express adequately, such as a required lock or atomic database operation, and leave the reason visible.
- Avoid fire-and-forget work unless an established background or outbox mechanism owns it.

## Contracts, logs and documentation

- Keep request DTO nullability separate from non-null domain state.
- Add XML documentation where the repository requires it for public contracts and APIs. Internal comments must add reasoning, not ceremonial text.
- Use structured logging with stable identifiers. Follow the repository's language and never log secrets, tokens, connection strings, raw unsafe database errors or full request bodies.
- Do not suppress compiler or nullable warnings to make a build pass unless the suppression documents a real invariant.

Write tests around the public result, persisted state and externally visible side effects. Do not expose internals solely to make them easier to test.

