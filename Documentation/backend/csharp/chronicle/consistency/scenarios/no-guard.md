---
title: Rules that need no guard
description: When a rare violation is acceptable, a hint is enough, the data is external, or the key has a long history, and what to do instead of a guard.
---

Use this page before you add a guard to a rule that does not need one. Each section says what you give up.

## A rare violation you can repair

| | |
| --- | --- |
| Rule | A flight may be overbooked briefly. A reactor cancels the reservations that exceed the capacity |
| Mechanism | The default append, and a reactor that appends a correcting event |
| What you give up | Between the violation and the repair the data is wrong. A repair cannot undo an irreversible side effect |

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record SeatReserved(Guid ReservationId);

[EventType]
public record ReservationCancelled(Guid ReservationId, string Reason);

public class OverbookingRepair(IEventStore eventStore) : IReactor
{
    const int Capacity = 100;

    [OnceOnly]
    public async Task<ReservationCancelled?> Reserved(SeatReserved @event, EventContext context)
    {
        var events = await eventStore.EventLog.GetFromSequenceNumber(
            EventSequenceNumber.First,
            context.EventSourceId);
        var reservedUpToThis = events.Count(_ =>
            _.Context.SequenceNumber <= context.SequenceNumber && _.Content is SeatReserved);

        return reservedUpToThis > Capacity
            ? new ReservationCancelled(@event.ReservationId, "The flight was overbooked.")
            : null;
    }
}
```

Returning `null` records nothing. `[OnceOnly]` keeps the repair out of replays. It does not make delivery exactly-once, so make the correction safe to repeat. A reactor is not a command, so it is not under read protection. Its own reads go through `IEventStore` and are not refused.

## A hint

| | |
| --- | --- |
| Rule | Show "no copies left" before the user submits |
| Mechanism | A query, or a validator over a read marked `[Unprotected("why")]` |
| What you give up | The hint can be stale. The rule is enforced elsewhere |

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

public record Isbn(string Value) : EventSourceId(Value);

[EventType]
public record BookReserved();

public record Reservations(
    EventSourceId Id,
    [Increment<BookReserved>] int Count);

[Command]
public record ReserveBook(Isbn Isbn)
{
    public BookReserved Handle() => new();
}

public class ReserveBookValidator : CommandValidator<ReserveBook>
{
    public ReserveBookValidator([Unprotected("hint only; the reservation limit is a constraint")] Reservations? reservations) =>
        RuleFor(command => command.Isbn)
            .Must(_ => reservations is not { Count: >= 10 })
            .WithMessage("This book has too many reservations.");
}
```

The reason is the part a reviewer reads. State what holds the rule instead.

## External data

| | |
| --- | --- |
| Rule | A purchase uses the current price, and a book can be bought only while a copy is available |
| Mechanism | `Provide()` fetches the price. A read model parameter guards your own state. The price goes into the event |
| What you give up | The external value is not guarded. The event records what the decision used |

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

public interface IPriceList
{
    Task<decimal> PriceOf(EventSourceId bookId);
}

public record QuotedPrice(decimal Amount);

public record BookId(string Value) : EventSourceId(Value);

[EventType]
public record CopyStocked();

[EventType]
public record CopySold(decimal Price);

public record CopiesInStock(
    EventSourceId Id,
    [Increment<CopyStocked>, Decrement<CopySold>] int Available);

[Command]
public record SellCopy(BookId Book)
{
    public async Task<QuotedPrice> Provide(IPriceList prices) => new(await prices.PriceOf(Book));

    public CopySold Handle(QuotedPrice price) => new(price.Amount);
}

public class SellCopyValidator : CommandValidator<SellCopy>
{
    public SellCopyValidator(CopiesInStock? stock) =>
        RuleFor(command => command.Book)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies are left.");
}
```

`IPriceList` is an unknown dependency. Read protection guards the stock and nothing about the price.

## A hot key with a long history

| | |
| --- | --- |
| Rule | A room accepts at most 20 bookings a month |
| Mechanism | Key the model by the period that bounds the rule, not by the room |
| What you give up | Nothing. A guarded read folds the key's history every time, so keep it short |

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

[EventType]
public record RoomBooked(Guid RoomId, DateOnly Day);

public record RoomMonth(EventSourceId Id, [Increment<RoomBooked>] int Bookings);

[Command]
public record BookRoom(Guid RoomId, DateOnly Day) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => $"{RoomId:D}:{Day:yyyy-MM}";

    public RoomBooked Handle() => new(RoomId, Day);
}

public class BookRoomValidator : CommandValidator<BookRoom>
{
    public const int MaximumBookingsPerMonth = 20;

    public BookRoomValidator(RoomMonth? month) =>
        RuleFor(command => command.Day)
            .Must(_ => month is not { Bookings: >= MaximumBookingsPerMonth })
            .WithMessage("The room is fully booked this month.");
}
```

Each month is a new event source, so each decision folds at most one month of bookings. The key must not be blank or `*`, must be trimmed, and must not contain `#`.

## Related

- [Reads](../reads.md#declare-an-advisory-read)
- [Pitfalls](../pitfalls.md)
