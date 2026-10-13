// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Describes why a command was refused without treating retryable or infrastructure failures as refusals.
/// </summary>
/// <param name="Kind">The refusal category.</param>
/// <param name="ConstraintName">The violated constraint name, when available for a constraint refusal.</param>
/// <param name="Message">The authored refusal messages, verbatim and joined with a newline.</param>
public record CommandRefusal(CommandRefusalKind Kind, string? ConstraintName, string Message)
{
    /// <summary>
    /// Gets all violated constraint names in result order, without duplicates.
    /// </summary>
    public IReadOnlyList<string> ConstraintNames { get; init; } = ConstraintName is null ? [] : [ConstraintName];
}
