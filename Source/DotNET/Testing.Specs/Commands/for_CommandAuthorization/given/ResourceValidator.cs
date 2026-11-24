// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

public class ResourceValidator : CommandValidator<ResourceCommand>
{
    public ResourceValidator(PolicyBehavior behavior) => RuleFor(_ => _).Custom((_, _) => behavior.ValidatorCalls++);
}
