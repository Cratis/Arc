// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for adding query endpoints.
/// </summary>
public static class QueryEndpointsExtensions
{
    /// <summary>
    /// Use Cratis query endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseQueryEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            // For now, use the base mapper - WebSocket support will be added separately
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapQueryEndpoints(app.ApplicationServices);
        }

        return app;
    }
}