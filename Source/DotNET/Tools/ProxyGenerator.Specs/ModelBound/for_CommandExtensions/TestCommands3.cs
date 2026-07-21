// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions.TestTypes.Invitations;

public record ContactEmailAddress(string Value) : ConceptAs<string>(Value);

public class ContactEmailAddressValidator : ConceptValidator<ContactEmailAddress>
{
    public ContactEmailAddressValidator() => RuleFor(x => x.Value).NotEmpty().EmailAddress();
}

// The value Provide() resolves and Handle() consumes - it carries a validated concept, but it is not the command,
// so its rules must not surface on the command's generated validator.
public record InvitationTarget(ContactEmailAddress Email);

[Command]
public class InviteContact
{
    public string OrganizationNumber { get; set; } = string.Empty;

    public InvitationTarget Provide() => new(new ContactEmailAddress(string.Empty));

    public void Handle(InvitationTarget target)
    {
    }
}
