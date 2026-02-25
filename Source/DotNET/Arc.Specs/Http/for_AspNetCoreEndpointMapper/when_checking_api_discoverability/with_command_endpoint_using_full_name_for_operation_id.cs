// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_api_discoverability;

public class with_command_endpoint_using_full_name_for_operation_id : Specification
{
    record TestCommand(string Value);

    WebApplication _app;
    AspNetCoreEndpointMapper _mapper;
    IReadOnlyList<ApiDescription> _apiDescriptions;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddEndpointsApiExplorer();
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

        var commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        commandHandlerProviders.Handlers.Returns([commandHandler]);
        builder.Services.AddSingleton(commandHandlerProviders);

        _app = builder.Build();

        _mapper = new AspNetCoreEndpointMapper(_app);
        _mapper.MapCommandEndpoints(_app.Services);

        _app.StartAsync().GetAwaiter().GetResult();
    }

    void Because()
    {
        var apiDescriptionProvider = _app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        _apiDescriptions = apiDescriptionProvider
            .ApiDescriptionGroups
            .Items
            .SelectMany(g => g.Items)
            .ToList();
    }

    [Fact] void should_have_two_api_descriptions() => _apiDescriptions.Count.ShouldEqual(2);

    [Fact]
    void should_use_full_name_in_execute_operation_id()
    {
        var executeDescription = _apiDescriptions.First(d => d.HttpMethod == "POST" && !d.RelativePath.Contains("validate"));
        executeDescription.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>()
            .First().EndpointName
            .ShouldEqual($"Execute{typeof(TestCommand).FullName}");
    }

    [Fact]
    void should_use_full_name_in_validate_operation_id()
    {
        var validateDescription = _apiDescriptions.First(d => d.HttpMethod == "POST" && d.RelativePath.Contains("validate"));
        validateDescription.ActionDescriptor.EndpointMetadata
            .OfType<Microsoft.AspNetCore.Routing.IEndpointNameMetadata>()
            .First().EndpointName
            .ShouldEqual($"Validate{typeof(TestCommand).FullName}");
    }

    void Destroy()
    {
        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
