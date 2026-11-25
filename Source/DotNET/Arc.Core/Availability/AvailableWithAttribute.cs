// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Makes a command, or the queries of a read model, available only when a named, reusable availability holds.
/// </summary>
/// <remarks>
/// The attribute does nothing unless the host registers an <see cref="IAvailabilityGate"/>.
/// </remarks>
/// <param name="name">The name of the availability.</param>
[Obsolete("Define the block as a type that derives from AvailabilityBlock and write [Available<DataQuality>]. A string names no type, so a typo is not a compile error.")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableWithAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the name of the availability.
    /// </summary>
    public string Name { get; } = name;
}
