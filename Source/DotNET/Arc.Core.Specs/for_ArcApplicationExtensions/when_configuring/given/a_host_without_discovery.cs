// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_ArcApplicationExtensions.when_configuring.given;

public class a_host_without_discovery : Specification
{
    static readonly string[] _discoveryPaths = ["/.cratis/commands", "/.cratis/queries", "/.cratis/users", "/.cratis/tenants", "/.cratis/identity-details/schema"];

    protected ArcApplication _app;
    protected IAuthentication _authentication;
    protected Action<ArcOptions> _configureAccess = _ => { };

    protected void BuildHost()
    {
        var builder = new ArcApplicationBuilder(["--environment", "Production"]);
        builder.AddCratisArc(options =>
        {
            options.Introspection.Enabled = false;
            options.Introspection.IdentityDiscovery = false;
            options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider);
            _configureAccess(options);
        });
        _authentication = Substitute.For<IAuthentication>();
        _authentication.HasHandlers.Returns(false);
        builder.Services.AddSingleton(_authentication);
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns([]);
        builder.Services.AddSingleton(commands);
        var queries = Substitute.For<IQueryPerformerProviders>();
        queries.Performers.Returns([]);
        builder.Services.AddSingleton(queries);
        _app = builder.Build();
    }

    protected IEnumerable<string> DiscoveryRoutes => ((HttpListenerEndpointMapper)_app.EndpointMapper).Routes
        .Select(route => route.Pattern)
        .Where(_discoveryPaths.Contains);

    protected bool HasCurrentCallerIdentity => ((HttpListenerEndpointMapper)_app.EndpointMapper).Routes.Any(route => route.Pattern == "/.cratis/me");

    async Task Destroy()
    {
        ((HttpListenerEndpointMapper)_app.EndpointMapper).Dispose();
        await _app.DisposeAsync();
    }
}
