// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing.given;

public abstract class a_nested_collection_endpoint : Specification
{
    protected ControllerQueryPerformer _performer;
    protected DefaultHttpContext _httpContext;
    protected JsonDocument _response;
    WebApplication _app;

    void Establish()
    {
        NestedCollectionController.WasCalled = false;
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ArcOptions>(options => options.GeneratedApis.SegmentsToSkipForRoute = 0);
        builder.Services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.NotSet);
        builder.Services.AddSingleton(correlationIdAccessor);
        builder.Services.AddSingleton<IInstancesOf<IQueryRequestReader>>(
            new KnownInstancesOf<IQueryRequestReader>([new QueryStringQueryRequestReader(), new BodyQueryRequestReader()]));

        var descriptor = new ControllerActionDescriptor
        {
            ActionName = nameof(NestedCollectionController.ByValues),
            ControllerName = nameof(NestedCollectionController),
            ControllerTypeInfo = typeof(NestedCollectionController).GetTypeInfo(),
            MethodInfo = typeof(NestedCollectionController).GetMethod(nameof(NestedCollectionController.ByValues))!
        };
        using var classificationServices = new ServiceCollection().BuildServiceProvider();
        _performer = new ControllerQueryPerformer(
            descriptor,
            classificationServices.GetRequiredService<IServiceProviderIsService>(),
            Substitute.For<IAuthorizationEvaluator>());
        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.Performers.Returns([_performer]);
        builder.Services.AddSingleton(providers);

        _app = builder.Build();
        new AspNetCoreEndpointMapper(_app).MapQueryEndpoints(_app.Services);
    }

    protected async Task Invoke(string method, string endpointPrefix, string queryString, string? body = null)
    {
        var endpoint = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == $"{endpointPrefix}{_performer.FullyQualifiedName}");
        _httpContext = new DefaultHttpContext
        {
            RequestServices = _app.Services
        };
        _httpContext.Request.Method = method;
        _httpContext.Request.QueryString = new QueryString(queryString);
        _httpContext.Response.Body = new MemoryStream();
        if (body is not null)
        {
            _httpContext.Request.ContentType = "application/json";
            _httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }

        await endpoint.RequestDelegate!.Invoke(_httpContext);
        _httpContext.Response.Body.Position = 0;
        _response = await JsonDocument.ParseAsync(_httpContext.Response.Body);
    }

    void Destroy()
    {
        _response?.Dispose();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public class NestedCollectionController : ControllerBase
    {
        public static bool WasCalled { get; set; }

        [HttpGet]
        public int ByValues(IEnumerable<IEnumerable<int>> values)
        {
            WasCalled = true;
            return values.Count();
        }
    }
}
