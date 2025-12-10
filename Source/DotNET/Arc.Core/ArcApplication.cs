// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc;

/// <summary>
/// Represents an Arc application.
/// </summary>
/// <param name="host">The underlying <see cref="IHost"/>.</param>
public class ArcApplication(IHost host) : IHost, IAsyncDisposable
{
    readonly IHost _host = host;
    readonly List<Func<IServiceProvider, Task>> _startupActions = [];
    IEndpointMapper? _endpointMapper;

    /// <inheritdoc/>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Gets the endpoint mapper for this application.
    /// </summary>
    internal IEndpointMapper? EndpointMapper => _endpointMapper;

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

    /// <summary>
    /// Sets the endpoint mapper for this application.
    /// </summary>
    /// <param name="mapper">The endpoint mapper.</param>
    internal void SetEndpointMapper(IEndpointMapper mapper)
    {
        _endpointMapper = mapper;
    }
}
