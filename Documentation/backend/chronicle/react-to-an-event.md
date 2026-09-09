---
title: React to an event
description: Run a side effect or trigger a follow-up command automatically when a Chronicle event is appended — using a reactor.
---

**Goal:** when an event-sourced slice records something — a book is added, an author is registered — you want something _else_ to happen automatically: a notification goes out, another system is told, a follow-up command runs. That's a reactor.

This is part of Arc's Chronicle integration. Direct database-backed Arc slices use commands, queries, and ordinary services; reactors become available when the write side records events in Chronicle.

## A reactor does, a projection shows

A projection builds queryable state; a reactor _acts_. Where you'd reach for a projection to display data, reach for a reactor to cause an effect. `IReactor` is a marker interface — there's nothing to override. Chronicle discovers supported handler signatures by the **type of their first parameter**. Register the integration and event types. For a returned-command reaction, use the supported `Task<TCommand>` signature shown in [command side effects](./reactors/command-side-effects.md).

## Do it

1. **For a side effect, call a collaborator.** Inject whatever does the work and handle the event:

    ```csharp
    public class NewArrivalsAnnouncer(INewArrivalsFeed feed) : IReactor
    {
        [OnceOnly]
        public async Task BookAdded(BookAddedToCatalog @event, EventContext context) =>
            await feed.Announce($"New on the shelf: {@event.Title}");
    }
    ```

2. **To invoke another behavior, execute a command.** Prefer a returned command, or inject `ICommandPipeline` when you need to inspect the result. Chronicle also supports returned events directly; returning them preserves the side-effect pipeline without a manual default-log append:

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

Convert the context's framework identity to `BookId` at the boundary; the command and event retain their domain types. Throwing on an unsuccessful command fails the reactor so Chronicle's failure/recovery policy applies. Logging and returning normally would acknowledge the event despite the failed indexing. A manual command has no inherited HTTP actor; configure authorization deliberately. [Returned-command execution](./reactors/command-side-effects.md#authorization-and-the-actor) explains system roles.

Direct appends to another event sequence or another store remain advanced options when a returned side effect cannot express the target. [ARCCHR0003](./code-analysis/ARCCHR0003.md) documents that boundary; it is analyzer guidance, not a runtime ban on all event-log access.

:::caution[Design for idempotency]
`[OnceOnly]` skips these handlers during replay, not during live retries. A handler without replay exclusion can run on replay too. Make effects safe to repeat and derive decisions from the triggering event rather than assuming an asynchronously materialized model has caught up.
:::

## See also

- [Returning commands as side effects](./reactors/command-side-effects.md) — execute commands as side effects from a reactor.
- [Add event sourcing to an Arc slice](/arc/backend/chronicle/add-event-sourcing/) — where reactors enter the Arc model.
- [Return a result or an error](/arc/scenarios/return-a-result-or-error/) — what the command you execute can return.
