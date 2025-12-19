// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerProviders.when_trying_to_resolve;

public class and_there_are_are_two_providers_for_different_command_types : given.two_providers_with_one_command_handler_each
{
    object _command;
    bool _result;
    ICommandHandler? _handler;

    void Establish() => _command = new object();

    void Because()
    {
        _result = _providers.TryGetHandlerFor(_command, out var handler);
        _handler = handler;
    }

    [Fact] void should_find_a_handler() => _result.ShouldBeTrue();
    [Fact] void should_return_the_correct_handler() => _handler.ShouldEqual(_handlerSecondProvider);
}
