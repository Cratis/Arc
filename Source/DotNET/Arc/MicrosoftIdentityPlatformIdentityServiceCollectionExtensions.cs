// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Microsoft Identity Platform based authentication to <see cref="IServiceCollection"/>.
/// </summary>
public static class MicrosoftIdentityPlatformIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Add the Microsoft Identity Platform identity authentication.
    /// </summary>
    /// <remarks>
    /// The scheme reads the unsigned identity headers that Azure App Service or Container Apps authentication (EasyAuth)
    /// and Cratis AuthProxy forward. It ignores them, and requests stay anonymous, until the host opts in with
    /// <see cref="Cratis.Arc.ArcOptions.TrustForwardedIdentityHeaders"/> (<c>Cratis:Arc:TrustForwardedIdentityHeaders</c>).
    /// Opt in only when every request reaches the application through such an ingress. Until then, the host logs a
    /// warning naming the opt-in on every startup, and again on the first request that carries the headers.
    /// </remarks>
    /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
    /// <param name="scheme">Optional scheme name to use.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddMicrosoftIdentityPlatformIdentityAuthentication(this IServiceCollection services, string? scheme = default)
    {
        scheme ??= MicrosoftIDentityPlatformAuthHandler.SchemeName;

        services
            .AddAuthentication(scheme)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(scheme, _ => { });

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, UntrustedForwardedIdentityHeadersStartupWarning>());

        return services;
    }
}