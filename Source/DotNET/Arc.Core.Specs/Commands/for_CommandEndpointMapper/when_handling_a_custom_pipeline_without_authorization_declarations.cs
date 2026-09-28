// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper;

public class when_handling_a_custom_pipeline_without_authorization_declarations : Specification
{
    ICommandPipeline _pipeline;
    int[] _statusCodes;
    CommandResult[] _results;

    async Task Because()
    {
        _pipeline = Substitute.For<ICommandPipeline>();
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
            .AddSingleton(_pipeline)
            .AddSingleton(Options.Create(new ArcOptions()))
            .AddSingleton(correlationIdAccessor)
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

    [Fact] void should_report_forbidden_for_execute_and_validate() => _statusCodes.SequenceEqual([403, 403]).ShouldBeTrue();
    [Fact] void should_report_unauthorized_without_an_error() => _results.All(result => !result.IsAuthorized && !result.HasExceptions).ShouldBeTrue();
    [Fact] void should_not_execute_the_custom_pipeline() => _pipeline.DidNotReceive().Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<Cratis.Arc.Validation.ValidationResultSeverity?>());
    [Fact] void should_not_validate_using_the_custom_pipeline() => _pipeline.DidNotReceive().Validate(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<Cratis.Arc.Validation.ValidationResultSeverity?>());

    public record ProtectedCommand;
}
