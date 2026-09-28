// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization;

/// <summary>
/// A named policy type, resolved from the executing scope rather than captured at discovery.
/// </summary>
/// <param name="Name">The policy name.</param>
/// <param name="PolicyType">The registered implementation type.</param>
public record AuthorizationPolicyRegistration(string Name, Type PolicyType)
{
    /// <summary>
    /// Gets whether this policy may evaluate unauthenticated callers.
    /// </summary>
    public bool EvaluatesAnonymous { get; init; }
}
