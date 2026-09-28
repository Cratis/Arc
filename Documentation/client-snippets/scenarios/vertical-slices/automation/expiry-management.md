```csharp
// Reservations/ExpiryManagement/ExpiryManagement.cs
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reactors;
using MongoDB.Driver;
using Library.Members;
using Library.Reservations;

namespace Library.Reservations.ExpiryManagement;

// ─── Read Model ───────────────────────────────────────────────────────────────

[ReadModel]
[FromEvent<BookReserved>]
[RemovedWith<BookBorrowedFromReservation>]
[RemovedWith<ReservationCancelled>]
[RemovedWith<ReservationExpired>]
public record ReservationDueForExpiry(ReservationId Id, DateTimeOffset ExpiresAt);

[ReadModel]
[Passive]
[FromEvent<BookReserved>]
[RemovedWith<BookBorrowedFromReservation>]
[RemovedWith<ReservationCancelled>]
[RemovedWith<ReservationExpired>]
public record PendingReservation(
    ReservationId Id,
    ISBN Isbn,
    MemberId MemberId,
    DateTimeOffset ExpiresAt);

// ─── Event ────────────────────────────────────────────────────────────────────

/// <summary>Records the expiry of a reservation that was not collected in time.</summary>
[EventType]
public record ReservationExpired(ISBN Isbn, MemberId MemberId);

/// <summary>Records the scheduler's daily opportunity to check overdue reservations.</summary>
[EventType]
public record DailyTick(DateTimeOffset OccurredAt);

// ─── Command ──────────────────────────────────────────────────────────────────

[Command]
public record CancelExpiredReservation(ReservationId ReservationId)
{
    public DateTimeOffset Provide() => DateTimeOffset.UtcNow;

    public ReservationExpired? Handle(PendingReservation? reservation, DateTimeOffset now)
    {
        if (reservation is null || reservation.ExpiresAt > now)
        {
            return null;
        }

        return new ReservationExpired(reservation.Isbn, reservation.MemberId);
    }
}

// ─── Reactor ──────────────────────────────────────────────────────────────────

public class ReservationExpiryReactor(
    IMongoCollection<ReservationDueForExpiry> reservations,
    ICommandPipeline commandPipeline) : IReactor
{
    [OnceOnly]
    public async Task HandleDailyTick(DailyTick @event)
    {
        var expired = await reservations
            .Find(reservation => reservation.ExpiresAt <= @event.OccurredAt)
            .ToListAsync();

        foreach (var reservation in expired)
        {
            var result = await commandPipeline.Execute(
                new CancelExpiredReservation(reservation.Id));
            if (!result.IsSuccess)
            {
                throw new ReservationExpiryFailed(reservation.Id);
            }
        }
    }
}

public class ReservationExpiryFailed(ReservationId reservationId)
    : Exception($"Could not expire reservation '{reservationId}'.");
```
