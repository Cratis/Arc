---
title: Returning commands as side effects
description: Return discoverable command side effects from a Chronicle reactor, with explicit authorization, replay, and retry boundaries.
---

When a book enters the catalog, its search index should follow. Return the indexing command from a reactor and let Arc execute it through the same validation and authorization pipeline as other commands. This keeps the reaction focused on **what should happen next**, rather than repeating command-execution plumbing.

This behavior belongs to the optional `Cratis.Arc.Chronicle` integration.

## Return a command

This complete type example assumes the integration is registered and your application supplies `ISearchIndex`. The event source id comes from the triggering event context rather than duplicating it in the event payload.

```csharp
using System;
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Concepts;

namespace Catalog.Books;

public record BookId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly BookId NotSet = new(Guid.Empty);
    public static BookId New() => new(Guid.NewGuid());
    public static implicit operator BookId(Guid value) => new(value);
}

public record BookTitle(string Value) : ConceptAs<string>(Value)
{
    public static readonly BookTitle NotSet = new(string.Empty);
    public static implicit operator BookTitle(string value) => new(value);
}

[Command]
public record CreateSearchIndex(BookId BookId, BookTitle Title)
{
    public Task Handle(ISearchIndex index) => index.Upsert(BookId, Title);
}

public interface ISearchIndex
{
    Task Upsert(BookId bookId, BookTitle title);
}

/// <summary>Records a book added to the catalog with its title.</summary>
[EventType]
public record BookAddedToCatalog(BookTitle Title);

public class CatalogIndexer : IReactor
{
    [OnceOnly]
    public Task<CreateSearchIndex> BookAdded(BookAddedToCatalog @event, EventContext context)
    {
        BookId bookId = Guid.Parse(context.EventSourceId.Value);
        return Task.FromResult(new CreateSearchIndex(bookId, @event.Title));
    }
}
```

`BookId` names the entity and gives its GUID event-source semantics; `BookTitle` names an ordinary domain value. These types prevent unrelated IDs or strings from being swapped in C# while retaining straightforward wire values. The conversion from the event context is explicit at the framework boundary. Reuse these concepts across the book feature instead of redeclaring them in each slice.

Return `Task<CreateSearchIndex>` so Chronicle recognizes the reactor handler before Arc processes its command result. This supported asynchronous signature also leaves room for real asynchronous work later; constructing the command itself does not need to be asynchronous.

`[OnceOnly]` skips the method during replay. It does **not** mean exactly-once delivery or prevent retries after failure. Make `Upsert` and subsequent commands safe to repeat.

## Authorization and the actor

Arc creates a dedicated service scope and executes returned commands with validation and authorization. This is not an HTTP request: the reactor does not inherit the original caller's authenticated principal.

For commands requiring an authenticated system actor, opt in on the reactor class with `Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute`, supplying only the roles needed by its returned commands. This is an attribute fragment to apply to the reactor, not a blanket requirement:

```csharp
[ExecuteCommandsAsSystem("catalog-writer")]
```

The attribute establishes system execution for **returned command side effects**. It does not automatically authorize manual `ICommandPipeline.Execute` calls inside the handler. Such calls need their own deliberate execution context. Do not confuse compliance subject or event causation with authorization credentials.

## Return several commands

This handler fragment assumes `BookRemovedFromCatalog` and the three application command types exist; each command takes the same `BookId` concept. Import `System.Collections.Generic` for the collection:

```csharp
[OnceOnly]
public IEnumerable<object> BookRemoved(BookRemovedFromCatalog @event, EventContext context)
{
    BookId bookId = Guid.Parse(context.EventSourceId.Value);
    return
    [
        new ArchiveBookMetadata(bookId),
        new RemoveFromSearchIndex(bookId),
        new NotifySubscribers(bookId)
    ];
}
```

`IEnumerable<object>` is a supported synchronous discovery shape. Commands run sequentially in one service scope; execution stops on the first unsuccessful result. There is no transaction across the collection: each command has its own [transactional scope](../commands/transactional-commands.md). Earlier successes remain if a later one fails and may run again on retry.

## Failure and manual execution

An unsuccessful returned command becomes a reactor side-effect failure. Chronicle's observer failure/recovery policy then applies; do not assume failure means all previously completed side effects were undone.

When you need to inspect the `CommandResult`, inject `ICommandPipeline` instead. This method fragment belongs in an `IReactor` with a `commands` dependency and the same event/command types as above:

```csharp
[OnceOnly]
public async Task BookAdded(BookAddedToCatalog @event, EventContext context)
{
    BookId bookId = Guid.Parse(context.EventSourceId.Value);
    var result = await commands.Execute(new CreateSearchIndex(bookId, @event.Title));
    if (!result.IsSuccess)
    {
        throw new SearchIndexingFailed();
    }
}
```

Use a named exception for the failed operation rather than a generic exception:

```csharp
public class SearchIndexingFailed() : Exception("Search indexing command failed.");
```

Here throwing deliberately tells the reactor to fail; logging and returning normally would acknowledge the event despite the failed command. [ARCCHR0006](../code-analysis/index.md#arcchr0006-manual-reactor-commands-and-replay) warns when a manual execution handler omits `[OnceOnly]`.

An asynchronous handler can perform manual work and then return supported side effects. Prefer one clear composition style, but mixing them is not a runtime prohibition. Chronicle also supports directly returned **events**; see [React to an event](../react-to-an-event.md).

## See also

- [Commands](../commands/index.md)
- [Chronicle reactors](/chronicle/reactors/)
- [Transactional commands](../commands/transactional-commands.md)
