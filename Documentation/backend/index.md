---
title: Backend
description: Choose the Arc backend implementation for your language and compare shared concepts with capability-specific behavior.
---

Arc's backend gives commands, queries, validation, authorization and proxy
generation one application boundary. That shape is the same whichever language
you write it in — a command is a declared intention that is validated,
authorized and handled, and a query is a purpose-shaped read. What differs is
the host you run it on and the API surface you write against.

## Pick your language

- [C#](/arc/backend/csharp/) — ASP.NET Core and the lightweight Arc.Core host,
  with MongoDB and Entity Framework Core integrations, Roslyn analyzers, and
  post-build TypeScript proxy generation.
- [Kotlin and Java](/arc/backend/kotlin/) — Spring Boot, with Spring Data JPA
  and MongoDB integrations, KSP compile-time diagnostics, and Gradle-driven
  TypeScript proxy generation.
- [TypeScript](/arc/backend/typescript/) — Node.js, on its own HTTP host or in
  Express, Fastify, or Hono, with MongoDB and Drizzle SQL integrations, an
  experimental Chronicle integration, ESLint rules, and TypeScript proxies
  generated from your source. It is a source preview: no package is published
  to npm and it does not have full parity with the C# implementation. Its
  [capability reference](/arc/backend/typescript/reference/capabilities/)
  gives the status of each capability.

## What the implementations share

These implementations share the application-boundary model and frontend
packages. Wire compatibility is capability-specific; implementation references
document supported behavior and deliberate differences. The [HTTP contract](/arc/http-contract/)
describes the shared contract verified for C# and JVM, while the TypeScript
[capability reference](/arc/backend/typescript/reference/capabilities/) identifies
what its source preview supports.

## The frontend is shared

Every backend generates proxies for the same `@cratis/arc` and
`@cratis/arc.react` packages, so the [frontend documentation](/arc/frontend/)
applies whichever backend you chose. The steps for running the generator differ
by language; see your backend's proxy generation page.
