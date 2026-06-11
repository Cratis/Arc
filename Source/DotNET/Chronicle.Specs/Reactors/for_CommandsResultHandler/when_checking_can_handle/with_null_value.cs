// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors.for_CommandsResultHandler.when_checking_can_handle;

public class with_null_value : given.a_commands_result_handler
{
    bool _result;

    void Because() => _result = _handler.CanHandle(_reactorContext, null!);

    [Fact] void should_not_be_able_to_handle() => _result.ShouldBeFalse();
}
