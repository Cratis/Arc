// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_forward_execution_is_canceled : given.an_operation_pipeline
{
    CancellationTokenSource _cancellation;

    void Establish()
    {
        _cancellation = new();
        _log.CancelAfterExecution = _cancellation;
    }

    async Task Because() => _result = await Run(new given.OperationCommand([new given.NamedOperation("A"), new given.NamedOperation("B")]), _cancellation.Token);
    void Destroy() => _cancellation.Dispose();

    [Fact] void should_forward_the_original_token() => _log.ForwardTokens[0].ShouldEqual(_cancellation.Token);
    [Fact] void should_only_compensate_started_work() => _log.Calls.ShouldEqual(["execute:A", "compensate:A"]);
    [Fact] void should_supply_an_independent_cleanup_token() => _log.RecoveryTokens[0].IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_report_cancellation_as_original_source() => _log.Failures[0].Source.ShouldEqual(CommandOperationFailureSource.Cancellation);
}
