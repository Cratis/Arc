// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Declares that a command, or a read model query method, is exposed as an MCP tool.
/// </summary>
/// <remarks>
/// Nothing is exposed over MCP unless it is declared with this attribute or through the MCP builder.
/// The name is explicit and stable; it is never derived from the CLR name.
/// </remarks>
/// <param name="name">The snake_case tool name.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ExposeOverMcpAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the tool name.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets or sets the tool description. A tool without a description fails startup.
    /// </summary>
    public string? Description { get; set; }
}
