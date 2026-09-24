// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Queries.ModelBound;

/// <summary>A second hub admission that holds a selected identity during an earlier subscription's emission.</summary>
/// <param name="Value">The selected caller's name.</param>
[ReadModel]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "OtherSubscription")]
[Cratis.Arc.Authorization.Authorize(Policy = "Gated")]
public record GatedSelectedReadModel(string Value)
{
    /// <summary>Returns the selected principal after the gate opens.</summary>
    /// <param name="principal">The current Arc principal.</param>
    /// <returns>The selected principal's name.</returns>
    public static GatedSelectedReadModel All(Cratis.Arc.Authorization.ICurrentPrincipalAccessor principal) =>
        new(principal.Current?.Identity?.Name ?? "Missing");
}
