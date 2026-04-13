// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Execution;
using Cratis.Execution;

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_handling_correlation_id;

public class with_correlation_id_modifier_and_valid_header : Specification
{
    IHttpRequestContext _context;
    ICorrelationIdAccessor _correlationIdAccessor;
    ICorrelationIdModifier _correlationIdModifier;
    CorrelationIdOptions _options;
    CorrelationId _correlationId;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor, ICorrelationIdModifier>();
        _correlationIdModifier = (ICorrelationIdModifier)_correlationIdAccessor;
        _options = new CorrelationIdOptions();
        _correlationId = CorrelationId.New();

        _context.Headers.Returns(new Dictionary<string, string>
        {
            [Constants.DefaultCorrelationIdHeader] = _correlationId.ToString()
        });
    }

    void Because() => _context.HandleCorrelationId(_correlationIdAccessor, _options);

    [Fact] void should_modify_the_accessor_with_the_resolved_correlation_id() => _correlationIdModifier.Received(1).Modify(_correlationId);
    [Fact] void should_set_the_response_header() => _context.Received().SetResponseHeader(Constants.DefaultCorrelationIdHeader, _correlationId.Value.ToString());
}
