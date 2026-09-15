// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_nullable_operation_values_are_present : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new Command());

    [Fact] void should_execute_the_boxed_nullable_values() => _log.Calls.ShouldEqual(["execute:value", "execute:batch"]);
    [Fact] void should_report_both_started_operations() => _result.Recovery.StartedCount.ShouldEqual(2);
    [Fact] void should_not_treat_operations_as_a_client_response() => _result.ResponseValue.ShouldBeNull();
    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();

    public readonly record struct ValueOperation : ICommandOperation
    {
        public Task Execute(given.OperationLog log, CancellationToken cancellationToken) => log.Execute("value", cancellationToken);
    }

    [Command]
    public record Command
    {
        public (ValueOperation?, CommandOperations?) Handle() => (default(ValueOperation), new CommandOperations([new given.NamedOperation("batch")]));
    }
}
