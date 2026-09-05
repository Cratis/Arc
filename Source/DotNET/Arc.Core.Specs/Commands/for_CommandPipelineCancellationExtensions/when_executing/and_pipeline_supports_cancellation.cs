// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipelineCancellationExtensions.when_executing;

public class and_pipeline_supports_cancellation : Specification
{
    ICommandPipeline _pipeline;
    ICommandPipelineWithCancellation _cancellationAwarePipeline;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly object _command = new();
    CommandResult _expectedResult;
    CommandResult _result;

    void Establish()
    {
        _expectedResult = CommandResult.Success(CorrelationId.New());
        _pipeline = Substitute.For<ICommandPipeline, ICommandPipelineWithCancellation>();
        _cancellationAwarePipeline = (ICommandPipelineWithCancellation)_pipeline;
        _cancellationAwarePipeline
            .Execute(_command, Arg.Any<ValidationResultSeverity?>(), _cancellationTokenSource.Token)
            .Returns(_expectedResult);
    }

    async Task Because() => _result = await _pipeline.Execute(_command, _cancellationTokenSource.Token);

    void Destroy() => _cancellationTokenSource.Dispose();

    [Fact] void should_execute_with_the_cancellation_aware_pipeline() => _result.ShouldEqual(_expectedResult);
    [Fact] void should_pass_the_cancellation_token() =>
        _cancellationAwarePipeline.Received(1).Execute(_command, Arg.Any<ValidationResultSeverity?>(), _cancellationTokenSource.Token);
    [Fact] void should_not_use_the_legacy_pipeline_method() =>
        _pipeline.DidNotReceive().Execute(_command, Arg.Any<ValidationResultSeverity?>());
}
