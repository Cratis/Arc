// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Identity;

/// <summary>Fails startup before any forwarded authentication can use an invalid required trust bundle.</summary>
/// <param name="trust">The forwarding-peer trust store.</param>
internal sealed class ForwardedIdentityHopStartupValidation(ForwardedIdentityClientCertificateTrustStore trust) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        trust.ValidateConfiguration();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
