// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Describes the command refusal kinds and optional constraint name selected by an ordered invocation branch.
/// </summary>
public sealed record Refused
{
    Refused(IEnumerable<CommandRefusalKind> kinds, string? constraintName = null)
    {
        Kinds = Array.AsReadOnly(kinds.Distinct().Order().ToArray());
        ConstraintName = constraintName;
    }

    /// <summary>
    /// Gets the refusal kinds matched by this selector.
    /// </summary>
    public IReadOnlyList<CommandRefusalKind> Kinds { get; }

    /// <summary>
    /// Gets the exact constraint name to match, or null for no name restriction.
    /// </summary>
    public string? ConstraintName { get; }

    /// <summary>
    /// Gets the selector for authored validation refusals.
    /// </summary>
    public static Refused ByValidation { get; } = new([CommandRefusalKind.Validation]);

    /// <summary>
    /// Gets the selector for actual authorization denials.
    /// </summary>
    public static Refused ByAuthorization { get; } = new([CommandRefusalKind.Authorization]);

    /// <summary>
    /// Gets the selector for validation and constraint refusals. Excludes authorization, like bare on refused in Screenplay.
    /// </summary>
    public static Refused Any { get; } = new([CommandRefusalKind.Validation, CommandRefusalKind.Constraint]);

    /// <summary>
    /// Gets the selector for any constraint refusal, including one without a name.
    /// </summary>
    public static Refused ByAnyConstraint { get; } = new([CommandRefusalKind.Constraint]);

    /// <summary>
    /// Selects a constraint refusal by its exact, case-sensitive name among all violated names.
    /// </summary>
    /// <param name="name">The required constraint name.</param>
    /// <returns>The named constraint selector.</returns>
    public static Refused ByConstraint(string name) => new([CommandRefusalKind.Constraint], name);

    /// <summary>
    /// Compares selectors by their matched kinds and constraint name.
    /// </summary>
    /// <param name="other">The selector to compare.</param>
    /// <returns>Whether both selectors describe the same refusal selection.</returns>
    public bool Equals(Refused? other)
    {
        if (other is null)
        {
            return false;
        }

        return ConstraintName == other.ConstraintName && Kinds.SequenceEqual(other.Kinds);
    }

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Kinds.Aggregate(ConstraintName?.GetHashCode(StringComparison.Ordinal) ?? 0, HashCode.Combine);

    /// <summary>
    /// Tests whether the refusal satisfies this selector's kinds and optional name.
    /// </summary>
    /// <param name="refusal">The classified refusal.</param>
    /// <returns>Whether the selector matches.</returns>
    internal bool Matches(CommandRefusal refusal)
    {
        if (!Kinds.Contains(refusal.Kind))
        {
            return false;
        }

        return ConstraintName is null || refusal.ConstraintNames.Contains(ConstraintName);
    }
}
