// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_api_discoverability;

public class with_command_and_query_endpoints : Specification
{
    record TestCommand;
    record TestReadModel(string Name);

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
            o.GeneratedApis.IncludeQueryNameInRoute = true;
        });

        var commandHandler = Substitute.For<ICommandHandler>();
        commandHandler.Location.Returns(["Features", "Orders"]);
        commandHandler.CommandType.Returns(typeof(TestCommand));
        commandHandler.Dependencies.Returns([]);
        commandHandler.AllowsAnonymousAccess.Returns(false);

        var commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        commandHandlerProviders.Handlers.Returns([commandHandler]);
        builder.Services.AddSingleton(commandHandlerProviders);

        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.Name.Returns(new QueryName("AllOrders"));
        queryPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Features.Orders.AllOrders"));
        queryPerformer.Location.Returns(["Features", "Orders"]);
        queryPerformer.Type.Returns(typeof(TestReadModel));
        queryPerformer.ReadModelType.Returns(typeof(TestReadModel));
        queryPerformer.Dependencies.Returns([]);
        queryPerformer.Parameters.Returns(QueryParameters.Empty);
        queryPerformer.AllowsAnonymousAccess.Returns(false);

        var queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        queryPerformerProviders.Performers.Returns([queryPerformer]);
        builder.Services.AddSingleton(queryPerformerProviders);

        _app = builder.Build();

        _mapper = new AspNetCoreEndpointMapper(_app);
        _mapper.MapCommandEndpoints(_app.Services);
        _mapper.MapQueryEndpoints(_app.Services);

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

    [Fact] void should_have_api_descriptions() => _apiDescriptions.Count.ShouldBeGreaterThan(0);
    [Fact] void should_have_three_api_descriptions() => _apiDescriptions.Count.ShouldEqual(3);
    [Fact] void should_have_command_execute_description() => _apiDescriptions.Any(d => d.HttpMethod == "POST" && d.RelativePath.Contains("test-command")).ShouldBeTrue();
    [Fact] void should_have_command_validate_description() => _apiDescriptions.Any(d => d.HttpMethod == "POST" && d.RelativePath.Contains("validate")).ShouldBeTrue();
    [Fact] void should_have_query_description() => _apiDescriptions.Any(d => d.HttpMethod == "GET" && d.RelativePath.Contains("all-orders")).ShouldBeTrue();
    [Fact] void should_have_request_body_parameter_on_command() => _apiDescriptions.First(d => d.HttpMethod == "POST" && d.RelativePath.Contains("test-command") && !d.RelativePath.Contains("validate")).ParameterDescriptions.Any(p => p.Source.Id == "Body").ShouldBeTrue();
    [Fact] void should_have_response_type_on_command() => _apiDescriptions.First(d => d.HttpMethod == "POST" && d.RelativePath.Contains("test-command") && !d.RelativePath.Contains("validate")).SupportedResponseTypes.Any(r => r.StatusCode == 200).ShouldBeTrue();
    [Fact] void should_have_response_type_on_query() => _apiDescriptions.First(d => d.HttpMethod == "GET" && d.RelativePath.Contains("all-orders")).SupportedResponseTypes.Any(r => r.StatusCode == 200).ShouldBeTrue();

    void Destroy()
    {
        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
