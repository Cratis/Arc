// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http.Introspection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Provides extension methods for adding introspection endpoints.
/// </summary>
public static class IntrospectionEndpointsExtensions
{
    /// <summary>
    /// Maps introspection endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder MapIntrospectionEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapIntrospectionEndpoints();
        }

        return app;
    }
}
