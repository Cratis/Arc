// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Testing.Validation.for_ValidatorShouldExtensions.given;

public class an_asynchronous_item_validator : AbstractValidator<AddItem>
{
    public an_asynchronous_item_validator() => RuleFor(_ => _.Name)
        .MustAsync((name, cancellationToken) => Task.FromResult(name != "reserved"))
        .WithMessage("Name is reserved");
}
