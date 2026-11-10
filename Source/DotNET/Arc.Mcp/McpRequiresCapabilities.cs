// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception thrown when MCP is configured without capability hosting.
/// </summary>
public class McpRequiresCapabilities()
    : InvalidMcpConfiguration("MCP serving requires WithCapabilities() (registration order does not matter), a port assigned to its endpoint group, and CapabilitiesOptions.GatedEndpoints selecting the gated endpoints. Install verified callers with WithPersonalAccessTokens() or WithVerifiedCallers(). See https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting for McpOptions and setup guidance.");
