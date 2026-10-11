// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class and_a_command_fails : given.a_mixed_result_handler
{
    ICommandPipeline _pipeline;
    IServiceProvider _provider;
    TestCommand _first;
    TestCommand _failed;
    TestCommand _later;

    void Establish()
    {
        _pipeline = Substitute.For<ICommandPipeline>();
        _provider = Substitute.For<IServiceProvider>();
        _provider.GetService(typeof(ICommandPipeline)).Returns(_pipeline);
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_provider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);
        _handler = new(new CommandSideEffectExecutor(scopeFactory, Substitute.For<ISystemExecution>()));
        _first = new("first");
        _failed = new("failed");
        _later = new("later");
        _pipeline.Execute(_first, _provider).Returns(_ =>
        {
            _order.Add("first");
            return CommandResult.Success(CorrelationId.New());
        });
        _pipeline.Execute(_failed, _provider).Returns(_ =>
        {
            _order.Add("failed");
            return new CommandResult { CorrelationId = CorrelationId.New(), IsAuthorized = false };
        });
    }

    async Task Because() => _result = await _handler.Handle(_context, _store, new List<object> { _first, new TestEvent("retained"), _failed, _later });

    [Fact] void should_fail_the_reactor() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_append_then_execute_commands_until_the_failure() => _order.SequenceEqual(["append", "first", "failed"]).ShouldBeTrue();
    [Fact] void should_retain_the_appended_event_batch() => _appended.Select(envelope => envelope.Event).ShouldContainOnly(new TestEvent("retained"));
    [Fact] void should_not_execute_later_commands() => _pipeline.DidNotReceive().Execute(_later, _provider);
    [Fact] void should_not_attempt_compensation_or_another_append() => _eventLog.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "AppendMany").ShouldEqual(1);
}
