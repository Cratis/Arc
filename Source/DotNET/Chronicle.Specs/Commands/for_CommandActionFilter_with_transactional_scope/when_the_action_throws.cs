// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope;

public class when_the_action_throws : given.a_controller_command_with_a_transactional_scope
{
    void Establish() => _executed.Exception = new Exception("Action failed");

    async Task Because() => await _filter.OnActionExecutionAsync(_executing, () => Task.FromResult(_executed));

    [Fact] void should_roll_back_once() => _unitOfWork.Received(1).Rollback();
    [Fact] void should_not_commit() => _unitOfWork.DidNotReceive().Commit();
    [Fact] void should_return_internal_server_error() => _executing.HttpContext.Response.StatusCode.ShouldEqual(500);
    [Fact] void should_not_be_successful() => Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_clear_the_response() => Result.Response.ShouldBeNull();
}
