// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Specs.AspNetCoreDiscovery;

public class AspNetCoreDiscoveryCommandValidator : CommandValidator<AspNetCoreDiscoveryCommand>
{
    public AspNetCoreDiscoveryCommandValidator() => RuleFor(command => command.Value).NotEmpty().WithMessage("Value is required");
}
