// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// A reusable 'availability' block.
/// </summary>
/// <param name="Name">The name of the block.</param>
/// <param name="Expression">What must hold. The 'for' and 'when' lines of a block are combined with 'and'.</param>
public record AvailabilityDefinition(string Name, AvailabilityExpression Expression)
{
    /// <summary>
    /// Gets what the block is for, as the model describes it. Empty when the model does not say.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Creates a block from its lines, which must all hold.
    /// </summary>
    /// <param name="name">The name of the block.</param>
    /// <param name="description">What the block is for.</param>
    /// <param name="lines">The lines, in authored order.</param>
    /// <returns>The definition.</returns>
    public static AvailabilityDefinition FromLines(string name, string description, params AvailabilityExpression[] lines) =>
        FromLines(name, lines) with { Description = description };

    /// <summary>
    /// Creates a block from its lines, which must all hold.
    /// </summary>
    /// <param name="name">The name of the block.</param>
    /// <param name="lines">The lines, in authored order.</param>
    /// <returns>The definition.</returns>
    public static AvailabilityDefinition FromLines(string name, params AvailabilityExpression[] lines) =>
        new(name, lines.Length == 1 ? lines[0] : new AvailabilityAnd(lines));
}
