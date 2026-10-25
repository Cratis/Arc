// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_an_inner_immediate_append_fails : given.a_controller_invoking_the_pipeline
{
    void Establish()
    {
        _handle = _ =>
        {
            Stage("inner");
            _appends.OnNext([new(null!, AppendResult.Failed(Correlation, [new AppendError("inner append failed")]))]);
            return Task.FromResult<object?>(null);
        };
        _action = async () =>
        {
            Stage("controller");
            _innerResult = await Execute(new OffCommand());
        };
    }

    Task Because() => Invoke();

    [Fact] void should_fail_the_inner_command() => _innerResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_fail_the_controller() => ControllerResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_commit_only_the_controller() => _committed.ShouldEqual(["controller"]);
}
