// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_an_unclassified_scope_is_present : given.an_operation_pipeline
{
    void Establish() => _scopes = [_executionScope];
    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A")]));

    [Fact] void should_reject_before_execution() => _log.Calls.ShouldBeEmpty();
    [Fact] void should_explain_explicit_scope_compatibility() => _result.ExceptionMessages.Single().ShouldContain("ICommandOperationExecutionScope");
    [Fact] void should_not_begin_the_incompatible_scope() => _executionScope.DidNotReceive().Begin(Arg.Any<CommandContext>());
    [Fact] void should_not_complete_a_scope_that_never_began() => _executionScope.DidNotReceive().Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
}
