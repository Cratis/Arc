// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_it_executes_another_command : given.a_controller_invoking_the_pipeline
{
    CommandResult _nestedResult;
    bool _sharedInnerUnit;
    bool _pendingAfterNestedReturn;
    int _depth;

    void Establish() => _handle = async _ =>
    {
        if (_depth++ == 0)
        {
            _innerUnit = _manager.Current;
            Stage("inner");
            _nestedResult = await Execute(new OffCommand());
            _pendingAfterNestedReturn = !_innerUnit.IsCompleted && _committed.Count == 0;
        }
        else
        {
            _sharedInnerUnit = ReferenceEquals(_innerUnit, _manager.Current);
            Stage("nested");
        }
        return null;
    };

    Task Because() => Invoke();

    [Fact] void should_join_the_inner_transaction() => _sharedInnerUnit.ShouldBeTrue();
    [Fact] void should_wait_for_the_inner_owner_to_commit() => _pendingAfterNestedReturn.ShouldBeTrue();
    [Fact] void should_commit_both_commands_together() => _committed.ShouldEqual(["inner", "nested"]);
    [Fact] void should_succeed_at_every_boundary() => (ControllerResult.IsSuccess && _innerResult.IsSuccess && _nestedResult.IsSuccess).ShouldBeTrue();
}
