// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class CommandWithValidation
{
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CommandWithValidationValidator : BaseValidator<CommandWithValidation>
{
    public CommandWithValidationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(50);
    }
}
