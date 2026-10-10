// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// Extension methods for <see cref="IAggregateRootContext"/>.
/// </summary>
internal static class AggregateRootContextExtensions
{
    /// <summary>
    /// Check whether the aggregate root of the context declares its event source.
    /// </summary>
    /// <param name="context">The <see cref="IAggregateRootContext"/> to check.</param>
    /// <returns>True if it declares an event source, false if not.</returns>
    public static bool HasDeclaredEventSource(this IAggregateRootContext context) =>
        context is IAggregateRootEventSourceContext { EventSource: not null };
}
