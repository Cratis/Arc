// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for mapping observable query hub endpoints.
/// </summary>
public static class ObservableQueryHubEndpointExtensions
{
    /// <summary>
    /// Maps the observable query hub endpoints (<c>/.cratis/queries/ws</c> and <c>/.cratis/queries/sse</c>)
    /// for composite real-time query streaming.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseObservableQueryHub(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapObservableQueryHubEndpoints(app.ApplicationServices);
        }

        return app;
    }
}
