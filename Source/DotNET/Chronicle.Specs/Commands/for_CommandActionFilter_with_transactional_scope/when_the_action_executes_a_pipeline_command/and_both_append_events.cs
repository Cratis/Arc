// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_both_append_events : given.a_controller_invoking_the_pipeline
{
    bool _innerCommittedBeforeReturn;
    bool _controllerWasStillPending;
    bool _restoredCurrent;
    bool _restoredLookup;

    void Establish() => _action = async () =>
    {
        Stage("controller before");
        _innerResult = await Execute(new OffCommand());
        _innerCommittedBeforeReturn = _committed.SequenceEqual(["inner"]);
        _controllerWasStillPending = !_controllerUnit.IsCompleted;
        _restoredCurrent = ReferenceEquals(_manager.Current, _controllerUnit) && ReferenceEquals(CommandTransaction.Current, _controllerUnit);
        _restoredLookup = _manager.TryGetFor(Correlation, out var unit) && ReferenceEquals(unit, _controllerUnit);
        Stage("controller after");
    };

    Task Because() => Invoke();

    [Fact] void should_commit_the_inner_command_before_returning() => _innerCommittedBeforeReturn.ShouldBeTrue();
    [Fact] void should_leave_the_controller_pending_until_its_action_returns() => _controllerWasStillPending.ShouldBeTrue();
    [Fact] void should_restore_both_ambient_units() => _restoredCurrent.ShouldBeTrue();
    [Fact] void should_restore_the_correlation_lookup() => _restoredLookup.ShouldBeTrue();
    [Fact] void should_commit_both_controller_appends() => _committed.ShouldEqual(["inner", "controller before", "controller after"]);
    [Fact] void should_return_controller_success() => ControllerResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_inner_success() => _innerResult.IsSuccess.ShouldBeTrue();
}
