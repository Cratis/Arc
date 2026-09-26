// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Cratis.Arc.Introspection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
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
    /// <exception cref="InvalidIntrospectionConfiguration">Authentication or authorization services are missing for protected introspection.</exception>
    public static IApplicationBuilder MapIntrospectionEndpoints(this IApplicationBuilder app)
    {
        if (app is IEndpointRouteBuilder endpoints)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<Cratis.Arc.ArcOptions>>().Value.Introspection;
            if (options.Enabled && options.RequireAuthentication)
            {
                var schemes = app.ApplicationServices.GetService<IAuthenticationSchemeProvider>();
                var defaultScheme = schemes?.GetDefaultAuthenticateSchemeAsync().GetAwaiter().GetResult() ??
                    throw new InvalidIntrospectionConfiguration("Introspection requires a default ASP.NET Core authentication scheme when RequireAuthentication is true.");
                if (!options.TrustForwardedIdentityHeaders && UsesUnsignedIdentityHeaders(app.ApplicationServices, schemes, defaultScheme))
                {
                    throw new InvalidIntrospectionConfiguration("The default ASP.NET Core authentication scheme trusts unsigned x-ms-client-principal headers. Protected introspection requires a trusted ingress (such as Azure App Service or Container Apps EasyAuth) that strips and sets these headers. Set Cratis:Arc:Introspection:TrustForwardedIdentityHeaders=true to opt in, or use a different authentication scheme.");
                }
                if (app.ApplicationServices.GetService<IAuthorizationService>() is null)
                {
                    throw new InvalidIntrospectionConfiguration("Introspection requires ASP.NET Core authorization services (AddAuthorization) when RequireAuthentication is true.");
                }
            }

            var mapper = new AspNetCoreEndpointMapper(endpoints);
            mapper.MapIntrospectionEndpoints(options);
        }

        return app;
    }

    static bool UsesUnsignedIdentityHeaders(IServiceProvider services, IAuthenticationSchemeProvider schemes, AuthenticationScheme scheme)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (visited.Add(scheme.Name))
        {
            if (scheme.HandlerType == typeof(MicrosoftIDentityPlatformAuthHandler))
            {
                return true;
            }

            if (scheme.HandlerType != typeof(PolicySchemeHandler))
            {
                return false;
            }

            var policy = services.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>().Get(scheme.Name);
            var forwarded = policy.ForwardAuthenticate ?? policy.ForwardDefault;
            if (forwarded is null || forwarded == scheme.Name)
            {
                return false;
            }

            var next = schemes.GetSchemeAsync(forwarded).GetAwaiter().GetResult();
            if (next is null)
            {
                return false;
            }
            scheme = next;
        }

        return false;
    }
}
