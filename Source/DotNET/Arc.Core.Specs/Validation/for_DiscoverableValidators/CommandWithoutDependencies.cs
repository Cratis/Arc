// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators;

#pragma warning disable SA1649, SA1402

/// <summary>
/// A command whose validator only validates the command's own input.
/// </summary>
/// <param name="Name">The name to validate.</param>
public record CommandWithoutDependencies(string Name);

/// <summary>
/// Validator for <see cref="CommandWithoutDependencies"/> with a single parameterless constructor.
/// </summary>
public class CommandWithoutDependenciesValidator : CommandValidator<CommandWithoutDependencies>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandWithoutDependenciesValidator"/> class.
    /// </summary>
    public CommandWithoutDependenciesValidator() => RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required.");

    /// <summary>
    /// Adds a rule after construction, the way a registration factory or decorator could.
    /// </summary>
    public void AddDenyingRule() => RuleFor(command => command.Name).Must(_ => false).WithMessage("Denied.");
}

#pragma warning restore SA1649, SA1402
