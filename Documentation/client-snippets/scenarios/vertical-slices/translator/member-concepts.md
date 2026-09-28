```csharp
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Concepts;
using FluentValidation;

namespace Library.Members;

// Members/MemberId.cs
public record MemberId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static readonly MemberId NotSet = new(Guid.Empty);
    public static MemberId New() => new(Guid.NewGuid());
    public static implicit operator MemberId(Guid value) => new(value);
}

// Members/MemberName.cs
public record MemberName(string Value) : ConceptAs<string>(Value)
{
    public static readonly MemberName NotSet = new(string.Empty);
    public static implicit operator MemberName(string value) => new(value);
}

public class MemberNameValidator : ConceptValidator<MemberName>
{
    public MemberNameValidator() => RuleFor(name => name.Value).NotEmpty();
}
```
