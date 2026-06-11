// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandResultHandler.when_checking_can_handle;

public class with_valid_command : given.a_command_result_handler
{
    TestCommand _command;
    bool _result;

    void Establish() => _command = new TestCommand("Test");

    void Because() => _result = _handler.CanHandle(_reactorContext, _command);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
}
