// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandInvocationResultHandler.when_dispatching;

public class invocations_and_commands : given.an_invocation_dispatcher
{
    TestCommand _first = new("first");
    List<object> _executed = [];

    void Establish()
    {
        _commandPipeline.Execute(_first, _serviceProvider).Returns(_ =>
        {
            _executed.Add(_first);
            return CommandResult.Success(CorrelationId.NotSet);
        });
        _commandPipeline.Execute(_command, _serviceProvider).Returns(_ =>
        {
            _executed.Add(_command);
            return _commandResult;
        });
    }
    async Task Because() => _result = await _dispatcher.Handle(_context, _store, new object[] { _first, _invocation, Invoke.Command(_later) });

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_execute_in_order() => _executed.SequenceEqual([_first, _command]).ShouldBeTrue();
    [Fact] void should_skip_later_invocations() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
