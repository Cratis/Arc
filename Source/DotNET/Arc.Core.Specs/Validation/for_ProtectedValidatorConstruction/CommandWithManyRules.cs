// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ProtectedValidatorConstruction;

#pragma warning disable SA1649, SA1402

/// <summary>
/// A command whose validator has more rules than a snapshot starts out with room for.
/// </summary>
/// <param name="Name">The name to validate.</param>
public record CommandWithManyRules(string Name);

/// <summary>
/// Validator for <see cref="CommandWithManyRules"/> with many rules, each with several components.
/// </summary>
public class CommandWithManyRulesValidator : CommandValidator<CommandWithManyRules>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandWithManyRulesValidator"/> class.
    /// </summary>
    public CommandWithManyRulesValidator()
    {
        for (var rule = 0; rule < 12; rule++)
        {
            RuleFor(command => command.Name).NotNull().NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>
    /// Adds a rule after construction.
    /// </summary>
    public void AddRule() => RuleFor(command => command.Name).NotEqual("denied");
}

#pragma warning restore SA1649, SA1402
