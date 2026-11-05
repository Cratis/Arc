---
title: Arc consistency without guarding every read
description: Leave hints and external values unguarded, repair acceptable violations, or shorten event histories while keeping the guard on a business rule.
---

Cratis Arc consistency guards protect state that must remain valid until commit. Hints may be stale, external values cannot be guarded by Cratis Chronicle, and some violations can be repaired later. A long history is a different problem: shorten the history you read while keeping the rule guarded.

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
        if (events.Any(_ => _.Content is ReservationCancelled cancelled && cancelled.ReservationId == @event.ReservationId))
        {
            return null;
        }

        var upToThis = events.Where(_ => _.Context.SequenceNumber <= context.SequenceNumber).ToList();
        var active = upToThis.Count(_ => _.Content is SeatReserved) - upToThis.Count(_ => _.Content is ReservationCancelled);

        return active > Capacity
            ? new ReservationCancelled(@event.ReservationId, "The flight was overbooked.")
            : null;
    }
}
```

Returning `null` records nothing. `[OnceOnly]` keeps the repair out of replays. It does not make delivery exactly-once, so the reactor skips a reservation it already cancelled, and it counts cancellations so a seat freed earlier is not held against a later reservation. A reactor is not a command, so it is not under read protection. Its own reads go through `IEventStore` and are not refused.

## A hint

| | |
| --- | --- |
| Rule | Warn before submitting a reservation when the book has ten reservations |
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

The `[Unprotected]` reason tells a reviewer where the real rule is enforced. This excerpt assumes the reservation-limit constraint is configured separately; the advisory validator alone does not enforce the limit under concurrency.

## External data

| | |
| --- | --- |
| Rule | Record the quoted price, and sell a book only while a copy is available |
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

Read protection cannot inspect the `IPriceList` dependency. Under `On`, the stock read is guarded, but the price can change after `Provide()` returns. `CopySold` records the quote the command used.

## A hot key with a long history

| | |
| --- | --- |
| Rule | A room accepts at most 20 bookings a month |
| Mechanism | Key the model by the period that bounds the rule, not by the room |
| What changes | The room's history is split across monthly event sources; the monthly limit stays guarded |

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

Each room-month is a separate event source, so a guarded decision folds only that month's bookings for the room. Use read protection `On` to guard the validator's `RoomMonth` read. The key must not be blank or `*`, must be trimmed, and must not contain `#`.

## Related

- [Reads](../reads.md#declare-an-advisory-read)
- [Pitfalls](../pitfalls.md)
