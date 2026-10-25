// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EndpointGroups;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.AspNetCore.EndpointGroups;

/// <summary>
/// Selects Arc endpoints by the endpoint groups served on the local port of the connection.
/// </summary>
/// <param name="options">The <see cref="ArcOptions"/> holding the port assignments.</param>
/// <remarks>
/// <para>
/// The policy runs while routing chooses among the candidate endpoints, before an endpoint is selected, so an
/// endpoint in a group the port does not serve is never matched. Requests on the public port therefore get the
/// same result as for a route that does not exist.
/// </para>
/// <para>
/// The decision uses <see cref="ConnectionInfo.LocalPort"/>, never the Host header, which the caller controls.
/// </para>
/// </remarks>
public sealed class EndpointGroupMatcherPolicy(IOptions<ArcOptions> options) : MatcherPolicy, IEndpointSelectorPolicy
{
    /// <inheritdoc/>
    public override int Order => -1000;

    /// <inheritdoc/>
    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) =>
        options.Value.EndpointGroups.HasGroups ||
        endpoints.Any(_ => _.Metadata.GetMetadata<EndpointGroupMetadata>()?.Name is not null);

    /// <inheritdoc/>
    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        var scope = options.Value.EndpointGroups.ScopeFor(httpContext.Connection.LocalPort);
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var group = candidates[i].Endpoint.Metadata.GetMetadata<EndpointGroupMetadata>();
            if (group is not null && !scope.Serves(group.Name))
            {
                candidates.SetValidity(i, false);
            }
        }

        return Task.CompletedTask;
    }
}
