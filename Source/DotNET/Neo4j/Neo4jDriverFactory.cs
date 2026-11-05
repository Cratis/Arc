// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Represents an implementation of <see cref="INeo4jDriverFactory"/>.
/// </summary>
/// <param name="options"><see cref="IOptions{TOptions}"/> for getting the options.</param>
/// <param name="configuration">The <see cref="IConfiguration"/> the password secret is read from.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[Singleton]
public class Neo4jDriverFactory(IOptions<Neo4jOptions> options, IConfiguration configuration, ILogger<Neo4jDriverFactory> logger) : INeo4jDriverFactory, IAsyncDisposable, IDisposable
{
    readonly ConcurrentDictionary<(string Server, IAuthTokenManager Authentication), IDriver> _drivers = new();
    readonly Func<Uri, IAuthTokenManager, IDriver> _createDriver = static (server, authentication) => GraphDatabase.Driver(server, authentication);

    /// <summary>
    /// Initializes a new instance of the <see cref="Neo4jDriverFactory"/> class with a native driver constructor.
    /// </summary>
    /// <param name="options">The Neo4j options.</param>
    /// <param name="configuration">The configuration holding secrets.</param>
    /// <param name="logger">The factory logger.</param>
    /// <param name="createDriver">The native driver constructor.</param>
    internal Neo4jDriverFactory(IOptions<Neo4jOptions> options, IConfiguration configuration, ILogger<Neo4jDriverFactory> logger, Func<Uri, IAuthTokenManager, IDriver> createDriver)
        : this(options, configuration, logger) => _createDriver = createDriver;

    /// <inheritdoc/>
    public IDriver Create() => Create(new Uri(options.Value.Server), GetConfiguredAuthentication());

    /// <inheritdoc/>
    public IDriver Create(Uri server, IAuthTokenManager authentication) =>
        _drivers.GetOrAdd(
            (server.ToString(), authentication),
            static (_, state) => state.Factory.CreateDriver(state.Server, state.Authentication),
            (Factory: this, Server: server, Authentication: authentication));

    /// <inheritdoc/>
    public BasicGraphAuthentication GetConfiguredAuthentication()
    {
        var current = options.Value;
        var password = string.IsNullOrEmpty(current.PasswordSecret) ? current.Password : configuration[current.PasswordSecret] ?? current.Password;
        return new BasicGraphAuthentication(current.UserName, password);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        var drivers = _drivers.Values.ToArray();
        _drivers.Clear();
        foreach (var driver in drivers)
        {
            await driver.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    IDriver CreateDriver(Uri server, IAuthTokenManager authentication)
    {
        logger.CreateDriver(server.Authority);
        return _createDriver(server, authentication);
    }
}
