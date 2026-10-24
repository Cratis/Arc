// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope.when_the_action_executes_a_pipeline_command;

public class and_the_inner_commit_is_refused : given.a_controller_invoking_the_pipeline
{
    bool _refusedBeforeReturn;

    void Establish()
    {
        var successfulAppend = _append;
        _append = events => events.Any(_ => Equals(_.Event, "inner"))
            ? AppendManyResult.Failed(Correlation, [new ConstraintViolation(
                EventTypeId.Unknown,
                EventSequenceNumber.Unavailable,
                ConstraintType.Unknown,
                new ConstraintName("UniqueValue"),
                new ConstraintViolationMessage("Value must be unique"),
                new ConstraintViolationDetails())])
            : successfulAppend(events);
        _action = async () =>
        {
            Stage("controller");
            _innerResult = await Execute(new OffCommand());
            _refusedBeforeReturn = !_innerResult.IsSuccess && _innerUnit.IsCompleted;
        };
    }

    Task Because() => Invoke();

    [Fact] void should_report_the_inner_refusal_before_returning() => _refusedBeforeReturn.ShouldBeTrue();
    [Fact] void should_include_the_constraint_violation() => _innerResult.ValidationResults.Any(_ => _.Message.Contains("Value must be unique")).ShouldBeTrue();
    [Fact] void should_commit_only_the_controller_events() => _committed.ShouldEqual(["controller"]);
    [Fact] void should_keep_the_controller_successful() => ControllerResult.IsSuccess.ShouldBeTrue();
}
