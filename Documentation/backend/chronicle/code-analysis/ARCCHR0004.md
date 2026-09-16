---
title: "ARCCHR0004: [EventType] repeats the type name as its id"
description: An [EventType] attribute passes the type's own name, or an empty string, as its id — neither changes what the event type resolves to.
---

## Rule

Chronicle resolves an event type's identifier from `[EventType]`'s `id` argument when one is given, and falls back to the CLR type name otherwise. Passing an id equal to the type's own name — or an empty string — produces exactly the identifier the attribute would have used anyway. This rule fires on both:

```csharp
[EventType("AuthorRegistered")]  // ARCCHR0004: this is already the default
public record AuthorRegistered(string Name);

[EventType("")]  // ARCCHR0004: same as a bare [EventType]
public record AuthorRegistered(string Name);
```

The comparison is ordinal, matching how Chronicle compares event type identifiers, and it only considers a **constant** id — one the compiler can evaluate at the call site (a literal, `nameof(...)`, or a reference to a `const` field). A non-constant id cannot be checked against the type name here, so it is left alone rather than guessed at.

## Severity

Warning

## Example

### Violation

```csharp
using Cratis.Chronicle.Events;

// ARCCHR0004: 'AuthorRegistered' passes its own name as the id
[EventType("AuthorRegistered")]
public record AuthorRegistered(string Name);
```

### Fix

```csharp
using Cratis.Chronicle.Events;

[EventType]
public record AuthorRegistered(string Name);
```

## When It Does Not Fire

**An id that differs from the type name is left alone — it is the documented way to rename an event record.** Chronicle resolves the stored event type by its id, not by the CLR type name, so pinning the original id is what lets you rename the record in code while every event already appended under the old name keeps resolving:

```csharp
using Cratis.Chronicle.Events;

// The record used to be called FeatureUITemplateSet. Renaming it in code is safe
// because the id keeps every already-stored event resolving under its original name.
[EventType("FeatureUITemplateSet")]
public record FeatureScreenTemplateSet(string TemplateId);
```

**Removing the id here is not a cleanup — it orphans every stored event.** Once the id is gone, `[EventType]` falls back to the type name, `FeatureScreenTemplateSet`, and every event Chronicle already has on disk under `FeatureUITemplateSet` stops resolving to this type. Only remove an id once you have confirmed no stored event still depends on it — and even then, prefer leaving it as its own explicit statement of the type's history over relying on the rule's silence to justify the removal.

The rule also stays silent on a few narrower cases:

| Shape | Why it is left alone |
|---|---|
| An id that differs from the type name, in any casing | A genuinely different identifier — the comparison is ordinal, so `[EventType("authorregistered")]` on `AuthorRegistered` is not redundant |
| A non-constant id (a variable, a method call, a property) | Cannot be evaluated at compile time, so it cannot be compared against the type name |
| The `generation` argument, alone or alongside an id | Never part of this rule — evolving an event's generation is unrelated to its id |

## Why This Rule Exists

An id equal to the type name is pure noise: it changes nothing about how the event resolves, but it reads as if it might matter, which is exactly what leads someone to "simplify" a genuinely load-bearing id on a different type later. Reporting only the provably redundant case — not every explicit id — keeps that noise out without ever risking a false positive against the rename pattern the id argument exists to support.

## Related Rules

- [ARCCHR0006](ARCCHR0006.md) — Reactor handler invoking ICommandPipeline.Execute does not say what replay should do
