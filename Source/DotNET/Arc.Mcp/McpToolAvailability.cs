// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Decides whether a tool is listed, according to the availability of the command or query it executes.
/// </summary>
static class McpToolAvailability
{
    /// <summary>
    /// Determines whether a tool is listed for the current request.
    /// </summary>
    /// <param name="tool">The tool from the catalog.</param>
    /// <param name="services">The services of the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>False when the part the tool executes is not available; true otherwise.</returns>
    /// <remarks>
    /// Without an <see cref="IAvailabilityGate"/> every tool is listed. A failure to decide never hides a tool, because
    /// hiding it would report the failure as "not available": the call itself is refused as a retryable error.
    /// </remarks>
    internal static async ValueTask<bool> IsListed(McpToolDescriptor tool, IServiceProvider services, CancellationToken cancellationToken)
    {
        var gate = services.GetService<IAvailabilityGate>();
        var part = gate is null ? null : PartOf(tool, services);
        if (gate is null || part is null || !gate.IsGated(part))
        {
            return true;
        }

        try
        {
            return (await gate.Decide(part, services, cancellationToken)).IsAvailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            services.GetService<ILoggerFactory>()?.CreateLogger(typeof(McpToolAvailability)).AvailabilityNotDecided(tool.Name, ex);
            return true;
        }
    }

    static Type? PartOf(McpToolDescriptor tool, IServiceProvider services) =>
        tool.Operation switch
        {
            Type command => command,
            FullyQualifiedQueryName query when services.GetService<IQueryPerformerProviders>() is { } performers && performers.TryGetPerformersFor(query, out var performer) => performer.Type,
            _ => null
        };
}
