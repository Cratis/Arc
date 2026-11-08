// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception that is thrown when the arguments of a tool call do not fit the tool. A curated tool source may throw
/// it; the caller then gets a fixed message that reveals nothing about the reason.
/// </summary>
/// <param name="tool">The tool name.</param>
/// <param name="innerException">The reason, if any.</param>
public sealed class McpArgumentsInvalid(string tool, Exception? innerException = null)
    : Exception($"The arguments are not valid for MCP tool '{tool}'.", innerException);
