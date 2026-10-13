// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Selects a command refusal for an ordered invocation branch.
/// </summary>
public sealed class Refused
{
    readonly Func<CommandRefusal, bool> _matches;

    Refused(Func<CommandRefusal, bool> matches) => _matches = matches;

    /// <summary>
    /// Gets the selector for authored validation refusals.
    /// </summary>
    public static Refused ByValidation { get; } = new(refusal => refusal.Kind == CommandRefusalKind.Validation);

    /// <summary>
    /// Gets the selector for actual authorization denials.
    /// </summary>
    public static Refused ByAuthorization { get; } = new(refusal => refusal.Kind == CommandRefusalKind.Authorization);

    /// <summary>
    /// Gets the selector for validation and constraint refusals, never authorization denials.
    /// </summary>
    public static Refused Any { get; } = new(refusal => refusal.Kind is CommandRefusalKind.Validation or CommandRefusalKind.Constraint);

    /// <summary>
    /// Selects a constraint refusal, optionally by its exact, case-sensitive name.
    /// </summary>
    /// <param name="name">The constraint name, or null to match any constraint.</param>
    /// <returns>The constraint selector.</returns>
    public static Refused ByConstraint(string? name = null) =>
        new(refusal => refusal.Kind == CommandRefusalKind.Constraint && (name is null || refusal.ConstraintNames.Contains(name)));

    /// <summary>
    /// Tests whether the refusal satisfies this selector.
    /// </summary>
    /// <param name="refusal">The classified refusal.</param>
    /// <returns>Whether the selector matches.</returns>
    internal bool Matches(CommandRefusal refusal) => _matches(refusal);
}
