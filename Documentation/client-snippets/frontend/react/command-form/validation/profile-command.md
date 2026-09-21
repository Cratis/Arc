```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace MyApp.Profiles;

public record ProfileName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ProfileName NotSet = new(string.Empty);
}

public record EmailAddress(string Value) : ConceptAs<string>(Value)
{
    public static readonly EmailAddress NotSet = new(string.Empty);
}

public class EmailAddressValidator : ConceptValidator<EmailAddress>
{
    public EmailAddressValidator() => RuleFor(email => email.Value).NotEmpty().EmailAddress();
}

[Command]
public record UpdateProfile(ProfileName Name, EmailAddress Email)
{
    public ProfileDetails Handle() => new(Name, Email);
}

public record ProfileDetails(ProfileName Name, EmailAddress Email);

public class UpdateProfileValidator : CommandValidator<UpdateProfile>
{
    public UpdateProfileValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MinimumLength(3).MaximumLength(100);
    }
}
```
