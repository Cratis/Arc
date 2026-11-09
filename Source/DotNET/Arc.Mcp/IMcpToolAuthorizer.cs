// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Decides whether a verified caller may use a tool from the ceiling, on top of the tool's scopes.
/// </summary>
/// <remarks>
/// Register with <see cref="McpBuilder.AddToolAuthorizer{TAuthorizer}"/>. Every registered authorizer must allow a tool for
/// the caller to see it in <c>tools/list</c> or call it. An authorizer that throws denies. Authorizers only narrow access:
/// a caller whose scopes do not satisfy the tool is refused before any authorizer runs. Tools registered directly on the
/// MCP server are not part of the ceiling and are not passed to authorizers.
/// </remarks>
public interface IMcpToolAuthorizer
{
    /// <summary>
    /// Decides whether the caller may use the tool.
    /// </summary>
    /// <param name="authorization">The tool and the caller.</param>
    /// <returns>True to allow; false to deny.</returns>
    ValueTask<bool> IsAllowed(McpToolAuthorization authorization);
}
