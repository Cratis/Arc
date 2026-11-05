---
title: React to Cratis Chronicle events from Arc
description: Use a reactor in Arc's Chronicle integration to send notifications or execute follow-up commands after recorded events, with replay and retry limits.
---

A reactor handles recorded Cratis Chronicle events so you can send a notification or start follow-up work outside the original command. For example, after a book enters the catalog, a reactor can announce its arrival or execute a command to update its search index.

Use this path when your Arc slice records events in Chronicle. Direct database-backed slices can use commands and ordinary services without adding event sourcing. To migrate an existing slice first, follow [Add event sourcing to an Arc slice](/arc/backend/csharp/chronicle/add-event-sourcing/).

## A reactor does, a projection shows

Use a projection to build state for queries and a reactor to perform follow-up work. `IReactor` is a marker interface with no methods to override. Chronicle recognizes a handler by its supported signature and the event type of its first parameter.

Before adding a handler, register the [Chronicle integration](./cratis-package.md) and its event types, and configure the client connection to a running server. The fragments below assume that setup. For a reactor that returns a command, use the `Task<TCommand>` signature in [command side effects](./reactors/command-side-effects.md).

## Do it

1. To send a notification, inject the service that delivers it and handle the event:

    ```csharp
    public class NewArrivalsAnnouncer(INewArrivalsFeed feed) : IReactor
    {
        [OnceOnly]
        public async Task BookAdded(BookAddedToCatalog @event, EventContext context) =>
            await feed.Announce($"New on the shelf: {@event.Title}");
    }
    ```

2. To invoke application behavior, prefer returning a command. Inject `ICommandPipeline` when you need to inspect its result, as this indexing handler does:

    ```csharp
    public class CatalogIndexer(ICommandPipeline commands) : IReactor
    {
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
    }
    ```

These reactor fragments reuse `BookId`, `BookTitle`, `BookAddedToCatalog`, `CreateSearchIndex`, and the named `SearchIndexingFailed` exception from [command side effects](./reactors/command-side-effects.md). Place them in the same `Catalog.Books` namespace and supply `INewArrivalsFeed` for the first example. Import `System`, `System.Threading.Tasks`, `Cratis.Arc.Commands`, `Cratis.Chronicle.Events`, and `Cratis.Chronicle.Reactors` as needed.

The triggering event's context supplies the book's event source id. Convert it to `BookId` at that boundary so the follow-up command keeps its domain identity. Throwing when the command is unsuccessful fails the reactor and lets Chronicle's recovery policy apply. Logging the failure and returning normally would acknowledge the event even though indexing failed.

A manual command call does not inherit the original HTTP caller's identity. Configure its authorization context deliberately. [Returned-command execution](./reactors/command-side-effects.md#authorization-and-the-actor) explains how to select system roles when the reactor returns a command instead.

Chronicle also supports returned events, preserving the side-effect pipeline without a manual default-log append. To record events and invoke commands in the same reaction, [return a stable mixed collection](./reactors/command-side-effects.md#return-events-and-commands-together). Arc appends one event batch before executing commands in order. An append failure prevents every command; a later command failure does not undo the event batch or earlier commands. Use `EventForEventSourceId` for explicit destinations. A retry does not append the events twice, but it does run the commands again, so keep commands safe to repeat.

Direct appends to another event sequence or another store remain advanced options when a returned side effect cannot express the target. For the **default log**, return events unless an ordered workflow genuinely needs an append result, an observer-completion wait, or append options before it can proceed. In that case, [ARCCHR0003's ordered manual append example](./code-analysis/ARCCHR0003.md#ordered-manual-appends) shows a narrowly scoped `#pragma warning disable ARCCHR0003` with a reason and a matching restore. The warning is guidance, not a runtime ban or a reason to silence the rule across a project.

Manual appends do not appear in `ReactorScenario<T>.Produced` or its `ShouldHaveProduced<T>()` assertions. Test the append outcomes and ordering separately. Suppressing the warning provides neither replay protection nor a transaction across those appends: choose replay behavior, handle failures, and make retries safe.

:::caution[Design for idempotency]
`[OnceOnly]` skips these handlers during replay, not during live retries. A handler without replay exclusion can run on replay too. Make effects safe to repeat and derive decisions from the triggering event rather than assuming an asynchronously materialized model has caught up.
:::

## See also

- [Returning commands as side effects](./reactors/command-side-effects.md) — execute commands as side effects from a reactor.
- [Add event sourcing to an Arc slice](/arc/backend/csharp/chronicle/add-event-sourcing/) — where reactors enter the Arc model.
- [Return a result or an error](/arc/scenarios/return-a-result-or-error/) — what the command you execute can return.
