// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Layered.Ordering.Placing;

/// <summary>
/// Represents the identity of a customer.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record CustomerId(Guid Value) : EventSourceId<Guid>(Value)
{
    /// <summary>
    /// The customer nobody is.
    /// </summary>
    public static readonly CustomerId NotSet = new(Guid.Empty);

    /// <summary>
    /// Converts an underlying value to an identity.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    public static implicit operator CustomerId(Guid value) => new(value);

    /// <summary>
    /// Creates a new identity.
    /// </summary>
    /// <returns>The <see cref="CustomerId"/>.</returns>
    public static CustomerId New() => new(Guid.NewGuid());
}

/// <summary>
/// Holds the customers the application knows by name rather than by lookup.
/// </summary>
/// <remarks>
/// This is what the check exists for. A scenario in the project above states one of these as the customer it is
/// about, so reading that scenario means following a member declared here - in a project the workspace hands over as
/// the compilation it built rather than as an assembly on disk. The declaration therefore belongs to a compilation
/// that is not the one reading it, which is a crash rather than a wrong answer unless every body is read through the
/// models of the whole application.
/// </remarks>
public static class KnownCustomers
{
    /// <summary>
    /// The customer the shop itself orders as.
    /// </summary>
    public static readonly CustomerId House = CustomerId.New();
}

/// <summary>
/// The event that occurs when an order has been placed.
/// </summary>
/// <param name="Sku">What was ordered.</param>
[EventType]
public record OrderPlaced(string Sku);
