// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace Cratis.Arc.Swagger.for_generated_openapi_document;

/// <summary>
/// Verifies that registering a query with both its GET and QUERY endpoints still produces a valid OpenAPI
/// document — the QUERY endpoint is excluded from the description so Swashbuckle never encounters the
/// unsupported verb, and the query stays documented via GET.
/// </summary>
public class when_generating_with_a_query : Specification
{
    record TestReadModel(string Name);

    WebApplication _app;
    OpenApiDocument _document;
    Exception _exception;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Test", Version = "v1" });
            options.AddModelBoundOperationFilters();
        });
        builder.Services.Configure<ArcOptions>(o =>
        {
            o.GeneratedApis.SegmentsToSkipForRoute = 0;
            o.GeneratedApis.IncludeQueryNameInRoute = true;
        });

        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName("AllOrders"));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Features.Orders.AllOrders"));
        performer.Location.Returns(["Features", "Orders"]);
        performer.Type.Returns(typeof(TestReadModel));
        performer.ReadModelType.Returns(typeof(TestReadModel));
        performer.Dependencies.Returns([]);
        performer.Parameters.Returns(QueryParameters.Empty);
        performer.AllowsAnonymousAccess.Returns(false);

        var performerProviders = Substitute.For<IQueryPerformerProviders>();
        performerProviders.Performers.Returns([performer]);
        builder.Services.AddSingleton(performerProviders);

        var commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        commandHandlerProviders.Handlers.Returns([]);
        builder.Services.AddSingleton(commandHandlerProviders);

        builder.Services.AddSingleton<IInstancesOf<IQueryRequestReader>>(
            new KnownInstancesOf<IQueryRequestReader>([new QueryStringQueryRequestReader(), new BodyQueryRequestReader()]));

        _app = builder.Build();

        var mapper = new AspNetCoreEndpointMapper(_app);
        mapper.MapQueryEndpoints(_app.Services);

        _app.StartAsync().GetAwaiter().GetResult();
    }

    void Because()
    {
        try
        {
            _document = _app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }

    [Fact] void should_generate_the_document_without_error() => _exception.ShouldBeNull();
    [Fact] void should_populate_a_path_for_the_query() => _document.Paths.Keys.Any(key => key.Contains("all-orders", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();

    void Destroy()
    {
        _app?.StopAsync().GetAwaiter().GetResult();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
