```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record BookAdded(string Title, int Copies);

[FromEvent<BookAdded>]
public record BookAvailability(
    [Key] Guid Id,
    string Title,
    [SetFrom<BookAdded>(nameof(BookAdded.Copies))]
    [Decrement<BookReserved>]
    int AvailableCopies);
```
