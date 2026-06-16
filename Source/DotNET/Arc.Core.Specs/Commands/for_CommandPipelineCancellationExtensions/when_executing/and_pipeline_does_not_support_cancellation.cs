// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandPipelineCancellationExtensions.when_executing;

public class and_pipeline_does_not_support_cancellation : Specification
{
    ICommandPipeline _pipeline;
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly object _command = new();
    CommandResult _expectedResult;
    CommandResult _result;

    void Establish()
    {
        _expectedResult = CommandResult.Success(CorrelationId.New());
        _pipeline = Substitute.For<ICommandPipeline>();
        _pipeline
            .Execute(_command, Arg.Any<ValidationResultSeverity?>())
            .Returns(_expectedResult);
    }

    async Task Because() => _result = await _pipeline.Execute(_command, _cancellationTokenSource.Token);

    void Destroy() => _cancellationTokenSource.Dispose();

    [Fact] void should_fall_back_to_the_legacy_pipeline_method() => _result.ShouldEqual(_expectedResult);
    [Fact] void should_execute_the_legacy_pipeline_method() =>
        _pipeline.Received(1).Execute(_command, Arg.Any<ValidationResultSeverity?>());
}
