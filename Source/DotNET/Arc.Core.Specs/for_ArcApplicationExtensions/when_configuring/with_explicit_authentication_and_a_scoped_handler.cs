// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.for_ArcApplicationExtensions.when_configuring;

[Collection("UsesCurrentDirectory")]
public class with_explicit_authentication_and_a_scoped_handler : Introspection.for_DiscoveryExposure.given.a_host_with_scoped_authentication
{
    ArcApplication _app;

    void Establish()
    {
        _registrations.Configure<ArcOptions>(options => options.Introspection.RequireAuthentication = true);
        _registrations.AddSingleton(Substitute.For<IHostApplicationLifetime>());
        var commands = Substitute.For<ICommandHandlerProviders>();
        commands.Handlers.Returns([]);
        _registrations.AddSingleton(commands);
        var queries = Substitute.For<IQueryPerformerProviders>();
        queries.Performers.Returns([]);
        _registrations.AddSingleton(queries);
        _registrations.AddSingleton(Substitute.For<IInstancesOf<IQueryRequestReader>>());
        Build();
        var host = Substitute.For<IHost>();
        host.Services.Returns(_services);
        _app = new ArcApplication(host, new ArcOptions());
    }

    void Because() => _app.UseCratisArc();

    [Fact] void should_activate_arc() => _app.IsCratisArcConfigured.ShouldBeTrue();
    [Fact] void should_construct_scoped_handlers() => _handlers.ShouldNotBeEmpty();
    [Fact] void should_dispose_scoped_handlers_asynchronously() => _handlers.TrueForAll(handler => handler.Disposed).ShouldBeTrue();
    [Fact] void should_map_authenticated_discovery() => ((HttpListenerEndpointMapper)_app.EndpointMapper).Routes.Single(route => route.Pattern == "/.cratis/commands").Metadata!.RequireAuthentication.ShouldBeTrue();

    void Destroy()
    {
        ((HttpListenerEndpointMapper)_app.EndpointMapper).Dispose();
        _app.Dispose();
    }
}
