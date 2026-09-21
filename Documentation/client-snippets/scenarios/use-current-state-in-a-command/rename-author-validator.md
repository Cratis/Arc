```csharp
public class RenameAuthorValidator : CommandValidator<RenameAuthor>
{
    public RenameAuthorValidator(Author? author)
    {
        RuleFor(command => command.Id)
            .Must(_ => author is not null)
            .WithMessage("Author does not exist.");
        When(_ => author is not null, () =>
            RuleFor(command => command.NewName)
                .Must(name => name != author!.Name)
                .WithMessage("Choose a different name."));
    }
}
```
