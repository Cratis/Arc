```csharp
public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);
    public static implicit operator AuthorId(Guid value) => new(value);
    public static AuthorId New() => new(Guid.NewGuid());
}
```
