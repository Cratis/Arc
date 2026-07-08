// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandResultHandler.when_handling;

public class a_command : given.a_command_result_handler
{
    TestCommand _command;
    Result<ReactorSideEffectFailure> _expected;
    Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _command = new TestCommand("Test");
        _expected = Result.Success<ReactorSideEffectFailure>();
        _executor.Execute(Arg.Any<IEnumerable<object>>()).Returns(_expected);
    }

    async Task Because() => _result = await _handler.Handle(_reactorContext, _eventStore, _command);

    [Fact] void should_execute_the_command_through_the_executor() => _executor.Received(1).Execute(Arg.Is<IEnumerable<object>>(_ => _.Single().Equals(_command)));
    [Fact] void should_return_the_result_from_the_executor() => _result.ShouldEqual(_expected);
}
