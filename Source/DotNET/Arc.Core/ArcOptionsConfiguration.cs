// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.OptionsBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Binds Arc options without treating missing configuration values as explicit discovery overrides.
/// </summary>
internal static class ArcOptionsConfiguration
{
    /// <summary>
    /// Registers configuration binding and its reload notifications.
    /// </summary>
    /// <param name="services">The host services.</param>
    /// <param name="configuration">The Arc configuration section.</param>
    internal static void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOptionsChangeTokenSource<ArcOptions>>(new ConfigurationChangeTokenSource<ArcOptions>(configuration));
        services.AddOptions<ArcOptions>().Configure<IServiceProvider>((options, provider) =>
            Bind(options, configuration, provider.GetService<ILoggerFactory>()?.CreateLogger(typeof(ArcOptions).FullName!)));
    }

    /// <summary>
    /// Binds configuration while preserving an existing override when its key is absent.
    /// </summary>
    /// <param name="options">The options to bind.</param>
    /// <param name="configuration">The Arc configuration section.</param>
    /// <param name="logger">Optional logger for the runtime subtype warning.</param>
    internal static void Bind(ArcOptions options, IConfiguration configuration, ILogger? logger = null)
    {
        var authenticationOverride = options.Introspection.AuthenticationOverride;
        var queryHealthAuthenticationOverride = options.QueryHealth.AuthenticationOverride;
        var exposeExceptionDetails = options.ExplicitExposeExceptionDetails;
        ArcOptionsBinder.Bind(options, configuration, logger);
        if (configuration["ExposeExceptionDetails"] is null)
        {
            options.ExplicitExposeExceptionDetails = exposeExceptionDetails;
        }
        if (configuration["Introspection:RequireAuthentication"] is null)
        {
            options.Introspection.AuthenticationOverride = authenticationOverride;
        }
        if (configuration["QueryHealth:RequireAuthentication"] is null)
        {
            options.QueryHealth.AuthenticationOverride = queryHealthAuthenticationOverride;
        }
    }
}
