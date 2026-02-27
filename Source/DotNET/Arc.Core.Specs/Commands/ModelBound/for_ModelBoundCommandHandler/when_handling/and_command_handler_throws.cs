// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.ModelBound.for_ModelBoundCommandHandler.when_handling;

/// <summary>
/// When the command method itself throws, the exception is wrapped by the CLR in a
/// <see cref="System.Reflection.TargetInvocationException"/> because we invoke it via reflection.
/// The handler must unwrap it so callers see the real exception, not the reflection wrapper.
/// </summary>
public class and_command_handler_throws : Specification
{
    public class CommandException : Exception
    {
        public CommandException() : base("real command error") { }
    }

    record Command()
    {
        public void Handle() => throw new CommandException();
    }

    ModelBoundCommandHandler _handler;
    CommandContext _context;
    Exception _thrownException;

    void Establish()
    {
        _context = new(CorrelationId.New(), typeof(Command), new Command(), [], new());
        _handler = new ModelBoundCommandHandler(
            typeof(Command),
            typeof(Command).GetMethod(nameof(Command.Handle))!);
    }

    async Task Because() => _thrownException = await Catch.Exception(async () => await _handler.Handle(_context));

    [Fact] void should_throw_the_original_exception_type() => _thrownException.ShouldBeOfExactType<CommandException>();
    [Fact] void should_not_wrap_in_target_invocation_exception() => (_thrownException is System.Reflection.TargetInvocationException).ShouldBeFalse();
    [Fact] void should_preserve_the_original_message() => _thrownException.Message.ShouldEqual("real command error");
}
