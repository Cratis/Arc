// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.PersonalAccessTokens.Development;

/// <summary>Refuses accidental deployment of the explicitly selected development adapters.</summary>
/// <param name="environment">The actual host environment.</param>
/// <param name="logger">The startup logger.</param>
internal sealed partial class DevelopmentPatStartupValidation(IHostEnvironment environment, ILogger<DevelopmentPatStartupValidation> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Development PAT adapters require the Development host environment. Install production PAT authority, policy and recovery adapters instead. See https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting.");
        }
        DevelopmentAdaptersSelected(logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(5104, LogLevel.Warning, "Development PAT adapters selected: all credentials are process-local and disappear on restart. These adapters are not production persistence, policy or recovery infrastructure")]
    static partial void DevelopmentAdaptersSelected(ILogger logger);
}
