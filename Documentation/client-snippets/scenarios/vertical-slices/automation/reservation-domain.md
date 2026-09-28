```csharp
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using Library.Members;

namespace Library.Reservations;

// Reservations/ReservationId.cs
public record ReservationId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly ReservationId NotSet = new(Guid.Empty);
    public static ReservationId New() => new(Guid.NewGuid());
    public static implicit operator ReservationId(Guid value) => new(value);
}

// Reservations/ISBN.cs
public record ISBN(string Value) : ConceptAs<string>(Value)
{
    public static readonly ISBN NotSet = new(string.Empty);
    public static implicit operator ISBN(string value) => new(value);
}

// Reservations/ReservationEvents.cs

/// <summary>Records a book held for a member until the collection deadline.</summary>
[EventType]
public record BookReserved(ISBN Isbn, MemberId MemberId, DateTimeOffset ExpiresAt);

/// <summary>Records that a reservation was canceled without collection.</summary>
[EventType]
public record ReservationCancelled(ISBN Isbn, MemberId MemberId);

/// <summary>Records that the member collected the reserved book.</summary>
[EventType]
public record BookBorrowedFromReservation(ISBN Isbn, MemberId MemberId);
```
