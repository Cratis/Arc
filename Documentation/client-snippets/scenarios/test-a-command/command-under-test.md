```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Library.Authors;

public interface IAuthorRegistration
{
    Task Register(AuthorId id, AuthorName name);
}

[Command]
public record RecordAuthor(AuthorId Id, AuthorName Name)
{
    public Task Handle(IAuthorRegistration registration) => registration.Register(Id, Name);
}

public class RecordAuthorValidator : CommandValidator<RecordAuthor>
{
    public RecordAuthorValidator() => RuleFor(command => command.Name.Value).NotEmpty();
}
```
