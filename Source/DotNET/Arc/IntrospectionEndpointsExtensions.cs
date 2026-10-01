// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Introspection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// <exception cref="InvalidIntrospectionConfiguration">Authentication is explicitly required and authentication or authorization services are missing.</exception>
    public static IApplicationBuilder MapIntrospectionEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<Cratis.Arc.ArcOptions>>().Value.Introspection;
            var logger = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(IntrospectionEndpointMapper).FullName!);
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapIntrospectionEndpoints(options, logger);
        }

        return app;
    }
}
