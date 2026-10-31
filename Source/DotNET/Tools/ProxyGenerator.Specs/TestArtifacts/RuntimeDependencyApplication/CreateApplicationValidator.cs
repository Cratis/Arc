// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication;

public class CreateApplicationValidator : CommandValidator<CreateApplication>
{
    public CreateApplicationValidator() => RuleFor(c => c.Name).NotEmpty().WithMessage("Application name is required").MaximumLength(64);
}
