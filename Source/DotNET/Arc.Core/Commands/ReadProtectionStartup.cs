// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>
/// Makes operational downgrades visible before commands are accepted.
/// </summary>
/// <param name="configuration">The host configuration.</param>
/// <param name="handlers">The discovered command handlers.</param>
/// <param name="logger">The startup logger.</param>
internal sealed class ReadProtectionStartup(IConfiguration configuration, ICommandHandlerProviders handlers, ILogger<ReadProtectionStartup> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var maximum = ReadProtectionConfiguration.MaximumMode(configuration);
        if (maximum == ReadProtectionMode.On)
        {
            return Task.CompletedTask;
        }

        foreach (var command in handlers.Handlers.Select(_ => _.CommandType).Distinct())
        {
            var declared = (ReadProtectionMode)CommandAttributePolicy.ResolveCommand(command).ReadProtection;
            if (declared > maximum)
            {
                logger.ModeLowered(command.FullName ?? command.Name, declared, maximum);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
