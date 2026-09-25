```csharp
public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator(IMongoCollection<Author> authors)
    {
        RuleFor(c => c.Name)
            .MustAsync(async (name, _) => !await authors.Find(a => a.Name == name).AnyAsync())
            .WithMessage("An author with that name is already registered.");
    }
}
```
