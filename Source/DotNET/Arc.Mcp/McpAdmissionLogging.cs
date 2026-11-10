// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Safe diagnostics for requests refused before the MCP transport.
/// </summary>
internal static partial class McpAdmissionLogging
{
    internal static void OriginRefused(this ILogger<McpAdmission> logger, IReadOnlyList<string?> origins)
    {
        var origin = string.Concat((origins.FirstOrDefault() ?? string.Empty).Where(_ => !char.IsControl(_)).Take(200));
        logger.RefusedOrigin(origin, Reason(origins));
    }

    static string Reason(IReadOnlyList<string?> origins) => origins.Count != 1
        ? "multiple Origin values; send exactly one origin"
        : McpOrigins.IsOrigin(origins[0]) ? "origin is not allowed" : "malformed Origin; send only scheme, host and port";

    [LoggerMessage(5101, LogLevel.Debug, "MCP request refused: origin '{Origin}' ({Reason}). Check McpOptions.AllowedOrigins (add the origin to allow browser-based tools such as MCP Inspector)")]
    internal static partial void RefusedOrigin(this ILogger<McpAdmission> logger, string origin, string reason);
}
