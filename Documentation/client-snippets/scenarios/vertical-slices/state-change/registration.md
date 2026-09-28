```csharp
// Authors/Registration/Registration.cs
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using FluentValidation;

namespace Library.Authors.Registration;

/// <summary>Records an author's registration with their first and last names.</summary>
[EventType]
public record AuthorRegistered(AuthorName FirstName, AuthorName LastName);

public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator()
    {
        RuleFor(c => c.FirstName)
            .NotEmpty().WithMessage("First name is required");

        RuleFor(c => c.LastName)
            .NotEmpty().WithMessage("Last name is required");
    }
}

[Command]
public record RegisterAuthor(AuthorName FirstName, AuthorName LastName)
{
    public AuthorId Provide() => AuthorId.New();

    public (AuthorId, AuthorRegistered) Handle(AuthorId authorId) =>
        (authorId, new AuthorRegistered(FirstName, LastName));
}
```
