// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerProviders.when_trying_to_resolve;

public class and_there_are_no_handlers_for_command_type : given.two_providers_with_one_command_handler_each
{
    object _command;
    bool _result;
    ICommandHandler? _handler;

    void Establish() => _command = 42;

    void Because()
    {
        _result = _providers.TryGetHandlerFor(_command, out var handler);
        _handler = handler;
    }

    [Fact] void should_not_find_a_handler() => _result.ShouldBeFalse();
    [Fact] void should_return_null_handler() => _handler.ShouldBeNull();
}