// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.AspNetCore.Http.for_HttpContextCorrelationIdExtensions.when_getting_correlation_id;

public class with_request_header_and_no_context_item : Specification
{
    Cratis.Execution.CorrelationId _correlationId;
    DefaultHttpContext _httpContext;
    Cratis.Execution.CorrelationId _result;

    void Establish()
    {
        _correlationId = Cratis.Execution.CorrelationId.New();
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Headers[Cratis.Arc.Execution.Constants.DefaultCorrelationIdHeader] = _correlationId.ToString();
    }

    void Because() => _result = _httpContext.GetCorrelationId();

    [Fact] void should_return_the_correlation_id_from_the_request_header() => _result.ShouldEqual(_correlationId);
}
