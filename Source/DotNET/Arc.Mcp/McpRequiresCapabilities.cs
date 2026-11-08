// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when MCP is configured without capability hosting.
/// </summary>
public class McpRequiresCapabilities()
    : InvalidMcpConfiguration("MCP serving requires capability hosting. Call WithCapabilities() before WithMcp().");
