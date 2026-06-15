// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators;

#pragma warning disable SA1649, SA1402

/// <summary>
/// A command whose validator depends on an injected, provider-resolved collaborator. This stands in for a
/// Chronicle read model that is resolved for the command's event source id — the value differs per service
/// provider/scope, which is exactly what the validator resolution must honor.
/// </summary>
/// <param name="Id">The identifier of the command.</param>
public record CommandWithDependency(Guid Id);

/// <summary>
/// A collaborator for <see cref="CommandWithDependencyValidator"/> whose value is determined by the service
/// provider it is resolved from — mimicking a command-scoped read model.
/// </summary>
public class CommandDependency
{
    /// <summary>
    /// Gets a value indicating whether the dependency allows the command to validate.
    /// </summary>
    public bool IsAllowed { get; init; }
}

/// <summary>
/// Validator for <see cref="CommandWithDependency"/> that only passes when its injected
/// <see cref="CommandDependency"/> allows it.
/// </summary>
public class CommandWithDependencyValidator : CommandValidator<CommandWithDependency>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandWithDependencyValidator"/> class.
    /// </summary>
    /// <param name="dependency">The provider-resolved <see cref="CommandDependency"/>.</param>
    public CommandWithDependencyValidator(CommandDependency dependency) =>
        RuleFor(command => command.Id)
            .Must(_ => dependency.IsAllowed)
            .WithMessage("The dependency does not allow this command.");
}

#pragma warning restore SA1649, SA1402
