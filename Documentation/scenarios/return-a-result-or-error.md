---
title: Return a result or an error
description: Choose what a command's Handle() hands back — one event, several, a value for the caller, or a typed failure — and read it on the client.
---

**Goal:** your command needs to return more than a single event — maybe a generated id the caller needs, maybe a typed failure, maybe several events at once.

## `Handle()` returns what happened

A command's `Handle()` method returns the event(s) it produced, and Arc appends them. The *shape* you return is how you express intent — Arc understands several, so pick the one that fits rather than bending your logic to a single form.

## Do it

Return the shape that matches the outcome:

| You want to… | Return |
|---|---|
| record one event (source from the command's `[Key]`) | the event: `AuthorRegistered Handle()` |
| record one event against an explicit source | a tuple: `(AuthorId, AuthorRegistered) Handle()` |
| also hand the caller a value | a tuple of `(result, event)` |
| reject with a typed failure *or* succeed | `Result<ValidationResult, AuthorRegistered>` |
| record several events | return them together as an `IEnumerable<…>` |
| record nothing | `void` |

The `Result<,>` form is how a handler rejects based on state it had to consult — return `ValidationResult.Error("…")` to fail, or the event to proceed:

```csharp
public Result<ValidationResult, AuthorRegistered> Handle(RegisteredAuthorName? existing) =>
    existing is not null && existing.Name != AuthorName.NotSet
        ? ValidationResult.Error("An author with that name is already registered.")
        : new AuthorRegistered(Name);
```

## Reading it on the client

Whatever the handler returns, the generated proxy gives the caller a `CommandResult`. Check `isSuccess`, read any returned value off the result, and inspect validation errors when it failed — the same object carries all three.

## See also

- [Commands](/arc/backend/commands/) — the full command model and handler signatures.
- [Command Result](/arc/frontend/core/commands/command-result/) — reading the result on the frontend.
- [Validate a command](./validate-a-command.md) — where `ValidationResult.Error` comes from.
