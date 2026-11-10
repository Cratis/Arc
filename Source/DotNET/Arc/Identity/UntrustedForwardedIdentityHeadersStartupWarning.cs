// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Warns on every startup when the Microsoft Identity Platform scheme is registered but the host does not trust the
/// forwarded identity headers it reads, so an application that relied on them does not turn anonymous silently.
/// When on-behalf forwarding is enabled, it also reports a configuration that would make every on-behalf forward fail.
/// </summary>
/// <param name="arcOptions">The <see cref="ArcOptions"/> that decide whether forwarded identity headers are trusted.</param>
/// <param name="logger">The <see cref="ILogger"/> to warn with.</param>
internal sealed class UntrustedForwardedIdentityHeadersStartupWarning(
    IOptions<ArcOptions> arcOptions,
    ILogger<UntrustedForwardedIdentityHeadersStartupWarning> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!arcOptions.Value.TrustForwardedIdentityHeaders)
        {
            logger.ForwardedIdentityHeadersNotTrustedAtStartup();
        }

        if (arcOptions.Value.OnBehalfOf?.Enabled == true &&
            OnBehalfOfSigningKeys.For(arcOptions.Value.OnBehalfOf).Problem is { } problem)
        {
            logger.OnBehalfOfMisconfiguredAtStartup(problem);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
