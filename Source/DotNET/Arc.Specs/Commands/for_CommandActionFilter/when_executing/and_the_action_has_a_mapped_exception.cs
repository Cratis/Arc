// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Exceptions;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_action_has_a_mapped_exception : given.a_post_request_with_execution_scopes
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing", true)]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required", false)]
    async Task should_set_safe_status_headers_and_metadata(HttpStatusCode status, string code, bool delay)
    {
        var mapper = Substitute.For<IExceptionResponseMapper>();
        mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, delay ? new Dictionary<string, string> { ["Retry-After"] = "5" } : null);
            return true;
        });
        var original = _actionContext.HttpContext.RequestServices;
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(IInstancesOf<IExceptionResponseMapper>)
            ? new KnownInstancesOf<IExceptionResponseMapper>([mapper]) : original.GetService(call.Arg<Type>()));
        _actionContext.HttpContext.RequestServices = services;
        _executedContext.Exception = new InvalidOperationException("private failure detail");
        await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));
        _actionContext.HttpContext.Response.StatusCode.ShouldEqual((int)status);
        ResultingCommandResult.ErrorCode.ShouldEqual(code);
        ResultingCommandResult.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        ResultingCommandResult.ExceptionStackTrace.ShouldBeEmpty();
        ResultingCommandResult.IsSuccess.ShouldBeFalse();
        _actionContext.HttpContext.Response.Headers.ContainsKey("Retry-After").ShouldEqual(delay);
        if (delay) _actionContext.HttpContext.Response.Headers.RetryAfter.ToString().ShouldEqual("5");
    }
}
