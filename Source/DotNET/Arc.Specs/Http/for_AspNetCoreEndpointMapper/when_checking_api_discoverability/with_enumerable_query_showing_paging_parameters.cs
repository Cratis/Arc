// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.when_checking_api_discoverability;

public class with_enumerable_query_showing_paging_parameters : Specification
{
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
            o.GeneratedApis.IncludeQueryNameInRoute = true;
        });

        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.Name.Returns(new QueryName("AllOrders"));
        queryPerformer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Features.Orders.AllOrders"));
        queryPerformer.Location.Returns(["Features", "Orders"]);
        queryPerformer.Type.Returns(typeof(TestReadModel));
        queryPerformer.ReadModelType.Returns(typeof(TestReadModel));
        queryPerformer.Dependencies.Returns([]);
        queryPerformer.Parameters.Returns(QueryParameters.Empty);
        queryPerformer.AllowsAnonymousAccess.Returns(false);
        queryPerformer.IsEnumerableResult.Returns(true);

        var queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        queryPerformerProviders.Performers.Returns([queryPerformer]);
        builder.Services.AddSingleton(queryPerformerProviders);

        // Register with empty command handlers to avoid DI issues
        var commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        commandHandlerProviders.Handlers.Returns([]);
        builder.Services.AddSingleton(commandHandlerProviders);

        _app = builder.Build();

        _mapper = new AspNetCoreEndpointMapper(_app);
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

    [Fact] void should_have_one_api_description() => _apiDescriptions.Count.ShouldEqual(1);
    [Fact] void should_have_get_method() => _apiDescriptions[0].HttpMethod.ShouldEqual("GET");
    [Fact] void should_have_query_endpoint() => _apiDescriptions.Any(d => d.RelativePath.Contains("all-orders")).ShouldBeTrue();

    void Destroy()
    {
        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
