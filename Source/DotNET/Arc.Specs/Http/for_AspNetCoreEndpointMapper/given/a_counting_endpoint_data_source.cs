// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.given;

/// <summary>
/// An <see cref="EndpointDataSource"/> that records how many times its endpoints were materialized.
/// </summary>
/// <remarks>
/// Reading <see cref="EndpointDataSource.Endpoints"/> on a real route builder rebuilds every endpoint rather than
/// returning a cached list, so the number of reads - not wall-clock time - is what makes the difference between a
/// linear and a quadratic mapping pass observable in a spec.
/// </remarks>
/// <param name="endpointNames">The names of the endpoints this source holds.</param>
public class a_counting_endpoint_data_source(params string[] endpointNames) : EndpointDataSource
{
    readonly List<Endpoint> _endpoints = [.. endpointNames.Select(Endpoint)];

    public int Reads { get; private set; }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            Reads++;

            return _endpoints;
        }
    }

    public override IChangeToken GetChangeToken() => new CancellationChangeToken(CancellationToken.None);

    static Endpoint Endpoint(string name) =>
        new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse($"/{name}"),
            order: 0,
            new EndpointMetadataCollection(new EndpointNameMetadata(name)),
            name);
}
