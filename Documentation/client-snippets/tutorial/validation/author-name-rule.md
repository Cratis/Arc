```csharp
public class AuthorNameValidator : ConceptValidator<AuthorName>
{
    public AuthorNameValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("An author needs a name.");
    }
}
```
