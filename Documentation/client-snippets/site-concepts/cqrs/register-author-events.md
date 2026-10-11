```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record RegisterAuthor(EventSourceId Id, string Name)
{
    public AuthorRegistered Handle() => new(Name);
}

[EventType]
public record AuthorRegistered(string Name);
```
