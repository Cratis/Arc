// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandHandlerProviders;

public class when_a_second_container_registers_a_duplicate_handler : Specification
{
    Exception _exception;

    void Because()
    {
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(InternalCommand)]);
        var first = new CommandHandlerProvider(types);
        _ = new CommandHandlerProviders(new KnownInstancesOf<ICommandHandlerProvider>([first]));

        var duplicate = Substitute.For<ICommandHandler>();
        duplicate.CommandType.Returns(typeof(InternalCommand));
        var additional = Substitute.For<ICommandHandlerProvider>();
        additional.Handlers.Returns([duplicate]);

        var second = new CommandHandlerProvider(types);
        _exception = Catch.Exception(() => _ = new CommandHandlerProviders(new KnownInstancesOf<ICommandHandlerProvider>([second, additional])));
    }

    [Fact] void should_still_reject_a_duplicate() => _exception.ShouldBeOfExactType<MultipleCommandHandlersForSameCommandType>();
}
