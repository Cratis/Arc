// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope;

public class when_the_commit_reports_a_constraint_violation : given.a_controller_command_with_a_transactional_scope
{
    void Establish() => _unitOfWork.GetConstraintViolations().Returns([new ConstraintViolation(
        EventTypeId.Unknown,
        EventSequenceNumber.Unavailable,
        ConstraintType.Unknown,
        new ConstraintName("UniqueValue"),
        new ConstraintViolationMessage("Value must be unique"),
        new ConstraintViolationDetails())]);

    async Task Because() => await _filter.OnActionExecutionAsync(_executing, () => Task.FromResult(_executed));

    [Fact] void should_commit_once() => _unitOfWork.Received(1).Commit();
    [Fact] void should_return_bad_request() => _executing.HttpContext.Response.StatusCode.ShouldEqual(400);
    [Fact] void should_surface_the_violation() => Result.ValidationResults.Any(_ => _.Message.Contains("Value must be unique")).ShouldBeTrue();
    [Fact] void should_not_be_successful() => Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_clear_the_response() => Result.Response.ShouldBeNull();
}
