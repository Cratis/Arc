// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandResultHandler.given;

public class a_command_result_handler : Specification
{
    protected CommandResultHandler _handler;
    protected ICommandSideEffectExecutor _executor;
    protected ReactorContext _reactorContext = null!;
    protected IEventStore _eventStore = null!;

    void Establish()
    {
        _executor = Substitute.For<ICommandSideEffectExecutor>();
        _reactorContext = new ReactorContext(null!, new TestReactor(), null!);
        _handler = new CommandResultHandler(_executor);
    }

    [Command]
    public record TestCommand(string Name)
    {
        public Task Handle() => Task.CompletedTask;
    }

    public class NotACommand;

    public class TestReactor;
}
