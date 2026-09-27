// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_executing_command;

#pragma warning disable SA1649, SA1402

/// <summary>
/// A command validated against its operation receipt year.
/// </summary>
/// <param name="ExpectedYear">The expected year.</param>
public record CommandWithReceipt(int ExpectedYear);

/// <summary>
/// Records the receipt observed by a validator resolved from the command scope.
/// </summary>
public class ReceiptObservation
{
    /// <summary>Gets or sets the observed receipt.</summary>
    public DateTimeOffset? ReceivedAt { get; set; }
}

/// <summary>
/// Validates a command with the injectable operation context accessor.
/// </summary>
public class CommandWithReceiptValidator : CommandValidator<CommandWithReceipt>
{
    /// <summary>Initializes the rule.</summary>
    /// <param name="accessor">The operation context accessor.</param>
    /// <param name="observation">The observation made during validation.</param>
    public CommandWithReceiptValidator(IOperationContextAccessor accessor, ReceiptObservation observation)
    {
        RuleFor(command => command.ExpectedYear).Must(year =>
        {
            observation.ReceivedAt = accessor.ReceivedAt;
            return observation.ReceivedAt?.Year == year;
        });
    }
}

#pragma warning restore SA1649, SA1402
