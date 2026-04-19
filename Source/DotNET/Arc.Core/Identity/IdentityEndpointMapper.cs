// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity;

/// <summary>
/// Maps identity provider endpoints using the provided endpoint mapper.
/// </summary>
public static class IdentityEndpointMapper
{
    /// <summary>
    /// Maps the identity provider endpoint.
    /// </summary>
    /// <param name="mapper">The <see cref="IEndpointMapper"/> to use.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    public static void MapIdentityProviderEndpoint(this IEndpointMapper mapper, IServiceProvider serviceProvider)
    {
        var serviceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>();
        if (serviceProviderIsService?.IsService(typeof(IProvideIdentityDetails)) != true)
        {
            return;
        }

        const string endpointName = "GetIdentityDetails";
        if (mapper.EndpointExists(endpointName))
        {
            return;
        }

        var metadata = new EndpointMetadata(
            endpointName,
            "Get current user identity details",
            ["Cratis Identity"],
            AllowAnonymous: true);

        mapper.MapGet(
            "/.cratis/me",
            async context =>
            {
                var identityProvider = context.RequestServices.GetRequiredService<IIdentityProvider>();
                var result = await identityProvider.Get();

                if (!result.IsAuthenticated)
                {
                    context.StatusCode = 401;
                    return;
                }

                if (!result.IsAuthorized)
                {
                    context.StatusCode = 403;
                    return;
                }

                await identityProvider.SetCookieForHttpResponse(result);
            },
            metadata);
    }
}
