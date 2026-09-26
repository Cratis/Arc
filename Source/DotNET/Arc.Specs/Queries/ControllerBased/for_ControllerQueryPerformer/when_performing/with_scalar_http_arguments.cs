// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ControllerBased.for_ControllerQueryPerformer.when_performing;

public class with_scalar_http_arguments : Specification
{
    public enum Status
    {
        Active = 0,
        Inactive = 1
    }

    public record ScalarModel
    {
        public static bool WasCalled { get; set; }

        public static int Query(int count, Guid id, DateOnly date, Status state, bool enabled)
        {
            WasCalled = true;
            return count;
        }
    }

    public class ScalarController : ControllerBase
    {
        public static bool WasCalled { get; set; }

        [HttpGet]
        public int Query(int count, Guid id, DateOnly date, Status state, bool enabled)
        {
            WasCalled = true;
            return count;
        }
    }

    static IQueryPerformer Performer(bool controller, IServiceProviderIsService isService, IAuthorizationEvaluator evaluator) =>
        controller
            ? new ControllerQueryPerformer(
                new ControllerActionDescriptor
                {
                    ActionName = nameof(ScalarController.Query),
                    ControllerName = nameof(ScalarController),
                    ControllerTypeInfo = typeof(ScalarController).GetTypeInfo(),
                    MethodInfo = typeof(ScalarController).GetMethod(nameof(ScalarController.Query))!
                },
                isService,
                evaluator)
            : new ModelBoundQueryPerformer(
                typeof(ScalarModel),
                typeof(ScalarModel).FullName!,
                typeof(ScalarModel).GetMethod(nameof(ScalarModel.Query))!,
                isService,
                evaluator);

    static async Task<(int Status, JsonDocument Response, IQueryPipeline Pipeline)> Invoke(bool controller, string method, string name, string value)
    {
        await using var classificationServices = new ServiceCollection().BuildServiceProvider();
        var performer = Performer(controller, classificationServices.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        var pipeline = Substitute.For<IQueryPipeline>();
        pipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(QueryResult.Success(CorrelationId.New())));

        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ArcOptions>(options => options.GeneratedApis.SegmentsToSkipForRoute = 0);
        builder.Services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());
        builder.Services.AddSingleton(correlationIdAccessor);
        builder.Services.AddSingleton<IInstancesOf<IQueryRequestReader>>(
            new KnownInstancesOf<IQueryRequestReader>([new QueryStringQueryRequestReader(), new BodyQueryRequestReader()]));
        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.Performers.Returns([performer]);
        builder.Services.AddSingleton(providers);
        builder.Services.AddSingleton(pipeline);
        builder.Services.AddSingleton(Substitute.For<IObservableQueryHandler>());

        await using var app = builder.Build();
        new AspNetCoreEndpointMapper(app).MapQueryEndpoints(app.Services);
        var prefix = method == "GET" ? "Execute" : "Query";
        var endpoint = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == $"{prefix}{performer.FullyQualifiedName}");
        var httpContext = new DefaultHttpContext { RequestServices = app.Services };
        httpContext.Request.Method = method;
        httpContext.Request.QueryString = method == "GET" ? new QueryString($"?{name}={Uri.EscapeDataString(value)}") : QueryString.Empty;
        httpContext.Response.Body = new MemoryStream();
        if (method == "QUERY")
        {
            httpContext.Request.ContentType = "application/json";
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                arguments = new Dictionary<string, string> { [name] = value }
            })));
        }

        await endpoint.RequestDelegate!.Invoke(httpContext);
        httpContext.Response.Body.Position = 0;
        return (httpContext.Response.StatusCode, await JsonDocument.ParseAsync(httpContext.Response.Body), pipeline);
    }

    [Theory]
    [InlineData(false, "GET")]
    [InlineData(false, "QUERY")]
    [InlineData(true, "GET")]
    [InlineData(true, "QUERY")]
    async Task should_return_http_400_with_named_validation_and_no_invocation(bool controller, string method)
    {
        foreach (var (name, invalid) in new[]
        {
            ("count", "abc"), ("id", "not-a-guid"), ("date", "not-a-date"),
            ("state", "not-a-status"), ("enabled", "not-a-bool")
        })
        {
            ScalarModel.WasCalled = false;
            ScalarController.WasCalled = false;
            var (status, response, pipeline) = await Invoke(controller, method, name, invalid);
            using (response)
            {
                status.ShouldEqual(400);
                var validation = response.RootElement.GetProperty("validationResults").EnumerateArray().Single();
                validation.GetProperty("members").EnumerateArray().Single().GetString().ShouldEqual(name);
                response.RootElement.GetProperty("exceptionMessages").GetArrayLength().ShouldEqual(0);
            }
            await pipeline.DidNotReceive().Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
            ScalarModel.WasCalled.ShouldBeFalse();
            ScalarController.WasCalled.ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData(false, "GET")]
    [InlineData(false, "QUERY")]
    [InlineData(true, "GET")]
    [InlineData(true, "QUERY")]
    async Task should_return_http_200_for_valid_arguments(bool controller, string method)
    {
        var (status, response, pipeline) = await Invoke(controller, method, "count", "42");
        using (response)
        {
            status.ShouldEqual(200);
        }
        await pipeline.Received(1).Perform(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Is<QueryArguments>(arguments => (int)arguments["count"] == 42),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    void should_reject_invalid_values_from_the_aspnet_query_argument_helper()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var performer = Performer(true, services.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString("?count=abc");
        var error = Catch.Exception(() => context.GetQueryArguments(performer));
        (error is InvalidQueryArgument).ShouldBeTrue();
        ((InvalidQueryArgument)error).ValidationResult.Members.ShouldContainOnly("count");
    }

    [Fact]
    async Task should_reject_invalid_controller_argument_on_direct_invocation()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        ScalarController.WasCalled = false;
        var performer = Performer(true, services.GetRequiredService<IServiceProviderIsService>(), Substitute.For<IAuthorizationEvaluator>());
        var context = new QueryContext(
            performer.FullyQualifiedName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments { ["count"] = "abc" },
            [services]);
        var error = await Catch.Exception(async () => await performer.Perform(context));
        (error is InvalidQueryArgument).ShouldBeTrue();
        ((InvalidQueryArgument)error).ValidationResult.Members.ShouldContainOnly("count");
        ScalarController.WasCalled.ShouldBeFalse();
    }
}
