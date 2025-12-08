// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Checks if an endpoint with the specified name already exists.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="endpointName">The name of the endpoint to check.</param>
    /// <returns>True if the endpoint exists, false otherwise.</returns>
    public static bool EndpointExists(this IEndpointRouteBuilder endpoints, string endpointName) =>
        endpoints.DataSources
            .SelectMany(ds => ds.Endpoints)
            .Any(e => e.Metadata.GetMetadata<EndpointNameMetadata>()?.EndpointName == endpointName);
}
