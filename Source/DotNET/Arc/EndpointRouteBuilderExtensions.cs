// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Gets the names of every endpoint currently registered that carries an endpoint name.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The set of endpoint names.</returns>
    /// <remarks>
    /// Reading <see cref="EndpointDataSource.Endpoints"/> while the route builder is still being populated does
    /// not return a cached list - it materializes every endpoint, running the conventions and the request
    /// delegate factory for each one. That makes this an expensive call, so callers registering many endpoints
    /// should take the set once and consult it, rather than asking per registration.
    /// </remarks>
    public static IReadOnlySet<string> EndpointNames(this IEndpointRouteBuilder endpoints)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var endpoint in endpoints.DataSources.SelectMany(dataSource => dataSource.Endpoints))
        {
            if (endpoint.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName is { } name)
            {
                names.Add(name);
            }
        }

        return names;
    }

    /// <summary>
    /// Checks if an endpoint with the specified name already exists.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="endpointName">The name of the endpoint to check.</param>
    /// <returns>True if the endpoint exists, false otherwise.</returns>
    /// <remarks>
    /// Carries the cost described on <see cref="EndpointNames"/> - a single call materializes the whole endpoint
    /// table. Use <see cref="EndpointNames"/> when checking more than one name.
    /// </remarks>
    public static bool EndpointExists(this IEndpointRouteBuilder endpoints, string endpointName) =>
        endpoints.EndpointNames().Contains(endpointName);
}
