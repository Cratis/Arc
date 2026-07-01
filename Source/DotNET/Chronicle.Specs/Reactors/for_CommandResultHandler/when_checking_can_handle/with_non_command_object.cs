// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandResultHandler.when_checking_can_handle;

public class with_non_command_object : given.a_command_result_handler
{
    NotACommand _notACommand;
    bool _result;

    void Establish() => _notACommand = new NotACommand { Name = "Test" };

    void Because() => _result = _handler.CanHandle(_reactorContext, _notACommand);

    [Fact] void should_return_false() => _result.ShouldBeFalse();
}
