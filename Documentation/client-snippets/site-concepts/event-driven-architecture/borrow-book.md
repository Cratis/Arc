```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record BorrowBook(EventSourceId BookId, string MemberEmail, DateOnly DueDate)
{
    public BookBorrowed Handle() => new(MemberEmail, DueDate);
}
```
