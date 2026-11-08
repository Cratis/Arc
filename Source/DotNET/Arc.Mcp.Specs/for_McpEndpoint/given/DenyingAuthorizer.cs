// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// An authorizer that denies the tools named in <see cref="ToolGate.Denied"/>.
/// </summary>
/// <param name="gate">The gate.</param>
public class DenyingAuthorizer(ToolGate gate) : IMcpToolAuthorizer
{
    /// <inheritdoc/>
    public ValueTask<bool> IsAllowed(McpToolAuthorization authorization) =>
        ValueTask.FromResult(!gate.Denied.Contains(authorization.Tool.Name));
}
