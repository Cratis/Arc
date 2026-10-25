// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_one_overlapping_command_has_an_immediate_append_failure : given.a_controller_invoking_the_pipeline
{
    readonly TaskCompletionSource _bothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _failureReported = new(TaskCreationOptions.RunContinuationsAsynchronously);
    CommandResult[] _results;
    int _entered;

    void Establish()
    {
        _handle = async _ =>
        {
            if (Interlocked.Increment(ref _entered) == 1)
            {
                await _bothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
                _appends.OnNext([new(null!, AppendResult.Failed(Correlation, [new AppendError("one inner append failed")]))]);
                _failureReported.SetResult();
            }
            else
            {
                _bothEntered.SetResult();
                await _failureReported.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Stage("successful sibling");
            }
            return null;
        };
        _action = async () => _results = await Task.WhenAll(Execute(new OffCommand()), Execute(new OffCommand()));
    }

    Task Because() => Invoke();

    [Fact] void should_fail_only_the_writer() => _results.Select(_ => _.IsSuccess).ShouldEqual([false, true]);
    [Fact] void should_not_fail_the_controller() => ControllerResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_commit_the_sibling() => _committed.ShouldEqual(["successful sibling"]);
}
