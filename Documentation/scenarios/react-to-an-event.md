---
title: React to an event
description: Run a side effect or trigger a follow-up command automatically when an event is appended — using a reactor.
---

**Goal:** when something happens — a book is added, an author is registered — you want something *else* to happen automatically: a notification goes out, another system is told, a follow-up command runs. That's a reactor.

## A reactor does, a projection shows

A projection builds queryable state; a reactor *acts*. Where you'd reach for a projection to display data, reach for a reactor to cause an effect. `IReactor` is a marker interface — there's nothing to override. Arc dispatches to a method by the **type of its first parameter**, so adding an event type is all it takes to subscribe.

## Do it

1. **For a side effect, call a collaborator.** Inject whatever does the work and handle the event:

   ```csharp
   public class NewArrivalsAnnouncer(INewArrivalsFeed feed) : IReactor
   {
       public async Task BookAdded(BookAddedToCatalog @event, EventContext context) =>
           await feed.Announce($"New on the shelf: {@event.Title}");
   }
   ```

2. **To change state, execute a command.** A reactor must never touch the event log directly. When a reaction needs to produce new events, inject `ICommandPipeline` and run a command — it goes through validation and `Handle()` like any other:

   ```csharp
   public class CatalogIndexer(ICommandPipeline commands) : IReactor
   {
       public Task BookAdded(BookAddedToCatalog @event, EventContext context) =>
           commands.Execute(new IndexBookForSearch(@event.BookId, @event.Title));
   }
   ```

This is how one slice triggers another without either knowing the other's internals — they meet only at the event.

:::caution[Design for idempotency]
A reactor may be called more than once for the same event — during replay or recovery. Make its effect safe to run twice, and use the event's own data rather than querying a read model that may not have caught up.
:::

## See also

- [Reactors](/chronicle/reactors/) — the full reactor model, dispatch, and failure behavior.
- [Make it live, make it react](/arc/tutorial/real-time/) — a reactor built in context.
- [Return a result or an error](./return-a-result-or-error.md) — what the command you execute can return.
