```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

namespace Library.Authors;

[Command]
public record RegisterAuthor(EventSourceId AuthorId, string Name)
{
    public AuthorRegistered Handle() => new(Name);
}

[EventType]
public record AuthorRegistered(string Name);
```
