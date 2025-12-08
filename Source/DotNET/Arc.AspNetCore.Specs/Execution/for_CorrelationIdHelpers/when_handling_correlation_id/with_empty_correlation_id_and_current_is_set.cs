// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Execution.for_CorrelationIdHelpers.when_handling_correlation_id;

public class with_empty_correlation_id_and_current_is_set : given.a_correlation_id_helpers_context
{
    string _correlationIdAsString;
    CorrelationId _existingCorrelationId;

    void Establish()
    {
        _correlationIdAsString = string.Empty;
        _headers[Constants.DefaultCorrelationIdHeader].Returns(new StringValues(_correlationIdAsString));
        _existingCorrelationId = CorrelationId.New();
        _currentCorrelationId = _existingCorrelationId;
    }

    void Because() => _httpContext.HandleCorrelationId(_correlationIdAccessor, _correlationIdOptions);

    [Fact] void should_use_existing_correlation_id() => _currentCorrelationId.ShouldEqual(_existingCorrelationId);
    [Fact] void should_not_set_current_correlation_id() => _correlationIdModifier.DidNotReceive().Modify(Arg.Any<CorrelationId>());
    [Fact] void should_set_correlation_id_in_http_context_items() => _httpContextItems[Constants.CorrelationIdItemKey].ShouldEqual(_existingCorrelationId);
    [Fact] void should_set_correlation_id_in_request_header() => _headers.Received()[Constants.DefaultCorrelationIdHeader] = _existingCorrelationId.ToString();
}