// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing;

public class and_the_action_has_a_mapped_exception : given.a_controller_query_request
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "namespace_closing", true)]
    [InlineData(HttpStatusCode.Conflict, "namespace_recovery_required", false)]
    async Task should_set_safe_status_headers_and_metadata(HttpStatusCode status, string code, bool delay)
    {
        _options.ExposeExceptionDetails = true;
        var mapper = Substitute.For<IExceptionResponseMapper>();
        mapper.TryMap(Arg.Any<Exception>(), out Arg.Any<ExceptionResponse>()).Returns(call =>
        {
            call[1] = new ExceptionResponse(status, code, delay ? new Dictionary<string, string> { ["Retry-After"] = "5" } : null);
            return true;
        });
        var original = _httpContext.RequestServices;
        var services = Substitute.For<IServiceProvider>();
        services.GetService(Arg.Any<Type>()).Returns(call => call.Arg<Type>() == typeof(IInstancesOf<IExceptionResponseMapper>)
            ? new KnownInstancesOf<IExceptionResponseMapper>([mapper]) : original.GetService(call.Arg<Type>()));
        _httpContext.RequestServices = services;
        _executed.Exception = new InvalidOperationException("private failure detail");
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        var result = (QueryResult)((ObjectResult)(_actionContext.Result ?? _executed.Result)!).Value!;
        _httpContext.Response.StatusCode.ShouldEqual((int)status);
        result.ErrorCode.ShouldEqual(code);
        result.ExceptionMessages.ShouldContainOnly(ExceptionDetailRedactor.RedactedMessage);
        result.ExceptionStackTrace.ShouldBeEmpty();
        result.IsSuccess.ShouldBeFalse();
        _httpContext.Response.Headers.ContainsKey("Retry-After").ShouldEqual(delay);
        if (delay) _httpContext.Response.Headers.RetryAfter.ToString().ShouldEqual("5");
    }
}
