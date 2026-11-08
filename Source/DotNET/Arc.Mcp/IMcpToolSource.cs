// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Defines a source of curated MCP tools that are not a single command or query.
/// </summary>
/// <remarks>
/// The tools a source describes join the frozen <see cref="McpToolCeiling"/> at startup and follow the same rules as
/// exposed operations: a stable snake_case name, a description and at least one required scope. A tool is only invoked
/// for a verified caller that holds every required scope. The caller is passed in the invocation; never read identity
/// or tenant from the arguments.
/// </remarks>
public interface IMcpToolSource
{
    /// <summary>
    /// Describes the tools this source provides. Called once, when the ceiling is built.
    /// </summary>
    /// <returns>The tool descriptors.</returns>
    IEnumerable<McpToolDescriptor> Describe();

    /// <summary>
    /// Invokes one of the tools this source described.
    /// </summary>
    /// <param name="invocation">The invocation.</param>
    /// <returns>The result of the tool.</returns>
    ValueTask<McpToolResult> Invoke(McpToolInvocation invocation);
}
