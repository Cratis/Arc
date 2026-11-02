// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_command_pipeline_and_a_handler_for_command : a_command_pipeline
{
    protected ICommandHandler _commandHandler;
    protected string _command;

    void Establish()
    {
        _command = Guid.NewGuid().ToString();
        _commandHandler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders
            .TryGetHandlerFor(_command, out anyHandler)
            .Returns(r =>
            {
                r[1] = _commandHandler;
                return true;
            });
    }

    /// <summary>
    /// Routes argument resolution through the production <see cref="CommandHandlerArgumentResolver"/>, so generated values are resolved and validated as in production.
    /// </summary>
    /// <param name="commandType">The command type declaring the handler method.</param>
    /// <param name="handle">The handler method whose parameters are resolved.</param>
    protected void UseTheRealArgumentResolver(Type commandType, MethodInfo handle)
    {
        _commandHandler.CommandType.Returns(commandType);
        _commandHandler.Parameters.Returns(handle.GetParameters());
        var provideInvoker = Substitute.For<ICommandProvideInvoker>();
        provideInvoker.Invoke(Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>())
            .Returns(_ => new ValueTask<IReadOnlyList<object>>([]));
        var resolver = new CommandHandlerArgumentResolver(provideInvoker);
        _commandHandlerArgumentResolver
            .Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call => resolver.Resolve(
                call.Arg<ICommandHandler>(),
                call.Arg<CommandContext>() with { ReceivedAt = DateTimeOffset.UtcNow },
                call.Arg<IServiceProvider>(),
                call.Arg<ValidationResultSeverity?>()));
    }
}
