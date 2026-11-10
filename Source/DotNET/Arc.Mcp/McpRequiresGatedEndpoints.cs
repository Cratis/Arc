// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// The exception that is thrown when MCP is served while the capability gate admits every request before routing.
/// </summary>
/// <remarks>
/// The gate only knows which endpoint a request is for, and so only answers with MCP challenges, when it selects gated
/// endpoints after routing. Set <c>CapabilitiesOptions.GatedEndpoints</c>; the MCP endpoint is added to the selection.
/// </remarks>
public class McpRequiresGatedEndpoints()
    : InvalidMcpConfiguration("MCP serving requires selected gated endpoints. Set CapabilitiesOptions.GatedEndpoints when calling WithCapabilities() (registration order does not matter); MCP is added to that selection. Assign a port to the capability endpoint group and install verified callers with WithPersonalAccessTokens() or WithVerifiedCallers(). See https://www.cratis.io/arc/backend/csharp/mcp/troubleshooting for McpOptions and setup guidance.");
