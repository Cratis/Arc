// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableValidators;

#pragma warning disable SA1649, SA1402

/// <summary>
/// A command whose validator takes its collaborator as a nullable dependency.
/// </summary>
/// <param name="Id">The identifier of the command.</param>
public record CommandWithNullableDependency(Guid Id);

/// <summary>
/// Validator for <see cref="CommandWithNullableDependency"/>.
/// </summary>
public class CommandWithNullableDependencyValidator : CommandValidator<CommandWithNullableDependency>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandWithNullableDependencyValidator"/> class.
    /// </summary>
    /// <param name="dependency">The provider-resolved <see cref="CommandDependency"/>.</param>
    public CommandWithNullableDependencyValidator(CommandDependency? dependency) =>
        RuleFor(command => command.Id)
            .Must(_ => dependency is not null)
            .WithMessage("The dependency does not exist.");
}

#pragma warning restore SA1649, SA1402
