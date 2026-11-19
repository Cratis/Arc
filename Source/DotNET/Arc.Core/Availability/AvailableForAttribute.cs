// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Makes a command, or the queries of a read model, available only to the named audiences.
/// </summary>
/// <remarks>
/// Several audiences on one attribute are alternatives (or); several availability attributes on one type must all hold
/// (and). An unknown audience never opens the gate. The attribute does nothing unless the host registers an
/// <see cref="IAvailabilityGate"/>.
/// </remarks>
/// <param name="audiences">The audiences, any one of which opens the gate.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableForAttribute(params string[] audiences) : Attribute
{
    /// <summary>
    /// Gets the audiences, any one of which opens the gate.
    /// </summary>
    public IReadOnlyList<string> Audiences { get; } = audiences;
}
