---
title: Choose a Cratis Arc backend
description: Choose a Cratis Arc backend for C#, Kotlin, Java, or TypeScript. Compare ASP.NET Core, Spring Boot, and Node.js hosts and their supported capabilities.
---

Cratis Arc provides backends for C# on ASP.NET Core, Kotlin and Java on Spring
Boot, and TypeScript on Node.js. Each implementation follows CQRS: commands
request changes and queries read data, with generated TypeScript clients
connecting those contracts to the frontend. The host, APIs, and supported
capabilities differ, so choose a path below before copying setup or integration
code.

## Pick your language

- [C#](/arc/backend/csharp/) — ASP.NET Core and the lightweight Arc.Core host,
  with MongoDB and Entity Framework Core integrations, Roslyn analyzers, and
  post-build TypeScript proxy generation.
- [Kotlin and Java](/arc/backend/kotlin/) — Spring Boot, with Spring Data JPA
  and MongoDB integrations, KSP compile-time diagnostics, and Gradle-driven
  TypeScript proxy generation.
- [TypeScript](/arc/backend/typescript/) — Node.js, on its own HTTP host or in
  Express, Fastify, or Hono, with MongoDB and Drizzle SQL integrations, a
  Chronicle integration with a documented subset of capabilities, ESLint rules,
  and TypeScript proxies generated from your source. No package is published
  to npm, and it does not claim full parity with the C# implementation. Its
  [capability reference](/arc/backend/typescript/reference/capabilities/)
  gives the status of each capability.

## What the implementations share

A shared command/query model lets you organize a feature as a vertical slice:
keep its write, read, frontend, and specs together. It does not make backend
code or configuration interchangeable.

These implementations share the application-boundary model and frontend
packages. Wire compatibility is capability-specific; implementation references
document supported behavior and deliberate differences. The [HTTP contract](/arc/http-contract/)
describes the shared contract verified for C# and JVM, while the TypeScript
[capability reference](/arc/backend/typescript/reference/capabilities/) identifies
what that backend supports.

## The frontend is shared

Every backend generates proxies for the same `@cratis/arc` and
`@cratis/arc.react` packages, so the [frontend documentation](/arc/frontend/)
applies whichever backend you chose. First complete the chosen backend's setup
and proxy-generation steps, then use those generated clients in the frontend.
After a backend contract changes, regenerate and type-check its frontend callers.
The steps for running the generator differ by language; see your backend's proxy
generation page.
