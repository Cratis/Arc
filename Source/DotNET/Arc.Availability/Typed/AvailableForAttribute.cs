// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Gates a part of the application to an audience: <c>[AvailableFor&lt;Plus&gt;]</c>.
/// </summary>
/// <remarks>
/// Every attribute is a line of its own and every line must hold, so <c>[AvailableFor&lt;Plus&gt;, AvailableFor&lt;Preview&gt;]</c>
/// needs both audiences. To allow either, name both: <c>[AvailableFor&lt;Plus, Preview&gt;]</c>.
/// </remarks>
/// <typeparam name="TAudience">The audience.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableForAttribute<TAudience> : Attribute, IAvailabilityAttribute
    where TAudience : Audience
{
    /// <inheritdoc/>
    public AvailabilityTerm Term { get; } = new AudienceTerm(typeof(TAudience), false);
}
