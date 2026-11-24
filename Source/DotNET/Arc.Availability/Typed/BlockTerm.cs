// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// A 'with' term that builds on another <see cref="AvailabilityBlock"/>.
/// </summary>
/// <param name="Block">The type of the block.</param>
public sealed record BlockTerm(Type Block) : AvailabilityTerm
{
    /// <inheritdoc/>
    public override AvailabilityExpression ToExpression(Func<Type, IAvailabilityCondition> conditions) =>
        new AvailableWith(AvailabilityBlock.NameOf(Block));
}
