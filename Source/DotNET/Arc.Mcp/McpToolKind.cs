// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Defines what a tool executes.
/// </summary>
public enum McpToolKind
{
    /// <summary>The tool performs an Arc query.</summary>
    Query = 0,

    /// <summary>The tool executes an Arc command.</summary>
    Command = 1,

    /// <summary>The tool is curated by the application.</summary>
    Custom = 2
}
