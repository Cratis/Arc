// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp;

/// <summary>
/// Represents the set of operations explicitly declared as exposed over MCP. Empty by default.
/// </summary>
public sealed class McpExposure
{
    readonly List<McpExposureDeclaration> _declarations = [];

    /// <summary>
    /// Gets the declarations made so far.
    /// </summary>
    public IReadOnlyList<McpExposureDeclaration> Declarations => _declarations.ToArray();

    /// <summary>
    /// Exposes a command as a write tool.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The exposure for continuation.</returns>
    public McpExposure Expose<TCommand>(string name, string description) => Expose(typeof(TCommand), name, description);

    /// <summary>
    /// Exposes a command as a write tool.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The exposure for continuation.</returns>
    public McpExposure Expose(Type commandType, string name, string description)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        _declarations.Add(new McpExposureDeclaration(McpToolKind.Command, commandType, name, description));
        return this;
    }

    /// <summary>
    /// Exposes a query as a read tool.
    /// </summary>
    /// <param name="fullyQualifiedQueryName">The fully qualified query name, as in "Namespace.ReadModel.Method".</param>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The exposure for continuation.</returns>
    public McpExposure ExposeQuery(string fullyQualifiedQueryName, string name, string description)
    {
        ArgumentNullException.ThrowIfNull(fullyQualifiedQueryName);
        _declarations.Add(new McpExposureDeclaration(McpToolKind.Query, fullyQualifiedQueryName, name, description));
        return this;
    }
}
