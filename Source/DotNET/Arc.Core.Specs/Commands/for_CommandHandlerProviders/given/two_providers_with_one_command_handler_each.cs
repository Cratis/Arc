// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandHandlerProviders.given;

public class two_providers_with_one_command_handler_each : Specification
{
    protected ICommandHandlerProviders _providers;
    ICommandHandlerProvider _firstProvider;
    ICommandHandlerProvider _secondProvider;
    protected ICommandHandler _handlerFirstProvider;
    protected ICommandHandler _handlerSecondProvider;

    void Establish()
    {
        _handlerFirstProvider = Substitute.For<ICommandHandler>();
        _handlerFirstProvider.CommandType.Returns(typeof(string));
        _firstProvider = Substitute.For<ICommandHandlerProvider>();
        _firstProvider.Handlers.Returns([_handlerFirstProvider]);

        _handlerSecondProvider = Substitute.For<ICommandHandler>();
        _handlerSecondProvider.CommandType.Returns(typeof(object));
        _secondProvider = Substitute.For<ICommandHandlerProvider>();
        _secondProvider.Handlers.Returns([_handlerSecondProvider]);

        _providers = new CommandHandlerProviders(new KnownInstancesOf<ICommandHandlerProvider>([_firstProvider, _secondProvider]));
    }
}
