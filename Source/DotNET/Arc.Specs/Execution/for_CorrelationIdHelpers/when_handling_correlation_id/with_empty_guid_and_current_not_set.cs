// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Execution.for_CorrelationIdHelpers.when_handling_correlation_id;

public class with_empty_guid_and_current_not_set : given.a_correlation_id_helpers_context
{
    string _correlationIdAsString;

    void Establish()
    {
        _correlationIdAsString = Guid.Empty.ToString();
        _headers[Constants.DefaultCorrelationIdHeader].Returns(new StringValues(_correlationIdAsString));
        _currentCorrelationId = CorrelationId.NotSet;
    }

    void Because() => _httpContext.HandleCorrelationId(_correlationIdAccessor, _correlationIdOptions);

    [Fact] void should_generate_new_correlation_id() => _currentCorrelationId.ShouldNotEqual(CorrelationId.NotSet);
    [Fact] void should_set_current_correlation_id() => _correlationIdModifier.Received(1).Modify(_currentCorrelationId);
    [Fact] void should_set_correlation_id_in_http_context_items() => _httpContextItems[Constants.CorrelationIdItemKey].ShouldEqual(_currentCorrelationId);
    [Fact] void should_set_correlation_id_in_request_header() => _headers.Received()[Constants.DefaultCorrelationIdHeader] = _currentCorrelationId.ToString();
}