// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope;

public class when_the_command_succeeds : given.a_controller_command_with_a_transactional_scope
{
    async Task Because() => await _filter.OnActionExecutionAsync(_executing, () => Task.FromResult(_executed));

    [Fact] void should_commit_once() => _unitOfWork.Received(1).Commit();
    [Fact] void should_not_roll_back() => _unitOfWork.DidNotReceive().Rollback();
    [Fact] void should_return_ok() => _executing.HttpContext.Response.StatusCode.ShouldEqual(200);
    [Fact] void should_keep_the_response() => Result.Response.ShouldEqual("response");
    [Fact] void should_be_successful() => Result.IsSuccess.ShouldBeTrue();
}
