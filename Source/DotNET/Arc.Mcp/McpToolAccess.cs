// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Defines whether a tool reads or changes state.
/// </summary>
public enum McpToolAccess
{
    /// <summary>The tool only reads state.</summary>
    Read = 0,

    /// <summary>The tool may change state.</summary>
    Write = 1
}
