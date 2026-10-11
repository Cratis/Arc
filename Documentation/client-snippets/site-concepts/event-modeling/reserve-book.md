```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record ReserveBook(EventSourceId BookId, string Member)
{
    public BookReserved Handle() => new(Member);
}

[EventType]
public record BookReserved(string Member);
```
