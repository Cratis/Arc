---
title: Attribute reference
description: Every Arc attribute you write on a C# type, what it may be placed on, and what it changes.
---

Arc discovers what your application exposes from attributes on your own types.
This is the whole set you write, grouped by what it concerns. Attributes that
come from Chronicle or Fundamentals rather than Arc are marked as such, because
they arrive with a different package.

For the Kotlin and Java equivalents, see its
[annotation reference](/arc/backend/kotlin/reference/annotations/) — the concepts
match, the spellings do not.

## Artifacts

| Attribute | Valid on | Effect |
|---|---|---|
| `[Command]` | class | Marks a record or class as a command. Arc exposes it, validates and authorizes it, and calls its `Handle()` method. |
| `[ReadModel]` | class | Marks a type as a read model. Its `static` methods become queries. |

```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IMongoCollection<Author> authors) =>
        authors.InsertOneAsync(new Author(Id, Name));
}
```

## Routing and transport

| Attribute | Valid on | Effect |
|---|---|---|
| `[Path]` | class, method | Overrides the route Arc would otherwise derive from the type or method name. |
| `[QueryHttpMethod]` | class, method | Chooses the HTTP method a query is exposed with, where more than one is supported. |
| `[FromRequest]` | parameter, property | Binds a value from several request sources rather than the default one. |

A route you do not override is derived, so `[Path]` is for the cases where the
derived route is wrong for you — not something to apply everywhere.

## Authorization

| Attribute | Valid on | Effect |
|---|---|---|
| `[Authorize]` | class, method | Requires an authenticated caller, optionally satisfying a policy. |
| `[Roles]` | class, method | Requires **at least one** of the listed roles. |
| `[AllowAnonymous]` | class, method | Opens a specific artifact or operation back up. |

Apply them at class level to cover everything and override per method for the
exceptions — secure by default, opened where you mean it. A failure surfaces as
`isAuthorized: false` in the result rather than as an exception, so the frontend
can react to it.

## Validation

| Attribute | Valid on | Effect |
|---|---|---|
| `[IgnoreValidation]` | class, method | Excludes an artifact from validation that would otherwise apply. |

Most validation is declared in a `CommandValidator<T>` or `ConceptValidator<T>`
rather than with an attribute. See [validation](commands/validation.md).

## Hosting and registration

| Attribute | Valid on | Effect |
|---|---|---|
| `[IgnoreAutoRegistration]` | class | Keeps a type out of Arc's conventional discovery. |
| `[AspNetResult]` | class, method | Returns the ASP.NET Core result directly instead of Arc's result envelope. |

`[AspNetResult]` opts out of the envelope the generated client expects, so a
proxy will not consume that endpoint in the usual way. Reach for it when an
endpoint exists for something other than your own frontend.

## With the Chronicle integration

These arrive with Chronicle rather than Arc itself, and only apply when the
[Chronicle integration](chronicle/index.md) is in use.

| Attribute | Valid on | Effect |
|---|---|---|
| `[EventType]` | class | Marks a record as an event. Takes no arguments for a new event. |
| `[Key]` | property | Identifies the event source a command acts on. |
| `[NotAudited]` | class, struct, property, parameter | Keeps a secret out of the causation chain written with every event. |
| `[ExecuteCommandsAsSystem]` | class | Runs a reactor's returned commands without a user principal. |

`[NotAudited]` is the one worth knowing before you need it: a command's property
values are recorded in the causation chain of every event it appends, so an
unmarked token or password reaches the event log in clear text and stays there.
It is not interchangeable with Chronicle's `[PII]`, which encrypts and enrolls a
value in erasure — that is the right marking for personal data and the wrong one
for a secret.

## Related

- [Code analysis](code-analysis/index.md) — the analyzers that check these are used correctly.
- [Commands](commands/index.md) and [Queries](queries/index.md) — what the artifact attributes actually enable.
