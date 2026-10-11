```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

[EventType]
public record BookBorrowed(string MemberEmail, DateOnly DueDate);

public class LoanConfirmationReactor : IReactor
{
    public Task Borrowed(BookBorrowed @event, EventContext context) =>
        SendConfirmationAsync(@event.MemberEmail, @event.DueDate);

    Task SendConfirmationAsync(string email, DateOnly dueDate) => Task.CompletedTask;
}
```
