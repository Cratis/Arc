// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_a_protected_command_has_no_authorization_declarations : given.a_command_pipeline
{
    ICommandHandler _handler;
    CommandResult _result;

    void Establish()
    {
        _handler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders.TryGetHandlerFor(new ProtectedCommand(), out anyHandler).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _serviceProvider.GetService(typeof(AuthorizationDeclarations)).Returns((object?)null);
    }

    async Task Because() => _result = await _commandPipeline.Execute(new ProtectedCommand(), _serviceProvider);

    [Fact] void should_report_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_execute_the_handler() => _handler.DidNotReceive().Handle(Arg.Any<CommandContext>());

    [Authorize(Policy = "Allow")]
    public record ProtectedCommand;
}
