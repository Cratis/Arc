// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_rejecting_invalid_arguments;

public class with_warning_overrides : given.a_controller_query_request
{
    QueryResult _result;

    void Establish()
    {
        InvalidArguments();
        _webSocketFeature.IsWebSocketRequest.Returns(true);
        _actionContext.Filters.Add(new TreatWarningsAsErrorsAttribute());
        _httpContext.Request.Headers["X-Ignore-Warnings"] = "true";
    }

    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        _result = Deserialize(_frames.Single());
    }

    [Fact] void should_not_ignore_argument_errors() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_keep_error_severity() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_send_a_terminal_rejection() => _transportEvents.ShouldEqual(["accept", "send", "close"]);
}
