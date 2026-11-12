// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Filters <c>tools/list</c> down to the catalog tools the verified caller's scopes cover, plus the tools the application
/// registered directly on the server unless <see cref="McpOptions.ExclusiveTools"/> is set. A request without a verified
/// caller lists nothing.
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
            var http = services?.GetService<IHttpContextAccessor>()?.HttpContext;
            var caller = McpCaller.From(http);
            var catalog = services?.GetService<McpToolCatalogFactory>()?.Catalog;
            var exclusive = services?.GetService<IOptions<McpOptions>>()?.Value.ExclusiveTools ?? true;
            var listed = new List<Tool>();
            if (caller is not null && catalog is not null && http is not null)
            {
                foreach (var tool in result.Tools)
                {
                    if (catalog.TryGet(tool.Name, out var descriptor)
                        ? caller.HoldsScopesFor(descriptor!) && await McpToolAuthorizers.Allow(new McpToolAuthorization(descriptor!, caller.Capability, http.RequestServices, cancellationToken) { Operation = McpToolOperation.List })
                        : !exclusive)
                    {
                        listed.Add(tool);
                    }
                }
            }

            result.Tools = listed;
            return result;
        };
}
