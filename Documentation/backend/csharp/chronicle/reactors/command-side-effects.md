---
title: Returning commands as side effects
description: Return discoverable command side effects from a Chronicle reactor, with explicit authorization, replay, and retry boundaries.
---

When a book enters the catalog, its search index should follow. Return the indexing command from a reactor and let Arc execute it through the same validation and authorization pipeline as other commands. This keeps the reaction focused on **what should happen next**, rather than repeating command-execution plumbing.

This behavior belongs to the optional `Cratis.Arc.Chronicle` integration.

:::note[Reactor side effects are not command operations]
This page describes a reactor returning commands. Inside one of those commands, prefer [command operations](../../commands/operations/index.md) for immediate service side effects and optional compensation. Return the operation from the command's `Handle()`, not directly from the reactor. Keep the reactor when work should follow a committed event; inline command operations do not add durable delivery, replay protection, or exactly-once execution.
:::

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

The attribute establishes system execution for **returned command side effects**. Arc keeps that actor within the command execution's asynchronous lifetime, including service-scope creation and pipeline resolution. The caller's identity remains unchanged on success, command rejection, exceptions, and setup failures. It does not automatically authorize manual `ICommandPipeline.Execute` calls inside the handler. Such calls need their own deliberate execution context. Do not confuse compliance subject or event causation with authorization credentials.

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

## Return events and commands together

When indexing also needs a recorded request, return a stable `IEnumerable<object>`, `object[]`, or `List<object>` containing both registered events and `[Command]` records. A `Task` returning one of these collections works too. This fragment replaces `CatalogIndexer` above and reuses its domain types. Import `System.Collections.Generic` and `Cratis.Chronicle.EventSequences`:

```csharp
[EventType]
public record BookIndexingRequested(BookTitle Title);

public class CatalogIndexer : IReactor
{
    [OnceOnly]
    public IEnumerable<object> BookAdded(BookAddedToCatalog @event, EventContext context)
    {
        BookId bookId = Guid.Parse(context.EventSourceId.Value);
        return
        [
            new EventForEventSourceId(context.EventSourceId, new BookIndexingRequested(@event.Title)),
            new CreateSearchIndex(bookId, @event.Title)
        ];
    }
}
```

Arc appends **all events as one batch first**, in their relative order, then executes the commands in their relative order. Interleaving commands with events in the returned collection does not interleave execution. Bare events use the reactor's resolved event-source, stream, source-type, and subject metadata; `EventForEventSourceId` keeps its explicit destination, tags, named tags, and other append metadata.

If append fails, no command runs. If a command fails, later commands stop, but the appended batch and earlier successful commands remain committed. There is no transaction or compensation across events and commands. A retry can append the events again and repeat commands that already succeeded, so make both effects safe to repeat.

Return an array or list whose contents stay stable. Chronicle's discovery and dispatch can enumerate the sequence more than once; do not produce effects while enumerating it. Each element must be a registered event, an envelope containing a registered event, or a command, never both an event and a command. Nulls, unknown values, and `CompleteStream` are not accepted in the mixed collection. `[ExecuteCommandsAsSystem]` applies only while executing the command partition, not while appending the event partition.

## Handle a command refusal

Return an invocation when a business refusal should record a different outcome instead of failing the observer partition. This replacement for `CatalogIndexer` reuses `BookAddedToCatalog`, `BookId`, and `CreateSearchIndex` above. Import `Cratis.Arc.Chronicle.Reactors`:

```csharp
[EventType]
public record BookIndexingRefused(string Message);

public class CatalogIndexer : IReactor
{
    [OnceOnly]
    public CommandInvocation BookAdded(BookAddedToCatalog @event, EventContext context)
    {
        BookId bookId = Guid.Parse(context.EventSourceId.Value);
        return Invoke.Command(new CreateSearchIndex(bookId, @event.Title))
            .OnRefused(Refused.ByConstraint("UniqueBook"), refusal => new BookIndexingRefused(refusal.Message))
            .OnRefused(Refused.ByValidation, refusal => new BookIndexingRefused(refusal.Message))
            .Acknowledge(Refused.ByAuthorization);
    }
}
```

Synchronous `CommandInvocation` returns no longer raise `CHR0054` with Chronicle 19.63.0 or later.

Use your command's actual constraint name in place of `UniqueBook`. Register branch event types just like other returned events. The command still runs through validation and authorization; a branch cannot bypass either.

The **first matching branch wins**. It returns a registered event, an `EventForEventSourceId`, or a stable nonempty sequence of those. Use `.Acknowledge(selector)` to acknowledge without producing events; a branch handler that returns `null` also produces no events. Acknowledgment records a handled refusal; it does not prove that the command's work previously completed. Branches cannot return commands, operations, or stream completions.

[ARCCHR0027](../code-analysis/ARCCHR0027.md) warns when a visible `OnRefused` callback returns a command, operation, completion, non-event object, empty batch, or a batch with an invalid member. It checks lambdas, anonymous methods, and method groups whose bodies are available. Opaque helper results still require runtime validation. The runtime validates the whole branch result before appending any of its events; an invalid result fails the delivery, not just the invalid member.

