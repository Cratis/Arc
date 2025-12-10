// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up identity providers.
/// </summary>
public static class IdentityProviderEndpointExtensions
{
    /// <summary>
    /// Map identity provider endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder MapIdentityProvider(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapIdentityProviderEndpoint(app.ApplicationServices);
        }

        return app;
    }
}
