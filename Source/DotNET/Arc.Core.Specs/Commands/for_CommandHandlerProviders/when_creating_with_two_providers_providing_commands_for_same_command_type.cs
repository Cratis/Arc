// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerProviders;

public class when_creating_with_two_providers_providing_commands_for_same_command_type : Specification
{
    ICommandHandlerProvider _firstProvider;
    ICommandHandlerProvider _secondProvider;
    ICommandHandler _handlerFirstProvider;
    ICommandHandler _handlerSecondProvider;
    Exception _result;

    void Establish()
    {
        _handlerFirstProvider = Substitute.For<ICommandHandler>();
        _handlerFirstProvider.CommandType.Returns(typeof(string));
        _firstProvider = Substitute.For<ICommandHandlerProvider>();
        _firstProvider.Handlers.Returns([_handlerFirstProvider]);

        _handlerSecondProvider = Substitute.For<ICommandHandler>();
        _handlerSecondProvider.CommandType.Returns(typeof(string));
        _secondProvider = Substitute.For<ICommandHandlerProvider>();
        _secondProvider.Handlers.Returns([_handlerSecondProvider]);
    }

    void Because() => _result = Catch.Exception(() => _ = new CommandHandlerProviders(new KnownInstancesOf<ICommandHandlerProvider>([_firstProvider, _secondProvider])));

    [Fact] void should_throw_exception_indicating_there_are_multiple_handlers() => _result.ShouldBeOfExactType<MultipleCommandHandlersForSameCommandType>();
}