A handled refusal appends its branch events and **stops all remaining commands or invocations** in this delivery. If the branch append fails, the reactor fails; another refusal branch cannot handle that append failure. Bare branch events use the same resolved event-source, stream, source-type, and subject metadata as ordinary returned events. Explicit envelopes retain their metadata.

Return an invocation on its own, `Task<CommandInvocation>`, or a stable sequence of invocations and plain commands. You can also mix them with events using the existing events-first rule: ordinary returned events append as one batch before any command runs, then branch events append when a refusal is handled. Previously appended events and successful commands remain committed on failure or acknowledgment. Refusal handling adds no transaction, redelivery deduplication, or exactly-once guarantee.

The [Screenplay generator's refusal extraction](../../generating-a-screenplay.md#command-refusal-branches) recovers pure `Invoke.Command(...).OnRefused(...)` and `.Acknowledge(...)` chains as ordered executable `on refused` branches at ESM v19. Opaque callbacks and caps below v19 keep the whole trigger file-backed with `SP0071`; runtime support does not imply every C# callback is extractable.

### Refusal selectors and details

`CommandResult.GetRefusal()` in `Cratis.Arc.Commands` returns a `CommandRefusal`, or `null` for a non-refusal result. `CommandRefusal.Kind` is `CommandRefusalKind.Validation`, `Constraint`, or `Authorization`; `ConstraintNames` carries all distinct violated names in result order, with nullable `ConstraintName` retaining the first error constraint's name; `Message` preserves authored text, including unresolved `$strings.` keys.

| Selector | Matches |
| --- | --- |
| `Refused.ByValidation` | Error-severity authored rules, including property and concept rules and `require`. |
| `Refused.ByConstraint("Name")` | An error constraint refusal with that exact, case-sensitive name among its violated names. |
| `Refused.ByAnyConstraint` | Any error constraint refusal, including one without a name. |
| `Refused.ByAuthorization` | An actual unauthorized result (`IsAuthorized` is false). |
| `Refused.Any` | Validation and constraint refusals only, **never authorization**. |

Selectors are inspectable sealed records: `Kinds` exposes the matched refusal kinds, and `ConstraintName` is the optional exact-name restriction. Equal kinds and names compare as equal selectors. Use `ByAnyConstraint` for an unrestricted constraint branch; `ByConstraint(name)` requires a name.

For mixed business refusals, authorization wins, then constraints, then validation. Authorization uses `AuthorizationFailureReason` verbatim. For constraints, all error constraint messages join in result order with `\n`, and named selectors match any violated name. Validation joins all error rule messages in result order with `\n`. Joining does not trim or translate text. Messages are not stable identities; match constraint names rather than parsing prose.

Exceptions and authorization configuration failures are **not refusals**. Error-severity framework-composed stream-completion errors, concurrency conflicts, validator failures, unavailable dependencies, malformed requests, and unknown validation reasons also prevent classification, even if the result contains a business refusal. Warning and information results never prevent classification. Unsupported policy outcomes are not authorization refusals. Success and warning- or information-only rule results do not classify as refusals. A warning that blocks a command under its declared severity policy therefore still fails the partition rather than being acknowledged by a validation branch.

Concurrency remains a failure for Chronicle recovery to retry. An unhandled refusal and every non-refusal failure keep the existing failure behavior. Explicit authorization handling is deliberate: `Refused.Any` must not conceal a missing reactor actor. Custom `ICommandSideEffectExecutor` implementations must implement its context-and-event-store overload to support refusal branches.

## Failure and manual execution

An unsuccessful plain returned command becomes a reactor side-effect failure. Chronicle's observer failure/recovery policy then applies; do not assume failure means all previously completed side effects were undone.

Prefer declarative refusal branches for business refusals. When you need other inspection of the `CommandResult`, inject `ICommandPipeline` instead. This method fragment belongs in an `IReactor` with a `commands` dependency and the same event/command types as above:

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

Here throwing deliberately tells the reactor to fail; logging and returning normally would acknowledge the event despite the failed command. [ARCCHR0006](../code-analysis/ARCCHR0006.md) warns when a manual execution handler omits `[OnceOnly]`.

An asynchronous handler can perform manual work and then return supported side effects. Prefer one clear composition style, but mixing them is not a runtime prohibition. Chronicle also supports directly returned **events**; see [React to an event](../react-to-an-event.md).

## Complete the observed stream

Return `CompleteStream` from `Cratis.Arc.Chronicle.Streams` (or `Task<CompleteStream>`) to close a stream without injecting the event log. Completion is scoped to the observed event's event source id and event source type. Omitted stream parts use its stream type and id; redelivery of an already completed stream succeeds. This completion cannot be mixed with events or commands in a returned collection. Return a command when you need final events followed by completion. See [completing streams](../commands/completing-streams.md) for the scoped completion contract, Chronicle kernel 19.39 requirement, and non-atomic append/completion boundary.

## See also

- [Commands](../commands/index.md)
- [Chronicle reactors](/chronicle/reactors/)
- [Transactional commands](../commands/transactional-commands.md)
