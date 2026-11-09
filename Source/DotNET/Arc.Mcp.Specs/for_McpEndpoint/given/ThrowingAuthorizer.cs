// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// An authorizer that throws when <see cref="ToolGate.Throw"/> is set and allows otherwise.
/// </summary>
/// <param name="gate">The gate.</param>
public class ThrowingAuthorizer(ToolGate gate) : IMcpToolAuthorizer
{
    /// <inheritdoc/>
    public ValueTask<bool> IsAllowed(McpToolAuthorization authorization) =>
        gate.Throw ? throw new InvalidOperationException("Authorizer failed.") : ValueTask.FromResult(true);
}
