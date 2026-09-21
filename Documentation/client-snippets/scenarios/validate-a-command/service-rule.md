```csharp
public interface IAuthorsCatalog
{
    Task<bool> IsRegistered(AuthorName name);
}

public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator(IAuthorsCatalog authors)
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c)
            .MustAsync(async (command, ct) => !await authors.IsRegistered(command.Name))
            .WithMessage("An author with that name is already registered.");
    }
}
```
