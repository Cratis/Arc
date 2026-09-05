// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandResponseValueHandlers.when_the_command_carries_its_own_scope;

/// <summary>
/// A handler is resolved from the scope the command runs in, not from the provider that constructed this singleton.
/// Resolving from the latter means resolving from the root, where a handler depending on a scoped service — as every
/// Chronicle response value handler does through <c>IEventLog</c> — cannot be created at all.
/// </summary>
public class and_the_scope_offers_handlers : Specification
{
    CommandResponseValueHandlers _handlers;
    ICommandResponseValueHandler _handlerFromTheScope;
    ICommandResponseValueHandler _handlerFromTheRoot;
    CommandContext _context;
    string _value;
    CommandResult _result;

    void Establish()
    {
        _handlerFromTheScope = Substitute.For<ICommandResponseValueHandler>();
        _handlerFromTheScope.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(true);
        _handlerFromTheScope.Handle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(CommandResult.Success(CorrelationId.New()));

        _handlerFromTheRoot = Substitute.For<ICommandResponseValueHandler>();
        _handlerFromTheRoot.CanHandle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(true);
        _handlerFromTheRoot.Handle(Arg.Any<CommandContext>(), Arg.Any<object>()).Returns(CommandResult.Success(CorrelationId.New()));

        var scope = Substitute.For<IServiceProvider>();
        scope.GetService(typeof(IInstancesOf<ICommandResponseValueHandler>))
            .Returns(new KnownInstancesOf<ICommandResponseValueHandler>([_handlerFromTheScope]));

        _handlers = new(new KnownInstancesOf<ICommandResponseValueHandler>([_handlerFromTheRoot]));
        _context = new(CorrelationId.New(), typeof(string), "Something", [], new(), ServiceProvider: scope);
        _value = "Forty two";
    }

    async Task Because() => _result = await _handlers.Handle(_context, _value);

    [Fact] void should_handle_with_the_handler_from_the_scope() => _handlerFromTheScope.Received().Handle(_context, _value);
    [Fact] void should_not_handle_with_the_handler_from_the_root() => _handlerFromTheRoot.DidNotReceive().Handle(_context, _value);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
