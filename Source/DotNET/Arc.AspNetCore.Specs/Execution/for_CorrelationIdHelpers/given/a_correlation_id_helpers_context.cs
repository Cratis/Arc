// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Execution.for_CorrelationIdHelpers.given;

public class a_correlation_id_helpers_context : Specification
{
    protected HttpContext _httpContext;
    protected HttpRequest _httpRequest;
    protected IHeaderDictionary _headers;
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected ICorrelationIdModifier _correlationIdModifier;
    protected CorrelationIdOptions _correlationIdOptions;
    protected CorrelationId _currentCorrelationId;
    protected IDictionary<object, object?> _httpContextItems;

    void Establish()
    {
        _correlationIdOptions = new CorrelationIdOptions
        {
            HttpHeader = Constants.DefaultCorrelationIdHeader
        };

        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor, ICorrelationIdModifier>();
        _correlationIdModifier = _correlationIdAccessor as ICorrelationIdModifier;
        _currentCorrelationId = CorrelationId.NotSet;
        _correlationIdAccessor.Current.Returns(_ => _currentCorrelationId);
        _correlationIdModifier
            .When(c => c.Modify(Arg.Any<CorrelationId>()))
            .Do(c => _currentCorrelationId = c.Arg<CorrelationId>());

        _httpContext = Substitute.For<HttpContext>();
        _httpRequest = Substitute.For<HttpRequest>();
        _headers = Substitute.For<IHeaderDictionary>();
        _httpContextItems = new Dictionary<object, object?>();

        _httpContext.Request.Returns(_httpRequest);
        _httpRequest.Headers.Returns(_headers);
        _httpContext.Items.Returns(_httpContextItems);
    }
}