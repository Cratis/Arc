// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the options for one connection resolver slot.
/// </summary>
public class ConnectionResolverOptions
{
    /// <summary>
    /// Gets or sets the <see cref="ConnectionResolverType"/> to use.
    /// </summary>
    public ConnectionResolverType Resolver { get; set; } = ConnectionResolverType.Default;

    /// <summary>
    /// Gets or sets the options used when <see cref="Resolver"/> is <see cref="ConnectionResolverType.Template"/>.
    /// </summary>
    public TemplateConnectionResolverOptions Template { get; set; } = new();

    /// <summary>
    /// Gets or sets the options used when <see cref="Resolver"/> is <see cref="ConnectionResolverType.Http"/>.
    /// </summary>
    public HttpConnectionResolverOptions Http { get; set; } = new();

    /// <summary>
    /// Gets or sets the <see cref="IConnectionResolver"/> type used when <see cref="Resolver"/> is <see cref="ConnectionResolverType.Custom"/>.
    /// </summary>
    /// <remarks>
    /// The type is created through dependency injection, so it can take any registered service in its constructor.
    /// It can only be set from code.
    /// </remarks>
    public Type? CustomType { get; set; }

    /// <summary>
    /// Use the default resolver, which leaves the configured connection as it is.
    /// </summary>
    /// <returns>The <see cref="ConnectionResolverOptions"/> for continuation.</returns>
    public ConnectionResolverOptions UseDefault()
    {
        Resolver = ConnectionResolverType.Default;
        return this;
    }

    /// <summary>
    /// Use templates for resolving.
    /// </summary>
    /// <param name="connectionString">Optional connection string template.</param>
    /// <param name="database">Optional database name template.</param>
    /// <param name="schema">Optional schema name template.</param>
    /// <returns>The <see cref="ConnectionResolverOptions"/> for continuation.</returns>
    public ConnectionResolverOptions UseTemplate(string? connectionString = null, string? database = null, string? schema = null)
    {
        Resolver = ConnectionResolverType.Template;
        Template = new() { ConnectionString = connectionString, Database = database, Schema = schema };
        return this;
    }

    /// <summary>
    /// Use an HTTP endpoint for resolving.
    /// </summary>
    /// <param name="endpoint">The endpoint the request is posted to.</param>
    /// <param name="headers">Optional headers sent with every request.</param>
    /// <returns>The <see cref="ConnectionResolverOptions"/> for continuation.</returns>
    public ConnectionResolverOptions UseHttp(Uri endpoint, IDictionary<string, string>? headers = null)
    {
        Resolver = ConnectionResolverType.Http;
        Http.Endpoint = endpoint;
        if (headers is not null)
        {
            Http.Headers = new Dictionary<string, string>(headers);
        }

        return this;
    }

    /// <summary>
    /// Use a custom <see cref="IConnectionResolver"/> for resolving.
    /// </summary>
    /// <typeparam name="TResolver">The <see cref="IConnectionResolver"/> type.</typeparam>
    /// <returns>The <see cref="ConnectionResolverOptions"/> for continuation.</returns>
    public ConnectionResolverOptions UseCustom<TResolver>()
        where TResolver : IConnectionResolver
    {
        Resolver = ConnectionResolverType.Custom;
        CustomType = typeof(TResolver);
        return this;
    }
}
