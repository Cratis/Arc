// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.PersonalAccessTokens.Development;

/// <summary>Explicit registration of process-local PAT adapters for local Development hosts only.</summary>
public static class DevelopmentPatArcBuilderExtensions
{
    /// <summary>
    /// Uses process-local adapters with the limits configured by WithPersonalAccessTokens.
    /// Registration order does not matter. All PATs disappear on restart; no durable storage or recovery is provided.
    /// Host startup throws outside Development, and every adapter also fails closed outside Development.
    /// </summary>
    /// <param name="builder">The Arc builder with an explicit personal access token installation.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Use a real person directory with WithPersonalAccessTokens, then call this method explicitly.
    /// Do not combine these adapters with a production PAT adapter. Production requires authoritative persistence,
    /// product policy, and recovery evidence outside the store's restore domain.
    /// See https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting.
    /// </remarks>
    public static IArcBuilder UseDevelopmentPersonalAccessTokenAdapters(this IArcBuilder builder)
    {
        builder.Services.AddSingleton<DevelopmentPatAuthority>(services => new(
            services.GetRequiredService<IHostEnvironment>(), services.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton<IPatAuthority>(services => services.GetRequiredService<DevelopmentPatAuthority>());
        builder.Services.AddSingleton<DevelopmentPatPolicy>(services => new(
            services.GetRequiredService<IHostEnvironment>(), services.GetRequiredService<PatSettings>(), services.GetRequiredService<TimeProvider>()));
        builder.Services.AddSingleton<IPatPolicy>(services => services.GetRequiredService<DevelopmentPatPolicy>());
        builder.Services.AddSingleton<DevelopmentPatRecoveryEpoch>(services => new(services.GetRequiredService<IHostEnvironment>()));
        builder.Services.AddSingleton<IPatRecoveryEpoch>(services => services.GetRequiredService<DevelopmentPatRecoveryEpoch>());
        builder.Services.AddHostedService<DevelopmentPatStartupValidation>();
        return builder;
    }
}
