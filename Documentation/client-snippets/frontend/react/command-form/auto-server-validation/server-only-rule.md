```csharp
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using FluentValidation;

public interface IProfileDirectory
{
    Task<bool> IsEmailAllowed(EmailAddress email, CancellationToken cancellationToken);
}

public class UpdateProfileValidator : CommandValidator<UpdateProfile>
{
    public UpdateProfileValidator(IProfileDirectory profiles)
    {
        RuleFor(command => command.Name)
            .NotEmpty().MinimumLength(3).MaximumLength(100);
        RuleFor(command => command.Email)
            .MustAsync((command, _, cancellationToken) => profiles.IsEmailAllowed(command.Email, cancellationToken))
            .WithMessage("This email cannot be used for this profile.");
    }
}
```
