// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Commands;
using Cratis.Arc.EndpointGroups;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Arc.Introspection;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc;

/// <summary>
/// Extension methods for <see cref="ArcApplication"/>.
/// </summary>
public static class ArcApplicationExtensions
{
    /// <summary>
    /// Activates Cratis Arc on the <see cref="ArcApplication"/>, mapping the identity, introspection, command,
    /// and query endpoints. Calling it more than once is a no-op. This activates Arc only; when you use event
    /// sourcing, also activate the Chronicle client.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    /// <exception cref="InvalidIntrospectionConfiguration">Discovery is enabled and authentication is explicitly required, but no handler is configured.</exception>
    public static ArcApplication UseCratisArc(this ArcApplication app)
    {
        if (app.IsCratisArcConfigured)
        {
            return app;
        }

        var introspection = app.Services.GetRequiredService<IOptions<ArcOptions>>().Value.Introspection;
        if ((introspection.Enabled || introspection.IdentityDiscovery) && introspection.AuthenticationExplicitlyRequired)
        {
            var scope = app.Services.CreateAsyncScope();
            try
            {
                if (!scope.ServiceProvider.GetRequiredService<IAuthentication>().HasHandlers)
                {
                    throw new InvalidIntrospectionConfiguration("Requiring authentication on the discovery endpoints needs an Arc.Core authentication handler.");
                }
            }
            finally
            {
                scope.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }

        RefuseEndpointGroups(app.Services);

        app.EndpointMapper.MapIdentityProviderEndpoint(app.Services);
        app.EndpointMapper.MapIntrospectionEndpoints(introspection, app.Services.GetService<ILoggerFactory>()?.CreateLogger(typeof(IntrospectionEndpointMapper).FullName!));
        app.EndpointMapper.MapCommandEndpoints(app.Services);
        app.EndpointMapper.MapQueryEndpoints(app.Services);
        app.EndpointMapper.MapObservableQueryDemultiplexerEndpoints(app.Services);

        app.AddStartupAction(serviceProvider =>
        {
            if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
            {
                httpListenerEndpointMapper.Start(serviceProvider);
            }
            return Task.CompletedTask;
        });

        var applicationLifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        applicationLifetime.ApplicationStopping.Register(() =>
        {
            if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
            {
                httpListenerEndpointMapper.Stop().GetAwaiter().GetResult();
                httpListenerEndpointMapper.Dispose();
            }
        });

        app.MarkCratisArcAsConfigured();

        return app;
    }

    /// <summary>
    /// Configures a base path for all endpoints being mapped.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <param name="pathBase">The base path to use for all endpoints.</param>
    /// <returns>The <see cref="ArcApplication"/> for continuation.</returns>
    public static ArcApplication UsePathBase(this ArcApplication app, string pathBase)
    {
        if (app.EndpointMapper is HttpListenerEndpointMapper httpListenerEndpointMapper)
        {
            httpListenerEndpointMapper.SetPathBase(pathBase);
        }
        return app;
    }

    /// <summary>
    /// Runs the application and blocks until shutdown.
    /// </summary>
    /// <param name="app">The <see cref="ArcApplication"/>.</param>
    /// <returns>A task representing the application lifetime.</returns>
    public static async Task RunAsync(this ArcApplication app)
    {
        await app.StartAsync();

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        await WaitForShutdown(lifetime);

        await app.StopAsync();
    }

    static Task WaitForShutdown(IHostApplicationLifetime lifetime)
    {
        var tcs = new TaskCompletionSource();
        lifetime.ApplicationStopping.Register(() => tcs.TrySetResult());
        return tcs.Task;
    }

    /// <summary>
    /// Refuses named endpoint groups, because the HTTP listener host cannot restrict an endpoint to a local port.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> to read <see cref="ArcOptions"/> from.</param>
    static void RefuseEndpointGroups(IServiceProvider services)
    {
        const string host = "the Arc.Core HTTP listener host";
        if (services.GetRequiredService<IOptions<ArcOptions>>().Value.EndpointGroups.HasGroups)
        {
            throw new EndpointGroupsNotSupportedByHost(host);
        }

        var commandTypes = services.GetRequiredService<ICommandHandlerProviders>().Handlers.Select(_ => _.CommandType);
        var queryTypes = services.GetRequiredService<IQueryPerformerProviders>().Performers.Select(_ => _.Type);
        if (commandTypes.Concat(queryTypes).Any(_ => EndpointGroup.For(_) is not null))
        {
            throw new EndpointGroupsNotSupportedByHost(host);
        }
    }
}
