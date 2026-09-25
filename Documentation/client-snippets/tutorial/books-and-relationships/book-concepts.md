```csharp
public record BookId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly BookId NotSet = new(Guid.Empty);

    public static BookId New() => new(Guid.NewGuid());
}

public record BookTitle(string Value) : ConceptAs<string>(Value)
{
    public static readonly BookTitle NotSet = new(string.Empty);

    public static implicit operator BookTitle(string value) => new(value);
}
```
