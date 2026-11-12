// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Builds the <see cref="McpToolCatalog"/> exactly once, caching a failure so every caller fails closed.
/// </summary>
/// <param name="exposure">The explicit exposure.</param>
/// <param name="options">The MCP options.</param>
/// <param name="commands">The command handler providers.</param>
/// <param name="queries">The query performer providers.</param>
/// <param name="sources">The curated tool sources.</param>
/// <param name="arcOptions">The Arc serializer options used for binding.</param>
internal sealed class McpToolCatalogFactory(
    McpExposure exposure,
    IOptions<McpOptions> options,
    ICommandHandlerProviders commands,
    IQueryPerformerProviders queries,
    IEnumerable<IMcpToolSource> sources,
    IOptions<ArcOptions>? arcOptions = null)
{
    readonly Lazy<McpToolCatalog> _catalog = new(() => McpToolCatalog.Build(exposure, options.Value, commands, queries, sources, arcOptions?.Value.JsonSerializerOptions));

    /// <summary>
    /// Gets the catalog.
    /// </summary>
    public McpToolCatalog Catalog => _catalog.Value;
}
