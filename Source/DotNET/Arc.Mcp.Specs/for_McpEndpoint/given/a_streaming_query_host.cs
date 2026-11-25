// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ControllerBased;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class a_streaming_query_host : an_mcp_host
{
    protected readonly ToolGate _gate = new();
    protected static readonly string[] StreamingTools =
    [
        "stream", "subject", "stream_task", "subject_task",
        nameof(WrappedStreamingQueriesController.StreamValueTask), nameof(WrappedStreamingQueriesController.SubjectValueTask),
        nameof(WrappedStreamingQueriesController.StreamResult), nameof(WrappedStreamingQueriesController.SubjectResult),
        nameof(WrappedStreamingQueriesController.StreamResultTask), nameof(WrappedStreamingQueriesController.SubjectResultTask),
        nameof(WrappedStreamingQueriesController.StreamResultValueTask), nameof(WrappedStreamingQueriesController.SubjectResultValueTask)
    ];

    protected override void ConfigureMcp(McpBuilder mcp)
    {
        mcp.AddToolAuthorizer<DenyingAuthorizer>()
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.Stream), "stream", "Streams rows.")
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.Subject), "subject", "Observes rows.")
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.StreamTask), "stream_task", "Streams rows asynchronously.")
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.SubjectTask), "subject_task", "Observes rows asynchronously.");

        foreach (var method in ControllerMethods())
        {
            mcp.ExposeQuery<WrappedStreamingQueriesController>(method.Name, method.Name.ToLowerInvariant(), "Returns wrapped rows.");
        }
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_gate);
        services.AddSingleton<IInstancesOf<IQueryPerformerProvider>>(provider =>
        {
            // This host disables controller discovery; register real controller performers explicitly for these shapes.
            var queries = Substitute.For<IQueryPerformerProvider>();
            var controllers = ControllerMethods().Select(method => new ControllerQueryPerformer(
                new ControllerActionDescriptor
                {
                    ActionName = method.Name,
                    ControllerName = nameof(WrappedStreamingQueriesController),
                    ControllerTypeInfo = typeof(WrappedStreamingQueriesController).GetTypeInfo(),
                    MethodInfo = method
                },
                provider.GetRequiredService<IServiceProviderIsService>(),
                provider.GetRequiredService<IServiceScopeFactory>(),
                scope => scope.GetRequiredService<IAuthorizationEvaluator>()));
            queries.Performers.Returns(provider.GetRequiredService<Queries.ModelBound.QueryPerformerProvider>().Performers
                .Where(performer => performer.ReadModelType.Assembly == typeof(an_mcp_host).Assembly).Concat(controllers).ToArray());
            return new KnownInstancesOf<IQueryPerformerProvider>(queries);
        });
    }

    protected async Task<McpResponse[]> CallStreamingTools()
    {
        var responses = new List<McpResponse>();
        foreach (var name in StreamingTools)
        {
            responses.Add(await CallTool(name.ToLowerInvariant(), "{}"));
        }
        return [.. responses];
    }

    static IEnumerable<MethodInfo> ControllerMethods() => typeof(WrappedStreamingQueriesController)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
}
