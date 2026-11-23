// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>A rule's explicit applicability and result.</summary>
/// <param name="IsApplicable">Whether the rule owns this binding.</param>
/// <param name="Category">A failure category, or null for structural support.</param>
/// <param name="Guidance">The corrective action for a failure.</param>
/// <param name="Cause">The original failure.</param>
public sealed record CommandWiringVerdict(bool IsApplicable, string? Category = null, string? Guidance = null, Exception? Cause = null)
{
    /// <summary>Gets the verdict for a binding owned by another rule or ordinary DI.</summary>
    public static CommandWiringVerdict NotApplicable { get; } = new(false);

    /// <summary>Gets structural support, not proof that a runtime value exists.</summary>
    public static CommandWiringVerdict Supported { get; } = new(true);
}
