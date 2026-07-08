// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandsResultHandler.when_handling;

public class commands : given.a_commands_result_handler
{
    object[] _commands;
    Result<ReactorSideEffectFailure> _expected;
    Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _commands = [new TestCommand("first"), new TestCommand("second")];
        _expected = Result.Success<ReactorSideEffectFailure>();
        _executor.Execute(Arg.Any<IEnumerable<object>>()).Returns(_expected);
    }

    async Task Because() => _result = await _handler.Handle(_reactorContext, _eventStore, _commands);

    [Fact] void should_execute_the_commands_through_the_executor() => _executor.Received(1).Execute(_commands);
    [Fact] void should_return_the_result_from_the_executor() => _result.ShouldEqual(_expected);
}
