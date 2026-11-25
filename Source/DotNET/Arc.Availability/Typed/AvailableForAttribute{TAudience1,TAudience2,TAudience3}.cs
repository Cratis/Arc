// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Gates a part of the application to any of three audiences: <c>[AvailableFor&lt;Plus, Preview, Internal&gt;]</c>.
/// </summary>
/// <remarks>
/// The model states it as <c>available for Plus or Preview or Internal</c>. For more audiences, or to mix 'for' with
/// 'not', define an <see cref="AvailabilityBlock"/>.
/// </remarks>
/// <typeparam name="TAudience1">The first audience.</typeparam>
/// <typeparam name="TAudience2">The second audience.</typeparam>
/// <typeparam name="TAudience3">The third audience.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableForAttribute<TAudience1, TAudience2, TAudience3> : Attribute, IAvailabilityAttribute
    where TAudience1 : Audience
    where TAudience2 : Audience
    where TAudience3 : Audience
{
    /// <inheritdoc/>
    public AvailabilityTerm Term { get; } = new AnyOfTerm(
        new AnyOfTerm(new AudienceTerm(typeof(TAudience1), false), new AudienceTerm(typeof(TAudience2), false)),
        new AudienceTerm(typeof(TAudience3), false));
}
