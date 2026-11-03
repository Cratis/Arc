// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.given;

public class an_item_validator : CommandValidator<AddItem>
{
    public an_item_validator()
    {
        RuleFor(_ => _.Name).NotEmpty().WithMessage("Name is required");
        WhenCommand(() => RuleFor(_ => _.Name).NotEqual("command-only").WithMessage("Command name is reserved"));
        RuleFor(_ => _.Quantity).GreaterThan(0).WithMessage("Quantity must be positive").WithSeverity(Severity.Warning);
    }
}
