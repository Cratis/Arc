```csharp
public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator(LibraryDbContext db)
    {
        RuleFor(c => c.Name)
            .MustAsync(async (name, ct) => !await db.Authors.AnyAsync(a => a.Name == name, ct))
            .WithMessage("An author with that name is already registered.");
    }
}
```
