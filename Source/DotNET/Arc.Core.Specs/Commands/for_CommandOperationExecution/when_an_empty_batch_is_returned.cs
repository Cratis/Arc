// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_an_empty_batch_is_returned : given.an_operation_pipeline
{
    async Task Because() => _result = await Run(new given.OperationCommand([]));

    [Fact] void should_succeed_without_invocations() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_treat_the_batch_as_a_client_collection() => _result.ResponseValue.ShouldBeNull();
    [Fact] void should_report_zero_started() => _result.Recovery.StartedCount.ShouldEqual(0);
}
