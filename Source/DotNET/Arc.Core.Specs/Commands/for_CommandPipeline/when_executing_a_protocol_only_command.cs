// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandPipeline;

public class when_executing_a_protocol_only_command : given.a_command_pipeline
{
    ProtocolCommand _command;
    CommandResult _result;
    CommandHandlerProvider _provider;

    void Establish()
    {
        _command = new ProtocolCommand();
        var types = Substitute.For<ITypes>();
        types.All.Returns([typeof(ProtocolCommand)]);
        _provider = new CommandHandlerProvider(types);
        _provider.TryGetHandlerFor(_command, out var handler);
        ICommandHandler anyHandler = null!;
        _commandHandlerProviders.TryGetHandlerFor(_command, out anyHandler).Returns(call =>
        {
            call[1] = handler;
            return true;
        });
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _serviceProvider);

    [Fact] void should_keep_the_discovered_handler() => _provider.Handlers.Single().CommandType.ShouldEqual(typeof(ProtocolCommand));
    [Fact] void should_run_the_execution_filters() => _commandFilters.Received(1).OnExecution(Arg.Is<CommandContext>(context => ReferenceEquals(context.Command, _command)));
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_the_handler_response() => _result.Response.ShouldEqual("protocol response");

    [Command]
    [ProtocolOnly]
    public record ProtocolCommand
    {
        public string Handle() => "protocol response";
    }
}
