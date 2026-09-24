// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A read model protected by independent Arc and ASP.NET Core policies on one declaration.
/// </summary>
/// <param name="Value">The result value.</param>
[ReadModel]
[Cratis.Arc.Authorization.Authorize(Policy = "ActiveSubscription")]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "Audit")]
public record StackedPolicyReadModel(string Value)
{
    static int _all;
    static int _overrides;

    /// <summary>
    /// Gets the number of fully policy-authorized queries.
    /// </summary>
    public static int AllCount => Volatile.Read(ref _all);

    /// <summary>
    /// Gets the number of queries using the method override.
    /// </summary>
    public static int OverrideCount => Volatile.Read(ref _overrides);

    /// <summary>
    /// Requires both policies from the read-model declaration.
    /// </summary>
    /// <returns>The read model.</returns>
    [Path("/api/stacked-policy")]
    public static StackedPolicyReadModel All()
    {
        Interlocked.Increment(ref _all);
        return new("all");
    }

    /// <summary>
    /// Replaces the read-model policies with one method-level role requirement.
    /// </summary>
    /// <returns>The read model.</returns>
    [Path("/api/stacked-override")]
    [Cratis.Arc.Authorization.Roles("Admin")]
    public static StackedPolicyReadModel Override()
    {
        Interlocked.Increment(ref _overrides);
        return new("override");
    }
}
