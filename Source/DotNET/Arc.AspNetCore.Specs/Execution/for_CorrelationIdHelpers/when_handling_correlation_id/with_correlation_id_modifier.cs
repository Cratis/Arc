// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.Extensions.Primitives;

namespace Cratis.Arc.Execution.for_CorrelationIdHelpers.when_handling_correlation_id;

public class with_correlation_id_modifier : given.a_correlation_id_helpers_context
{
    CorrelationId _correlationId;
    string _correlationIdAsString;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _correlationIdAsString = _correlationId.ToString();
        _headers[Constants.DefaultCorrelationIdHeader].Returns(new StringValues(_correlationIdAsString));

        // Ensure the accessor implements ICorrelationIdModifier (already set up in the base context)
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor, ICorrelationIdModifier>();
        _correlationIdModifier = _correlationIdAccessor as ICorrelationIdModifier;
    }

    void Because() => _httpContext.HandleCorrelationId(_correlationIdAccessor, _correlationIdOptions);

    [Fact] void should_call_modify_on_correlation_id_modifier() => _correlationIdModifier.Received(1).Modify(_correlationId);
    [Fact] void should_set_correlation_id_in_http_context_items() => _httpContextItems[Constants.CorrelationIdItemKey].ShouldEqual(_correlationId);
}