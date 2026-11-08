// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Runs the registered <see cref="IMcpToolAuthorizer"/> instances. The only authorizer check: listing, admission and
/// dispatch all use it so they cannot disagree.
/// </summary>
internal static class McpToolAuthorizers
{
    /// <summary>
    /// Determines whether every registered authorizer allows the caller to use the tool.
    /// </summary>
    /// <param name="tool">The tool from the ceiling.</param>
    /// <param name="caller">The verified caller.</param>
    /// <param name="services">The services of the request.</param>
    /// <param name="cancellationToken">The cancellation token of the request.</param>
    /// <returns>True when every authorizer allows, or none is registered; false when any denies or throws.</returns>
    internal static async ValueTask<bool> Allow(McpToolDescriptor tool, McpCaller caller, IServiceProvider services, CancellationToken cancellationToken)
    {
        var authorization = new McpToolAuthorization(tool, caller.Capability, services, cancellationToken);
        foreach (var authorizer in services.GetServices<IMcpToolAuthorizer>())
        {
            try
            {
                if (!await authorizer.IsAllowed(authorization))
                {
                    return false;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                services.GetService<ILoggerFactory>()?.CreateLogger(typeof(McpToolAuthorizers)).AuthorizerFailed(tool.Name, ex);
                return false;
            }
        }

        return true;
    }
}
