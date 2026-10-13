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
    /// Gets the first violated constraint name, or null when no names are available.
    /// </summary>
    public string? ConstraintName => ConstraintNames.Count > 0 ? ConstraintNames[0] : null;

    /// <summary>
    /// Gets all violated constraint names in result order, without duplicates.
    /// </summary>
    public IReadOnlyList<string> ConstraintNames { get; init; } = ConstraintName is null ? [] : [ConstraintName];

    /// <summary>
    /// Compares refusals by their kind, authored message and ordered constraint names.
    /// </summary>
    /// <param name="other">The refusal to compare.</param>
    /// <returns>Whether both refusals describe the same outcome.</returns>
    public virtual bool Equals(CommandRefusal? other)
    {
        if (other is null)
        {
            return false;
        }

        return EqualityContract == other.EqualityContract && Kind == other.Kind &&
            Message == other.Message && ConstraintNames.SequenceEqual(other.ConstraintNames);
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
        ConstraintNames.Aggregate(HashCode.Combine(EqualityContract, Kind, Message), HashCode.Combine);
}
