// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when a curated tool that may change state does not require a configured write scope in every scope set it declares.
/// </summary>
/// <param name="name">The tool name.</param>
public class McpWriteToolLacksWriteScope(string name)
    : InvalidMcpConfiguration($"MCP tool '{name}' may change state, so every scope set it declares must require McpOptions.WriteScope or one of McpOptions.WriteScopes.");
