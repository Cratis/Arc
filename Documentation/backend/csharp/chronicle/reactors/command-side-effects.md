---
title: Chronicle reactors returning commands as side effects
description: Return commands from Chronicle reactors so Arc validates and authorizes follow-up work; choose replay behavior and handle retries or refusals.
---

Return a command from a Cratis Chronicle reactor to have Cratis Arc validate and authorize follow-up work after an event commits. When a book enters the catalog, for example, the reactor can return a search-indexing command. The handler chooses what happens next; Arc executes the command through its command pipeline.

This behavior belongs to the optional `Cratis.Arc.Chronicle` integration. Before adding the reactor, follow [Arc and Chronicle setup](../cratis-package.md#basic-setup), or its [split-registration setup](../cratis-package.md#running-arc-or-chronicle-on-their-own) if you supply authentication. Register your event types and provide the `ISearchIndex` service used below.

:::note[Reactor side effects are not command operations]
Return a command from the reactor when work should follow a committed event. Inside that command, use [command operations](../../commands/operations/index.md) for immediate service side effects and optional compensation. Return the operation from the command's `Handle()`, not directly from the reactor. Inline command operations do not add durable delivery, replay protection, or exactly-once execution.
:::

## Return a command

The example defines the event, indexing command, and reactor together. Chronicle matches the handler by its first parameter, `BookAddedToCatalog`. The event carries the title; `EventContext` supplies the event source id that identifies the book.

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

`BookId` gives the identifier GUID event-source semantics, while `BookTitle` distinguishes a title from other strings. Convert the framework's event source id to `BookId` at this boundary and reuse the domain types across the book feature.

The `Task<CreateSearchIndex>` return type lets Chronicle discover the handler and pass its result to Arc. `Task.FromResult` wraps the command without starting asynchronous work. On a successful delivery, Arc executes `CreateSearchIndex`, whose `Handle()` calls your `ISearchIndex.Upsert` implementation.

`[OnceOnly]` skips this method during replay, when Chronicle processes recorded events again. It does not prevent retries after a failed delivery. Make `Upsert` idempotent: repeating the same command should leave one intended index entry, not duplicate the change. Choose this behavior for every service the command calls, not just the reactor method.

## Authorization and the actor

Returned commands run in a dedicated service scope with validation and authorization. A reactor is not an HTTP request and does not inherit the original caller's authenticated principal, so a command requiring authentication needs an explicit actor.

To run returned commands as an authenticated system actor, apply `Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute` to the reactor class. Supply only the roles those commands need. Add this attribute to `CatalogIndexer` if its commands require the `catalog-writer` role:

```csharp
[ExecuteCommandsAsSystem("catalog-writer")]
```

The attribute applies to returned command side effects, including service-scope creation and pipeline resolution. Arc keeps the system actor for that asynchronous execution and restores the caller's context afterward, including when setup or command execution fails.

A manual `ICommandPipeline.Execute` call inside the handler is outside this attribute's scope and needs its own execution context. Compliance subjects and event causation describe who data concerns and what caused an event; they are not authorization credentials.

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

Chronicle also discovers synchronous handlers returning `IEnumerable<object>`. Arc runs these commands in order in one service scope and stops at the first unsuccessful result. Each command has its own [transactional scope](../commands/transactional-commands.md), not a transaction covering the collection. If `RemoveFromSearchIndex` fails, the completed archive remains, `NotifySubscribers` does not run, and recovery can run the archive command again.

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

Arc appends all returned events as one batch before running any commands. In this example, `BookIndexingRequested` is recorded before `CreateSearchIndex` runs. Events retain their relative order, as do commands; alternating them in the collection does not alternate their execution.

Bare events use the reactor's resolved event-source, stream, source-type, and subject metadata. `EventForEventSourceId` keeps its explicit destination, tags, named tags, and other append metadata.

If append fails, no command runs. If a command fails, later commands stop, but the event batch and earlier successful commands remain committed. There is no transaction or compensation across events and commands.

A failed command fails the event-source partition. When Chronicle recovers that partition, it redelivers the triggering event and Arc runs the returned commands again, including commands that already succeeded. `[OnceOnly]` does not suppress this recovery: it excludes replay, not retries.

Chronicle protects returned events separately. With a kernel that supports idempotent appends, it derives each event's key from the delivery identity, effect epoch, and position among the returned events. A matching redelivery does not append that event again. Keep the collection stable across deliveries, because reordering events changes their keys. An older kernel appends them without derived keys and Chronicle logs a warning. This protection does not cover the commands' external effects; those must remain safe to repeat. See [delivery identity](/chronicle/reactors/delivery-identity/) for stable delivery keys and effect epochs.

Return an array or list whose contents stay stable. Chronicle's discovery and dispatch can enumerate the sequence more than once; do not produce effects while enumerating it. Each element must be a registered event, an envelope containing a registered event, or a command, never both an event and a command. Nulls, unknown values, and `CompleteStream` are not accepted in the mixed collection. `[ExecuteCommandsAsSystem]` applies only while executing the command partition, not while appending the event partition.

## Handle a command refusal

A business refusal is an expected rejection, such as a validation rule or constraint preventing the command. Return a `CommandInvocation` when that refusal should record a different outcome instead of failing the observer partition. It pairs the command with ordered handlers for the refusals you choose to handle.

This replacement for `CatalogIndexer` reuses `BookAddedToCatalog`, `BookId`, and `CreateSearchIndex` above. Import `Cratis.Arc.Chronicle.Reactors`:

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

Arc chooses the first matching branch in declaration order. The branch returns a registered event, an `EventForEventSourceId`, or a stable nonempty sequence of those. Use `.Acknowledge(selector)` to handle a refusal without producing events; a branch handler returning `null` has the same effect. Acknowledgment treats the refusal as handled, not as proof that the command's work previously completed. Branches cannot return commands, operations, or stream completions.

[ARCCHR0027](../code-analysis/ARCCHR0027.md) warns when a visible `OnRefused` callback returns a command, operation, completion, non-event object, empty batch, or a batch with an invalid member. It checks lambdas, anonymous methods, and method groups whose bodies are available. Opaque helper results still require runtime validation. The runtime validates the whole branch result before appending any of its events; an invalid result fails the delivery, not just the invalid member.

A handled refusal appends its branch events and **stops all remaining commands or invocations** in this delivery. If the branch append fails, the reactor fails; another refusal branch cannot handle that append failure. Bare branch events use the same resolved event-source, stream, source-type, and subject metadata as ordinary returned events. Explicit envelopes retain their metadata.

Return an invocation on its own, `Task<CommandInvocation>`, or a stable sequence of invocations and plain commands. You can also mix them with events: ordinary returned events append as one batch before any command runs, then branch events append when a refusal is handled. Previously appended events and successful commands remain committed on failure or acknowledgment.

Refusal handling does not make the commands safe to retry. Branch events receive idempotency keys when a delivery identity is available and the kernel supports idempotent appends, but commands can run again on redelivery. There is still no transaction across the whole reaction or exactly-once execution guarantee.

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

Use refusal branches for expected business rejections. If you need to inspect other parts of `CommandResult`, inject `ICommandPipeline` and execute the command yourself. This method fragment belongs in an `IReactor` with a `commands` dependency and the same event/command types as above:

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

Throwing tells Chronicle that the reaction failed. Logging and returning normally would acknowledge the event despite the failed command. [ARCCHR0006](../code-analysis/ARCCHR0006.md) warns when manual command execution has no explicit replay decision. Use `[OnceOnly]` to skip replay, or define a `[Replay]` handler when replay needs its own behavior.

An asynchronous handler can do manual work and then return supported side effects. Prefer one composition style so the failure path stays clear; the runtime does allow both. If the next step only needs to record a fact, Chronicle also accepts returned events. [React to an event](../react-to-an-event.md) explains the choice.

## Complete the observed stream

Return `CompleteStream` from `Cratis.Arc.Chronicle.Streams` (or `Task<CompleteStream>`) to close a stream without injecting the event log. Completion is scoped to the observed event's event source id and event source type. Omitted stream parts use its stream type and id; redelivery of an already completed stream succeeds. This completion cannot be mixed with events or commands in a returned collection. Return a command when you need final events followed by completion. See [completing streams](../commands/completing-streams.md) for the scoped completion contract, Chronicle kernel 19.39 requirement, and non-atomic append/completion boundary.

## See also

- [Commands](../commands/index.md)
- [Chronicle reactors](/chronicle/reactors/)
- [Transactional commands](../commands/transactional-commands.md)
- [Commands for occurrences](../commands/occurrence-commands.md) — retry-safe commands for timers, startup and shutdown, which are not reactor side effects
