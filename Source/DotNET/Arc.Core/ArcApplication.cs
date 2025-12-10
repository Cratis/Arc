// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc;

/// <summary>
/// Represents an Arc application.
/// </summary>
public class ArcApplication : IHost, IAsyncDisposable
{
    readonly IHost _host;
    readonly List<Func<IServiceProvider, Task>> _startupActions = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ArcApplication"/> class.
    /// </summary>
    /// <param name="host">The underlying <see cref="IHost"/>.</param>
    /// <param name="options">The <see cref="ArcOptions"/>.</param>
    public ArcApplication(IHost host, ArcOptions options)
    {
        _host = host;
        Options = options;

        var prefixes = GetHttpPrefixes();

#pragma warning disable CA2000 // Dispose objects before losing scope
        var logger = host.Services.GetRequiredService<ILogger<HttpListenerEndpointMapper>>();
        EndpointMapper = new HttpListenerEndpointMapper(logger, [.. prefixes]);
#pragma warning restore CA2000 // Dispose objects before losing scope

        Internals.ServiceProvider = host.Services;
    }

    /// <inheritdoc/>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Gets the options for this Arc application.
    /// </summary>
    public ArcOptions Options { get; }

    /// <summary>
    /// Gets the endpoint mapper for this application.
    /// </summary>
    internal IEndpointMapper EndpointMapper { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="ArcApplicationBuilder"/>.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A new instance of the <see cref="ArcApplicationBuilder"/>.</returns>
    public static ArcApplicationBuilder CreateBuilder(string[]? args = null) => new(args);

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var action in _startupActions)
        {
            await action(Services);
        }

        await _host.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        return _host.StopAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _host.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_host is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _host.Dispose();
        }
    }

    /// <summary>
    /// Adds a startup action to be executed when the application starts.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    internal void AddStartupAction(Func<IServiceProvider, Task> action)
    {
        _startupActions.Add(action);
    }

    string[] GetHttpPrefixes() => Options.Hosting.ApplicationUrl
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(prefix => prefix.EndsWith('/') ? prefix : $"{prefix}/")
                .ToArray();
}
