// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>
/// A read model whose query waits for a scoped native policy.
/// </summary>
/// <param name="Value">The result value.</param>
[ReadModel]
[Cratis.Arc.Authorization.Authorize(Policy = "Gated")]
public record GatedReadModel(string Value)
{
    static int _performed;

    /// <summary>
    /// Gets the number of completed query invocations.
    /// </summary>
    public static int Performed => Volatile.Read(ref _performed);

    /// <summary>
    /// Performs the protected query.
    /// </summary>
    /// <param name="principal">The principal selected for this query.</param>
    /// <returns>The current read model.</returns>
    [Path("/api/gated-read-model")]
    public static GatedReadModel All(ICurrentPrincipalAccessor principal)
    {
        Interlocked.Increment(ref _performed);
        return new(principal.Current?.Identity?.Name ?? "Missing");
    }
}
