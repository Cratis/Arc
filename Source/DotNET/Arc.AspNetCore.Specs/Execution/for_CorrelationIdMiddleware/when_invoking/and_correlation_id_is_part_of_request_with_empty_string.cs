// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Execution.for_CorrelationIdMiddleware.when_invoking;

public class and_correlation_id_is_part_of_request_with_empty_string : given.a_correlation_id_middleware
{
    string _correlationIdAsString;
    CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _correlationIdAsString = string.Empty;
        _headers[Constants.DefaultCorrelationIdHeader].Returns(new StringValues(_correlationIdAsString));
    }

    Task Because() => _correlationIdMiddleware.InvokeAsync(_httpContext, _next);

    [Fact] void should_not_set_an_empty_correlation_id() => _currentCorrelationId.ShouldNotEqual(CorrelationId.NotSet);
    [Fact] void should_set_current_correlation_id() => _correlationIdModifier.Received(1).Modify(_currentCorrelationId);
    [Fact] void should_set_correlation_id_in_http_context_items() => _httpContext.Items[Constants.CorrelationIdItemKey].ShouldEqual(_currentCorrelationId);
}