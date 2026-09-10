// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_executing_sequentially : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), new given.NamedOperation("B")]));

    [Fact] void should_execute_in_declaration_order() => _log.Calls.ShouldEqual(["execute:A", "execute:B"]);
    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_record_both_returns() => _result.Recovery.CompletedCount.ShouldEqual(2);
    [Fact] void should_not_compensate() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.NotNeeded);
    [Fact] void should_not_expose_an_operation_response() => _result.ResponseValue.ShouldBeNull();
    [Fact] void should_not_offer_operations_to_overlapping_value_handlers() => _commandResponseValueHandlers.DidNotReceive().Handle(Arg.Any<CommandContext>(), Arg.Any<ICommandOperation>());
}
