// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// Holds what the spec authorizers decide.
/// </summary>
public class ToolGate
{
    /// <summary>
    /// Gets the tools the denying authorizer refuses.
    /// </summary>
    public HashSet<string> Denied { get; } = [];

    /// <summary>
    /// Gets the operations observed by the denying authorizer.
    /// </summary>
    public List<McpToolOperation> Operations { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the throwing authorizer throws.
    /// </summary>
    public bool Throw { get; set; }
}
