// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Gates a part of the application with an availability block: <c>[Available&lt;DataQuality&gt;]</c>.
/// </summary>
/// <typeparam name="TBlock">The block.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableAttribute<TBlock> : Attribute, IAvailabilityAttribute
    where TBlock : AvailabilityBlock
{
    /// <inheritdoc/>
    public AvailabilityTerm Term { get; } = new BlockTerm(typeof(TBlock));
}
