// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp;

/// <summary>
/// Configures MCP serving: options, and the operations explicitly exposed.
/// </summary>
public sealed class McpBuilder
{
    readonly McpExposure _exposure;
    readonly IServiceCollection _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="McpBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="exposure">The shared exposure.</param>
    internal McpBuilder(IServiceCollection services, McpExposure exposure)
    {
        _services = services;
        _exposure = exposure;
    }

    /// <summary>
    /// Configures the MCP options.
    /// </summary>
    /// <param name="configure">The configuration delegate.</param>
    /// <returns>The builder for continuation.</returns>
    public McpBuilder Configure(Action<McpOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _services.Configure(configure);
        return this;
    }

    /// <summary>
    /// Exposes a command as a write tool.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The builder for continuation.</returns>
    public McpBuilder Expose<TCommand>(string name, string description)
    {
        _exposure.Expose<TCommand>(name, description);
        return this;
    }

    /// <summary>
    /// Exposes a query as a read tool.
    /// </summary>
    /// <param name="fullyQualifiedQueryName">The fully qualified query name, as in "Namespace.ReadModel.Method".</param>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The builder for continuation.</returns>
    public McpBuilder ExposeQuery(string fullyQualifiedQueryName, string name, string description)
    {
        _exposure.ExposeQuery(fullyQualifiedQueryName, name, description);
        return this;
    }
}
