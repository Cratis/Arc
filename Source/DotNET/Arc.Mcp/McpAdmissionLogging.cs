// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Safe diagnostics for requests refused before the MCP transport.
/// </summary>
internal static partial class McpAdmissionLogging
{
    /// <summary>
    /// Logs a refused origin with control characters removed and its length bounded.
    /// </summary>
    /// <param name="logger">The admission logger.</param>
    /// <param name="origins">The Origin header values of the refused request.</param>
    internal static void OriginRefused(this ILogger<McpAdmission> logger, IReadOnlyList<string?> origins)
    {
        var origin = string.Concat((origins.Count > 0 ? origins[0] ?? string.Empty : string.Empty).Where(_ => !char.IsControl(_)).Take(200));
        logger.RefusedOrigin(origin, Reason(origins));
    }

    /// <summary>
    /// Logs that a request was refused because of its origin.
    /// </summary>
    /// <param name="logger">The admission logger.</param>
    /// <param name="origin">The sanitized origin.</param>
    /// <param name="reason">Why the origin was refused.</param>
    [LoggerMessage(5101, LogLevel.Debug, "MCP request refused: origin '{Origin}' ({Reason}). Check McpOptions.AllowedOrigins (add the origin to allow browser-based tools such as MCP Inspector)")]
    internal static partial void RefusedOrigin(this ILogger<McpAdmission> logger, string origin, string reason);

    static string Reason(IReadOnlyList<string?> origins) => origins.Count != 1
        ? "multiple Origin values; send exactly one origin"
        : McpOrigins.IsOrigin(origins[0]) ? "origin is not allowed" : "malformed Origin; send only scheme, host and port";
}
