// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Execution;
using Cratis.Traces;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_authorization_configuration_failure : given.a_refused_invocation
{
    ReactorSideEffectFailure _failure;
    System.Diagnostics.ActivitySource _activitySource;
    ICommandHandler _handler;

    void Establish()
    {
        var providers = Substitute.For<ICommandHandlerProviders>();
        _handler = Substitute.For<ICommandHandler>();
        providers.TryGetHandlerFor(_command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        var source = Substitute.For<IActivitySource<CommandPipeline>>();
        _activitySource = new("Cratis.Arc.Test");
        source.ActualSource.Returns(_activitySource);
        _commandPipeline = new CommandPipeline(
            Substitute.For<ICorrelationIdAccessor>(),
            Substitute.For<ICommandFilters>(),
            providers,
            Substitute.For<ICommandResponseValueHandlers>(),
            Substitute.For<ICommandContextModifier>(),
            Substitute.For<ICommandContextValuesBuilder>(),
            Substitute.For<ICommandHandlerArgumentResolver>(),
            new KnownInstancesOf<ICommandExecutionScope>([]),
            _serviceScopeFactory,
            source);
        _serviceProvider.GetService(typeof(ICommandPipeline)).Returns(_commandPipeline);
        _invocation = Invoke.Command(_command).OnRefused(Refused.ByAuthorization, _ =>
        {
            _branchesRun++;
            return null;
        });
    }

    async Task Because()
    {
        _result = await _executor.Execute([_invocation, _later], _context, _store);
        _result.TryGetError(out _failure);
    }

    void Cleanup() => _activitySource.Dispose();

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_existing_failure() => _failure.AppendFailures.SelectMany(failure => failure.Errors).ShouldContain("Command 'TestCommand' was not authorized.");
    [Fact] void should_not_acknowledge_a_contract_failure() => _branchesRun.ShouldEqual(0);
    [Fact] void should_not_invoke_any_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());
    [Fact] void should_not_append_any_events() => _appended.ShouldBeEmpty();
}
