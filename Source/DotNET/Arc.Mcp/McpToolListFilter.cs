// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Filters <c>tools/list</c> down to the tools the verified caller's scopes cover. A request without a verified caller
/// lists nothing.
/// </summary>
internal static class McpToolListFilter
{
    /// <summary>
    /// Wraps the list handler.
    /// </summary>
    /// <param name="next">The next handler.</param>
    /// <returns>The filtering handler.</returns>
    internal static McpRequestHandler<ListToolsRequestParams, ListToolsResult> Filter(McpRequestHandler<ListToolsRequestParams, ListToolsResult> next) =>
        async (request, cancellationToken) =>
        {
            var result = await next(request, cancellationToken);
            var services = request.Services;
            var caller = McpCaller.From(services?.GetService<IHttpContextAccessor>()?.HttpContext);
            var ceiling = services?.GetService<McpToolCeilingFactory>()?.Ceiling;
            result.Tools = [.. result.Tools.Where(tool =>
                caller is not null &&
                ceiling is not null &&
                ceiling.TryGet(tool.Name, out var descriptor) &&
                caller.Holds(descriptor!.RequiredScopes))];
            return result;
        };
}
