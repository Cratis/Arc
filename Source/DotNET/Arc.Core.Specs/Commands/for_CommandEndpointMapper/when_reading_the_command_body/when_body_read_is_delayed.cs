// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Queries.for_QueryEndpointMapper.given;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Commands.for_CommandEndpointMapper.when_reading_the_command_body;

public class when_body_read_is_delayed : Specification
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _duringBodyRead;
    DateTimeOffset? _duringExecution;
    DateTimeOffset? _after;

    async Task Because()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received);
        var pipeline = Substitute.For<ICommandPipeline>();
        pipeline.Execute(Arg.Any<object>(), Arg.Any<IServiceProvider>(), Arg.Any<Cratis.Arc.Validation.ValidationResultSeverity?>())
            .Returns(_ =>
            {
                // A decorator forwards to the built-in pipeline's public entry.
                using var forwarded = OperationContextScope.BeginPipeline(_.ArgAt<IServiceProvider>(1));
                _duringExecution = new OperationContextAccessor().ReceivedAt;
                return Task.FromResult(CommandResult.Success(CorrelationId.New()));
            });
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(SomeCommand));
        handler.Location.Returns(["Features", "Commands"]);
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([handler]);
        var mapper = new a_recording_endpoint_mapper();
        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton(pipeline)
            .AddSingleton(Options.Create(new ArcOptions()))
            .AddSingleton(correlationIdAccessor)
            .BuildServiceProvider();
        var mappingServices = new ServiceCollection()
            .AddSingleton(handlers)
            .AddSingleton(Options.Create(new ArcOptions()))
            .BuildServiceProvider();
        mapper.MapCommandEndpoints(mappingServices);
        var context = Substitute.For<IHttpRequestContext>();
        context.RequestServices.Returns(services);
        context.Headers.Returns(new Dictionary<string, string>());
        context.ReadBodyAsJson(typeof(SomeCommand), Arg.Any<CancellationToken>()).Returns(_ => ReadBody());

        await mapper.HandlerFor("POST")(context);
        _after = new OperationContextAccessor().ReceivedAt;

        async Task<object?> ReadBody()
        {
            _duringBodyRead = new OperationContextAccessor().ReceivedAt;
            clock.GetUtcNow().Returns(_received.AddMinutes(1));
            await Task.Yield();
            return new SomeCommand(42, "ok");
        }
    }

    [Fact] void should_have_the_receipt_during_body_read() => _duringBodyRead.ShouldEqual(_received);
    [Fact] void should_preserve_the_pre_body_receipt_through_a_decorator() => _duringExecution.ShouldEqual(_received);
    [Fact] void should_clear_the_receipt_after_dispatch() => _after.ShouldBeNull();
}
