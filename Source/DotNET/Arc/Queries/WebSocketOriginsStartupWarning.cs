// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Warns on startup when cookie authentication is configured and no trusted origins are set for observable query
/// WebSocket connections, so the application can decide whether to restrict them.
/// </summary>
/// <param name="arcOptions">The <see cref="ArcOptions"/> holding <see cref="QueryOptions.AllowedWebSocketOrigins"/>.</param>
/// <param name="schemes">The <see cref="IAuthenticationSchemeProvider"/> to look for cookie authentication in.</param>
/// <param name="logger">The <see cref="ILogger"/> to warn with.</param>
internal sealed class WebSocketOriginsStartupWarning(
    IOptions<ArcOptions> arcOptions,
    IAuthenticationSchemeProvider schemes,
    ILogger<WebSocketOriginsStartupWarning> logger) : IHostedService
{
    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (arcOptions.Value.Query.AllowedWebSocketOrigins is not null)
        {
            return;
        }

        var all = await schemes.GetAllSchemesAsync();
        if (all.Any(scheme => typeof(CookieAuthenticationHandler).IsAssignableFrom(scheme.HandlerType)))
        {
            logger.WebSocketOriginsNotRestricted();
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
