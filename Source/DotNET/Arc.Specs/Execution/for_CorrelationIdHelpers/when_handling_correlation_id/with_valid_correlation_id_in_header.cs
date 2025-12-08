// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Execution.for_CorrelationIdHelpers.when_handling_correlation_id;

public class with_valid_correlation_id_in_header : given.a_correlation_id_helpers_context
{
    CorrelationId _correlationId;
    string _correlationIdAsString;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _correlationIdAsString = _correlationId.ToString();
        _headers[Constants.DefaultCorrelationIdHeader].Returns(new StringValues(_correlationIdAsString));
    }

    void Because() => _httpContext.HandleCorrelationId(_correlationIdAccessor, _correlationIdOptions);

    [Fact] void should_set_correlation_id_from_header() => _correlationIdModifier.Received(1).Modify(_correlationId);
    [Fact] void should_set_correlation_id_in_http_context_items() => _httpContextItems[Constants.CorrelationIdItemKey].ShouldEqual(_correlationId);
    [Fact] void should_not_modify_request_header() => _headers.DidNotReceiveWithAnyArgs()[Constants.DefaultCorrelationIdHeader] = default;
}