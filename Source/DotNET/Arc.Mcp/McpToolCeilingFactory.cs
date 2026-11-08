// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Builds the <see cref="McpToolCeiling"/> exactly once, caching a failure so every caller fails closed.
/// </summary>
/// <param name="exposure">The explicit exposure.</param>
/// <param name="options">The MCP options.</param>
/// <param name="commands">The command handler providers.</param>
/// <param name="queries">The query performer providers.</param>
internal sealed class McpToolCeilingFactory(
    McpExposure exposure,
    IOptions<McpOptions> options,
    ICommandHandlerProviders commands,
    IQueryPerformerProviders queries)
{
    readonly Lazy<McpToolCeiling> _ceiling = new(() => McpToolCeiling.Build(exposure, options.Value, commands, queries));

    /// <summary>
    /// Gets the ceiling.
    /// </summary>
    public McpToolCeiling Ceiling => _ceiling.Value;
}
