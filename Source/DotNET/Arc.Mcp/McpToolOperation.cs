// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Identifies the MCP operation being authorized for a tool.
/// </summary>
public enum McpToolOperation
{
    /// <summary>
    /// A tools/call request, including the dispatch check.
    /// </summary>
    Call = 0,

    /// <summary>
    /// A tools/list request checking whether the tool may be listed.
    /// </summary>
    List = 1
}
