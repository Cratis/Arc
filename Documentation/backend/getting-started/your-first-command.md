---
title: Your first command and query
description: Build a backend slice in Arc — a command, the event it produces, the read model a projection builds, and the query that serves it.
---

This builds the backend half of a feature: a **command** that does something, the **event** it records, the **read model** a projection builds from that event, and the **query** that serves it. Put all of it in one `Features/Authors/` folder — Arc organizes by feature, not by technical layer.

## 1. A strongly-typed id

Never pass raw `Guid`s around the domain — wrap them so the compiler keeps them straight and your signatures document themselves:

```csharp
public record AuthorId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static AuthorId New() => new(Guid.NewGuid());
    public static implicit operator EventSourceId(AuthorId id) => new(id.Value.ToString());
}
```

## 2. The command — with `Handle()` on the record

A command is a `record` marked `[Command]`. The behavior lives in a `Handle()` method **on the record itself** — there is no separate handler class. It returns the event(s) that happened:

```csharp
[Command]
public record RegisterAuthor(AuthorId Id, AuthorName Name)
{
    public AuthorRegistered Handle() => new(Name);
}
```

`Handle()` can return a single event, a tuple of `(event, result)`, a `Result<,>`, or several events. Inject collaborators as parameters when you need them.

## 3. The event

An immutable, past-tense fact:

```csharp
[EventType]
public record AuthorRegistered(AuthorName Name);
```

## 4. The read model and its projection

Declare the shape you want to query, mark it `[ReadModel]`, and tell it which event feeds it. AutoMap matches `AuthorRegistered.Name` → `Name`, so there's no update code. A static method exposes the query — return an observable so consumers get live updates:

```csharp
[ReadModel]
[FromEvent<AuthorRegistered>]
public record Author([property: Key] AuthorId Id, AuthorName Name)
{
    public static ISubject<IEnumerable<Author>> AllAuthors(IMongoCollection<Author> collection) =>
        collection.Observe();
}
```

That static method **is** your query — Arc exposes it over HTTP automatically. No controller.

## 5. Build

```bash
dotnet build
```

Building does two things beyond compiling: it appends events through Chronicle when commands run, and it **generates the TypeScript proxies** for `RegisterAuthor` and `AllAuthors` so your frontend can call them type-safely.

## What you built

- A `[Command]` with `Handle()` — intent and implementation in one place.
- The `[EventType]` it records.
- A `[ReadModel]` whose projection builds it from events, with a query method served automatically over HTTP.

## Next

- Wire a UI to it in the [frontend getting started](/arc/frontend/getting-started/).
- See the whole slice end to end in [Build a full-stack feature](/build-a-full-app/).
- Go deeper on [Commands](/arc/backend/commands/) and [Queries](/arc/backend/queries/).
