// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>Fails startup before any forwarded authentication can use an invalid required trust bundle.</summary>
/// <param name="trust">The forwarding-peer trust store.</param>
/// <param name="options">The forwarding admission options.</param>
/// <param name="logger">The startup diagnostics logger.</param>
internal sealed class ForwardedIdentityHopStartupValidation(
    ForwardedIdentityClientCertificateTrustStore trust,
    IOptions<ArcOptions> options,
    ILogger<ForwardedIdentityHopStartupValidation> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        trust.ValidateConfiguration();
        if (options.Value.ForwardedIdentity.ClientCertificate.Required)
        {
            logger.HostMustRequestClientCertificates();
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
