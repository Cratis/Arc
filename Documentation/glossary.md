---
title: Glossary
description: One precise line per Arc term, stated once so it means the same thing in every backend language and on the frontend.
---

Arc is implemented for more than one backend language, and the same idea
sometimes carries a different spelling in each. This page defines the idea. Where
the spellings differ it names both, so a term you meet in one implementation is
recognisable in the other.

Terms that belong to one implementation only live in that implementation's own
glossary: [C#](/arc/backend/csharp/) and
[Kotlin and Java](/arc/backend/kotlin/reference/glossary/).

## Write side

- **Command** — a declared intention to change something. A type marked
  `[Command]` (C#) or `@Command` (JVM) whose properties are the inputs. Arc
  validates and authorizes it before anything runs.
- **Handler** — the method on the command that decides what happens. `Handle()`
  in C#, `handle` on the JVM. It receives its dependencies as parameters rather
  than reaching for them.
- **Provide** — an optional method that runs after validation and authorization
  and before the handler, to fetch or compute what the decision needs. Its return
  value is passed into the handler, which keeps the decision a function of its
  arguments. It can also short-circuit, rejecting the command before it is
  handled.
- **Command key** — the property that says *which* thing a command acts on.
  `[Key]` / `ICanProvideKeyForCommand` in C#, `@CommandKey` /
  `CommandKeyProvider` on the JVM. Exactly one per command; a command that
  creates something new has none.
- **Command result** — the envelope every command returns: whether it succeeded,
  the response value if any, validation results, and whether the caller was
  authorized. The frontend reads the same shape from either backend.
- **Command filter** — a cross-cutting rule applied around command execution.
- **Execution scope** — a lifetime concern bracketing a whole command: it begins
  before filters and the handler and completes afterwards for every outcome,
  which is how a transaction wraps a command rather than living inside it.

## Read side

- **Read model** — a type marked `[ReadModel]` / `@ReadModel` holding state shaped
  for a particular screen or question, rather than for storage.
- **Query** — a method on a read model that returns it. Static in C#, a
  `@JvmStatic` companion function or Java static method on the JVM.
- **One-shot query** — a query that answers once and completes.
- **Observable query** — a query whose return type keeps producing. The return
  type is the whole difference between load-once and stay-live: `ISubject<T>` in
  C#, a `Flow`, `StateFlow` or `Flow.Publisher` on the JVM.
- **Change stream** — the delta form of an observable query result: what was
  added, replaced or removed, rather than the whole collection again.
- **Paging and sorting** — request-level concerns Arc applies around a query, so
  a query does not implement them itself.

## Validation and access

- **Validation result** — a rejection with a message, the members it concerns and
  a severity. Produced before the handler runs, and surfaced in the command
  result so the frontend can show it against the right field.
- **Concept** — a wrapper giving a domain value its own type rather than passing
  a bare primitive: `ConceptAs<T>` in both implementations. Validation attached to
  a concept travels with the value everywhere it appears.
- **Identity details** — the application's own answer to "who is this user", built
  once per request from the authenticated principal and exposed to the frontend.
- **Authorization metadata** — the roles or policy a command or query requires.
  Declared on the type and overridable per operation, so a class can be closed by
  default and one operation opened.
- **Tenant resolution** — deciding which tenant a request belongs to, before any
  storage is selected. Selecting a tenant is not the same as proving the caller
  may access it.

## The boundary

- **Proxy generation** — producing the TypeScript client from the backend's own
  types during the build, so the frontend contract cannot drift from the backend.
  Each backend discovers its artifacts its own way; the emitted client is the
  same shape.
- **Generated proxy** — the TypeScript command or query the frontend imports. It
  is build output, never edited by hand.
- **Artifact** — anything Arc discovers and exposes: a command, a query, a read
  model, a validation rule, an identity-details provider.
- **Introspection endpoints** — the runtime description of the registered
  artifacts, served under `/.cratis/`.

## Optional integrations

- **Chronicle integration** — the optional event-sourced write path. A handler
  returns events instead of writing directly, and Arc appends them. Arc does not
  require it.
- **Current-state persistence** — the ordinary alternative: the handler writes to
  a database through an injected service or repository.
