```csharp
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using FluentValidation;

namespace Library.Authors;

// Authors/AuthorId.cs
public record AuthorId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly AuthorId NotSet = new(Guid.Empty);
    public static AuthorId New() => new(Guid.NewGuid());
    public static implicit operator AuthorId(Guid value) => new(value);
}

// Authors/AuthorName.cs
public record AuthorName(string Value) : ConceptAs<string>(Value)
{
    public static readonly AuthorName NotSet = new(string.Empty);
    public static implicit operator AuthorName(string value) => new(value);
}

public class AuthorNameValidator : ConceptValidator<AuthorName>
{
    public AuthorNameValidator() => RuleFor(name => name.Value).NotEmpty();
}
```
