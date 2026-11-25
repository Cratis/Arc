// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Gates a part of the application with a condition class: <c>[AvailableWhen&lt;HasPaidInvoice&gt;]</c>.
/// </summary>
/// <remarks>
/// The condition is an <see cref="IAvailabilityCondition"/> that can inject services. The model states it as an opaque
/// condition, because the rules inside a class cannot be read back.
/// </remarks>
/// <typeparam name="TCondition">The condition.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AvailableWhenAttribute<TCondition> : Attribute, IAvailabilityAttribute
    where TCondition : IAvailabilityCondition
{
    /// <inheritdoc/>
    public AvailabilityTerm Term { get; } = new ConditionClassTerm(typeof(TCondition));
}
