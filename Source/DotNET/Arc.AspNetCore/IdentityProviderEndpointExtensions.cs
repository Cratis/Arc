// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for setting up identity providers.
/// </summary>
public static class IdentityProviderEndpointExtensions
{
    /// <summary>
    /// Map identity provider endpoints.
    /// </summary>
    /// <param name="endpoints">Endpoints to extend.</param>
    /// <param name="appBuilder">Optional <see cref="IApplicationBuilder"/> adding to if the route builder is not an app builder.</param>
    /// <returns>Continuation.</returns>
    /// <exception cref="MultipleIdentityDetailsProvidersFound">Thrown if multiple identity details providers are found.</exception>
    public static IEndpointRouteBuilder MapIdentityProvider(this IEndpointRouteBuilder endpoints, IApplicationBuilder? appBuilder = default)
    {
        if (endpoints is IApplicationBuilder appBuilderAsEndpointBuilder)
        {
            appBuilder = appBuilderAsEndpointBuilder;
        }

        if (appBuilder is not null)
        {
            var serviceProviderIsService = appBuilder.ApplicationServices.GetService<IServiceProviderIsService>();
            if (serviceProviderIsService!.IsService(typeof(IProvideIdentityDetails)))
            {
                endpoints
                    .MapGet(".cratis/me", (HttpRequest request, HttpResponse response) =>
                        appBuilder.ApplicationServices.GetService<IdentityProviderEndpoint>()!.Handler(response))
                    .WithTags("Cratis Identity");
            }
        }

        return endpoints;
    }

    /// <summary>
    /// Map identity provider endpoints.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder MapIdentityProvider(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            MapIdentityProvider(endpoints, app);
        }

        return app;
    }
}
