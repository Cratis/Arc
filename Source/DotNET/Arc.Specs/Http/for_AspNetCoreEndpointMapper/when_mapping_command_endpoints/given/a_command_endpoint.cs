// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_command_endpoints.given;

public class a_command_endpoint : Specification
{
    protected WebApplication _app;
    protected AspNetCoreEndpointMapper _mapper;
    protected IEndpointRouteBuilder _routeBuilder;
    protected ICommandHandlerProviders _commandHandlerProviders;

    protected record TestCommand;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ArcOptions>(o =>
        {
            o.GeneratedApis.SegmentsToSkipForRoute = 0;
            o.GeneratedApis.IncludeCommandNameInRoute = true;
        });

        var commandHandler = Substitute.For<ICommandHandler>();
        commandHandler.Location.Returns(["Features", "Orders"]);
        commandHandler.CommandType.Returns(typeof(TestCommand));
        commandHandler.Dependencies.Returns([]);
        commandHandler.AllowsAnonymousAccess.Returns(false);

        _commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        _commandHandlerProviders.Handlers.Returns([commandHandler]);
        builder.Services.AddSingleton(_commandHandlerProviders);

        _app = builder.Build();
        _routeBuilder = _app;
        _mapper = new AspNetCoreEndpointMapper(_routeBuilder);
    }

    protected IReadOnlyList<Endpoint> GetEndpoints()
    {
        return _routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .ToList();
    }

    protected IEnumerable<RouteEndpoint> GetRouteEndpoints()
    {
        return GetEndpoints().OfType<RouteEndpoint>();
    }

    protected RouteEndpoint? FindEndpointByName(string name)
    {
        return GetRouteEndpoints()
            .FirstOrDefault(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);
    }

    void Destroy()
    {
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
