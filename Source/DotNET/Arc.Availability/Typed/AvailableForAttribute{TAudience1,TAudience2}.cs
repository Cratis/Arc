// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Gates a part of the application to either of two audiences: <c>[AvailableFor&lt;Plus, Preview&gt;]</c>.
/// </summary>
/// <remarks>
/// The model states it as <c>available for Plus or Preview</c>. Two attributes are two lines that must both hold.
/// </remarks>
/// <typeparam name="TAudience1">The first audience.</typeparam>
/// <typeparam name="TAudience2">The second audience.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableForAttribute<TAudience1, TAudience2> : Attribute, IAvailabilityAttribute
    where TAudience1 : Audience
    where TAudience2 : Audience
{
    /// <inheritdoc/>
    public AvailabilityTerm Term { get; } = new AnyOfTerm(new AudienceTerm(typeof(TAudience1), false), new AudienceTerm(typeof(TAudience2), false));
}
