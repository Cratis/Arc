// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// Adds a source of curated tools. Its tools join the frozen catalog at startup and run as the verified caller.
    /// </summary>
    /// <typeparam name="TSource">The tool source type.</typeparam>
    /// <returns>The builder for continuation.</returns>
    public McpBuilder AddToolSource<TSource>()
        where TSource : class, IMcpToolSource
    {
        _services.TryAddEnumerable(ServiceDescriptor.Singleton<IMcpToolSource, TSource>());
        return this;
    }

    /// <summary>
    /// Adds an authorizer that every tool of the catalog must pass, on top of its scopes.
    /// </summary>
    /// <typeparam name="TAuthorizer">The authorizer type, resolved from the services of each request.</typeparam>
    /// <returns>The builder for continuation.</returns>
    public McpBuilder AddToolAuthorizer<TAuthorizer>()
        where TAuthorizer : class, IMcpToolAuthorizer
    {
        _services.TryAddEnumerable(ServiceDescriptor.Scoped<IMcpToolAuthorizer, TAuthorizer>());
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
    /// Exposes a query of a read model as a read tool.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type that declares the query method.</typeparam>
    /// <param name="method">The query method name; use <c>nameof(ReadModel.Method)</c>.</param>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The builder for continuation.</returns>
    /// <exception cref="ArgumentException">The read model declares no method with that name.</exception>
    public McpBuilder ExposeQuery<TReadModel>(string method, string name, string description)
    {
        _exposure.ExposeQuery<TReadModel>(method, name, description);
        return this;
    }

    /// <summary>
    /// Exposes a query as a read tool.
    /// </summary>
    /// <param name="fullyQualifiedQueryName">The fully qualified query name, as in "Namespace.ReadModel.Method".</param>
    /// <param name="name">The snake_case tool name.</param>
    /// <param name="description">The tool description.</param>
    /// <returns>The builder for continuation.</returns>
    [Obsolete("Use ExposeQuery<TReadModel>(nameof(...)) or [ExposeOverMcp]. Will be removed in Arc 24.")]
    public McpBuilder ExposeQuery(string fullyQualifiedQueryName, string name, string description)
    {
#pragma warning disable CS0618 // Forwarding to the equally obsolete exposure overload.
        _exposure.ExposeQuery(fullyQualifiedQueryName, name, description);
#pragma warning restore CS0618
        return this;
    }
}
