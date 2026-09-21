---
title: Backend
description: Choose the Arc backend implementation for your language, and see what every implementation agrees on.
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

## What every implementation shares

The pieces below are contracts rather than APIs, so they hold across languages.
A client generated from one backend talks to the other without changes.

- The HTTP contract — routes, request and response envelopes, status codes, and
  correlation.
- The command and query result shape, including validation results and their
  severities.
- Observable queries and their transports, and how collection changes are
  transferred.
- Identity, authorization and tenant resolution semantics.

Where an implementation deliberately differs, its own pages say so rather than
leaving you to infer it.

## The frontend is shared

Both backends generate against the same `@cratis/arc` and `@cratis/arc.react`
packages, so the [frontend documentation](/arc/frontend/) applies whichever
backend you chose.
