```csharp
public class RegisterAuthorValidator : CommandValidator<RegisterAuthor>
{
    public RegisterAuthorValidator() =>
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
}
```
