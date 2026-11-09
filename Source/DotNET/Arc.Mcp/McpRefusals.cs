// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Writes Arc's MCP refusal responses for host middleware and authorizers.
/// </summary>
/// <remarks>
/// These methods only write a response. They do not verify credentials, publish a caller, or authorize an operation.
/// Use the host's validated MCP configuration, never values derived from request headers, for challenges.
/// </remarks>
public static class McpRefusals
{
    /// <summary>
    /// Writes a 401 unauthorized response with the configured Bearer challenge.
    /// </summary>
    /// <param name="context">The HTTP request.</param>
    /// <param name="options">The host's validated MCP options.</param>
    /// <returns>The response-writing task.</returns>
    public static Task Unauthorized(HttpContext context, McpOptions options) =>
        McpChallenge.Write(context, StatusCodes.Status401Unauthorized, "unauthorized", McpChallenge.Unauthorized(options));

    /// <summary>
    /// Writes a 403 forbidden response without adding a scope challenge.
    /// </summary>
    /// <param name="context">The HTTP request.</param>
    /// <returns>The response-writing task.</returns>
    public static Task Forbidden(HttpContext context) =>
        McpChallenge.Write(context, StatusCodes.Status403Forbidden, "forbidden");

    /// <summary>
    /// Writes a 403 insufficient_scope response with the required scopes in the Bearer challenge.
    /// </summary>
    /// <param name="context">The HTTP request.</param>
    /// <param name="options">The host's validated MCP options.</param>
    /// <param name="scopes">The configured scopes the operation requires.</param>
    /// <param name="stepUp">Whether the verified caller can step up through OAuth. Defaults to false.</param>
    /// <returns>The response-writing task.</returns>
    public static Task InsufficientScope(HttpContext context, McpOptions options, IEnumerable<string> scopes, bool stepUp = false) =>
        McpChallenge.Write(context, StatusCodes.Status403Forbidden, "insufficient_scope", McpChallenge.InsufficientScope(options, scopes, stepUp));
}
