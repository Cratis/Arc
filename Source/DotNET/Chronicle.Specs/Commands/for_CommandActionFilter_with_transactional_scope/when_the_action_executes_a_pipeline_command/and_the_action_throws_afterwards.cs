// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_the_action_throws_afterwards : given.a_controller_invoking_the_pipeline
{
    void Establish() => _action = async () =>
    {
        Stage("controller");
        _innerResult = await Execute(new OffCommand());
        throw new InvalidOperationException("Action failed");
    };

    Task Because() => Invoke();

    [Fact] void should_keep_the_inner_commit() => _committed.ShouldEqual(["inner"]);
    [Fact] void should_keep_the_inner_result_successful() => _innerResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_roll_back_the_controller() => _controllerUnit.IsCompleted.ShouldBeTrue();
    [Fact] void should_fail_the_controller_result() => ControllerResult.IsSuccess.ShouldBeFalse();
}
