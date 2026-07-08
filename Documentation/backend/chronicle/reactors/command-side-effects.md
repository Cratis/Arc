---
title: Returning commands as side effects
description: Let a reactor trigger follow-up commands by returning them — Arc executes them through the command pipeline automatically.
---

**Goal:** a reactor sees an event and needs to *change state* in response — index a book, archive a record, notify a downstream slice. The usual way is to inject `ICommandPipeline` and call `Execute` yourself. But when all a reaction does is fire one or more commands, that plumbing is noise. Instead, **return the command from the handler method** and Arc runs it for you.

## Return a command

A reactor method dispatches on the type of its first parameter, and its *return value* becomes the side effect. Return a `[Command]` and Arc executes it:

```csharp
[Command]
public record CreateSearchIndex(BookId BookId, string Title)
{
    public Task Handle(/* dependencies */) => /* ... */;
}

public class CatalogIndexer : IReactor
{
    public CreateSearchIndex BookAdded(BookAddedToCatalog @event) =>
        new(@event.BookId, @event.Title);
}
```

That is the whole reactor. No `ICommandPipeline`, no `Execute` call. Under the hood Arc detects that the returned value is a command, creates a dedicated service scope, and runs it through the pipeline — validation, authorization, and `Handle()` — exactly as if it had been sent from an HTTP endpoint.

The method can also be asynchronous — return `Task<CreateSearchIndex>` — if you need to compute the command with `await`.

## Return several commands

When one event should trigger a handful of commands, return them as a collection:

```csharp
public class BookArchiver : IReactor
{
    public IEnumerable<object> BookRemoved(BookRemovedFromCatalog @event) =>
    [
        new ArchiveBookMetadata(@event.BookId),
        new RemoveFromSearchIndex(@event.BookId),
        new NotifySubscribers(@event.BookId)
    ];
}
```

The commands run **sequentially, in order, within a single service scope**. If one fails, execution stops there — the commands after it do not run.

## When a command fails

If a returned command comes back unsuccessful — a validation error, an authorization failure, or an exception thrown from its `Handle()` — the reactor fails for that event. Chronicle then pauses the event source partition for that reactor until the problem is resolved and processing is retried, the same failure behavior as any other reactor that throws.

:::caution[This is not a transaction]
Returning several commands is a convenience, not an atomic unit of work. Each command appends its own events as it runs. If the third command fails, the events already appended by the first two **remain committed** — there is no rollback. If you need all-or-nothing semantics, model the operation as a single command that appends every event together.
:::

## When you need finer control

Returning commands is deliberately simple: run them, fail the reactor if any fails. When you need to *decide* what happens on failure — log and carry on, run a compensating command, branch on the result — inject `ICommandPipeline` and execute the command yourself so you can inspect the `CommandResult`:

```csharp
public class ResilientIndexer(ICommandPipeline commands, ILogger<ResilientIndexer> logger) : IReactor
{
    public async Task BookAdded(BookAddedToCatalog @event)
    {
        var result = await commands.Execute(new IndexBookForSearch(@event.BookId, @event.Title));
        if (!result.IsSuccess)
        {
            logger.LogWarning("Could not index book {BookId}", @event.BookId);
        }
    }
}
```

A single handler method does one or the other — either return commands, or execute them manually and return `Task`. It can't do both in the same invocation.

:::caution[Design for idempotency]
A reactor may be called more than once for the same event — during replay or recovery — so the commands it returns may run more than once. Make them safe to re-run, and derive their data from the triggering event rather than from a read model that may not have caught up.
:::

## See also

- [React to an event](../react-to-an-event.md) — the fundamentals of reactors and when to reach for one.
- [Commands](../commands/toc.yml) — how Arc commands are defined and executed.
