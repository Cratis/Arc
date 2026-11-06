// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Arc.Capabilities;

/// <summary>Registers the authentication scheme that presents a verified caller as an ASP.NET Core principal.</summary>
internal static class VerifiedCallerAuthentication
{
    /// <summary>
    /// Gets the name of the verified-caller authentication scheme.
    /// </summary>
    internal const string Scheme = VerifiedCallerIdentity.AuthenticationScheme;

    /// <summary>
    /// Registers the verified-caller scheme once, however many times capability hosting is turned on.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal static void Register(IServiceCollection services)
    {
        services.AddAuthenticationCore();
        services.AddWebEncoders();
        if (services.Any(descriptor => descriptor.ServiceType == typeof(SchemeRegistration)))
        {
            return;
        }

        services.AddSingleton<SchemeRegistration>();
        services.TryAddTransient<VerifiedCallerAuthenticationHandler>();
        services.Configure<AuthenticationOptions>(options => options.AddScheme<VerifiedCallerAuthenticationHandler>(Scheme, displayName: null));
    }

    sealed class SchemeRegistration;
}
