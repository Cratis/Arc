// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandActionFilter_with_transactional_scope;

public class when_the_commit_reports_an_append_error : given.a_controller_command_with_a_transactional_scope
{
    void Establish() => _unitOfWork.GetAppendErrors().Returns([new AppendError("Append failed")]);

    async Task Because() => await _filter.OnActionExecutionAsync(_executing, () => Task.FromResult(_executed));

    [Fact] void should_commit_once() => _unitOfWork.Received(1).Commit();
    [Fact] void should_return_internal_server_error() => _executing.HttpContext.Response.StatusCode.ShouldEqual(500);
    [Fact] void should_surface_the_error() => Result.ExceptionMessages.ShouldContain("Append failed");
    [Fact] void should_not_be_successful() => Result.IsSuccess.ShouldBeFalse();
    [Fact] void should_clear_the_response() => Result.Response.ShouldBeNull();
}
