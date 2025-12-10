// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Authentication;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Microsoft Identity Platform based authentication to <see cref="IServiceCollection"/>.
/// </summary>
public static class MicrosoftIdentityPlatformIdentityServiceCollectionExtensions
{
    /// <summary>
    /// Add the Microsoft Identity Platform identity authentication.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
    /// <param name="scheme">Optional scheme name to use.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddMicrosoftIdentityPlatformIdentityAuthentication(this IServiceCollection services, string? scheme = default)
    {
        scheme ??= MicrosoftIDentityPlatformAuthHandler.SchemeName;

        services
            .AddAuthentication(scheme)
            .AddScheme<AuthenticationSchemeOptions, MicrosoftIDentityPlatformAuthHandler>(scheme, _ => { });

        return services;
    }
}