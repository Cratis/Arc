// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Preflights statically known key types of discovered protected commands without discovering properties at runtime.
/// </summary>
/// <param name="handlers">The discovered command handlers.</param>
/// <param name="configuration">The host's operational read-protection configuration.</param>
/// <param name="traversal">The key coverage checker.</param>
internal sealed class ProtectedReadModelKeyStartup(
    ICommandHandlerProviders handlers,
    IConfiguration configuration,
    ProtectedReadModelKeyTraversal traversal) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (ReadProtectionConfiguration.MaximumMode(configuration) != ReadProtectionMode.On) return Task.CompletedTask;
        foreach (var command in handlers.Handlers.Select(handler => handler.CommandType).Distinct()
            .Where(command => CommandAttributePolicy.ResolveCommand(command).ReadProtection == (int)ReadProtectionMode.On))
        {
            foreach (var keyType in ProtectedReadModelKeyAccessors.DeclaredFor(command))
            {
                traversal.EnsureCoverage(keyType);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
