// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_mapping_command_endpoints;

public class with_status_code_mapping : Specification
{
    record TestCommand(string Value);

    WebApplication _app;
    IEndpointRouteBuilder _routeBuilder;
    AspNetCoreEndpointMapper _mapper;
    ICommandHandlerProviders _commandHandlerProviders;
    ICommandHandler _commandHandler;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ArcOptions>(o =>
        {
            o.GeneratedApis.SegmentsToSkipForRoute = 0;
            o.GeneratedApis.IncludeCommandNameInRoute = true;
        });

        _commandHandler = Substitute.For<ICommandHandler>();
        _commandHandler.Location.Returns(["Features", "Orders"]);
        _commandHandler.CommandType.Returns(typeof(TestCommand));
        _commandHandler.Dependencies.Returns([]);
        _commandHandler.AllowsAnonymousAccess.Returns(false);

        _commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        _commandHandlerProviders.Handlers.Returns([_commandHandler]);
        builder.Services.AddSingleton(_commandHandlerProviders);

        _app = builder.Build();
        _routeBuilder = _app;
        _mapper = new AspNetCoreEndpointMapper(_routeBuilder);
    }

    void Because() => _mapper.MapCommandEndpoints(_app.Services);

    [Fact]
    void should_register_execute_endpoint()
    {
        var endpoint = _routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == $"Execute{typeof(TestCommand).FullName}");

        endpoint.ShouldNotBeNull();
    }

    [Fact]
    void should_register_validate_endpoint()
    {
        var endpoint = _routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == $"Validate{typeof(TestCommand).FullName}");

        endpoint.ShouldNotBeNull();
    }

    void Destroy()
    {
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
