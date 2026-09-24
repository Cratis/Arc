// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// Distinguishes the service scope serving each concurrent policy invocation.
/// </summary>
public class ScopedPolicyProbe
{
    /// <summary>
    /// Gets the identity of this scoped service instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();
}
