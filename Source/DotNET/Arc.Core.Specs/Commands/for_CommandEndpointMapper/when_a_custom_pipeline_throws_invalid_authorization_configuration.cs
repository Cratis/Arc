// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_a_custom_pipeline_throws_invalid_authorization_configuration : Specification
{
    int[] _statusCodes;
    CommandResult[] _results;

    async Task Because()
    {
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns<Task<CommandResult>>(_ => throw new InvalidAuthorizationConfiguration("Raised by the custom command pipeline."));
        pipeline.Validate(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns<Task<CommandResult>>(_ => throw new InvalidAuthorizationConfiguration("Raised by the custom command pipeline."));
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(ProtectedCommand));
        handler.Location.Returns(["Features", "Commands"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var mapper = new a_recording_endpoint_mapper();
        mapper.MapCommandEndpoints(new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider());
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(pipeline)
            .AddSingleton(Options.Create(new ArcOptions()))
            .AddSingleton(correlationIdAccessor)
            .AddSingleton(new AuthorizationDeclarations(
                new KnownInstancesOf<IAnonymousEvaluator>([]),
                new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .BuildServiceProvider();
        var statuses = new List<int>();
        var results = new List<CommandResult>();
        foreach (var endpoint in mapper.Mapped)
        {
            var context = Substitute.For<IHttpRequestContext>();
            context.RequestServices.Returns(services);
            context.Headers.Returns(new Dictionary<string, string>());
            context.ReadBodyAsJson(typeof(ProtectedCommand), Arg.Any<CancellationToken>()).Returns(new ProtectedCommand());
            context.When(c => c.SetStatusCode(Arg.Any<int>())).Do(call => statuses.Add(call.Arg<int>()));
            context.WriteResponseAsJson(Arg.Any<object?>(), Arg.Any<Type>(), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    results.Add((CommandResult)call.ArgAt<object>(0));
                    return Task.CompletedTask;
                });
            await endpoint.Handler(context);
        }

        _statusCodes = [.. statuses];
        _results = [.. results];
    }

    [Fact] void should_report_server_error_for_execute_and_validate() => _statusCodes.SequenceEqual([500, 500]).ShouldBeTrue();
    [Fact] void should_report_an_error_not_a_denial() => _results.All(result => result.IsAuthorized && result.HasExceptions).ShouldBeTrue();

    public record ProtectedCommand;
}
