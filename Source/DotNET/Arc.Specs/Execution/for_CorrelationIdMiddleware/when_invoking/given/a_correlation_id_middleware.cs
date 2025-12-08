// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Execution.for_CorrelationIdMiddleware.when_invoking.given;

public class a_correlation_id_middleware : Specification
{
    protected CorrelationIdMiddleware _correlationIdMiddleware;
    protected HttpContext _httpContext;
    protected RequestDelegate _next;
    protected IOptions<ArcOptions> _options;
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected ICorrelationIdModifier _correlationIdModifier;
    protected HttpRequest _httpRequest;
    protected IHeaderDictionary _headers;
    protected IDictionary<object, object?> _items;
    protected CorrelationIdOptions _correlationIdOptions;
    protected CorrelationId _currentCorrelationId;

    void Establish()
    {
        _correlationIdOptions = new CorrelationIdOptions
        {
            HttpHeader = Constants.DefaultCorrelationIdHeader
        };
        _options = Options.Create(new ArcOptions
        {
            CorrelationId = _correlationIdOptions
        });
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor, ICorrelationIdModifier>();
        _correlationIdModifier = _correlationIdAccessor as ICorrelationIdModifier;
        _currentCorrelationId = CorrelationId.NotSet;
        _correlationIdAccessor.Current.Returns(_ => _currentCorrelationId);
        _correlationIdModifier
            .When(c => c.Modify(Arg.Any<CorrelationId>()))
            .Do(c => _currentCorrelationId = c.Arg<CorrelationId>());

        _httpContext = Substitute.For<HttpContext>();
        _httpRequest = Substitute.For<HttpRequest>();
        _httpContext.Request.Returns(_httpRequest);
        _headers = Substitute.For<IHeaderDictionary>();
        _httpRequest.Headers.Returns(_headers);
        _items = new Dictionary<object, object?>();
        _httpContext.Items.Returns(_items);

        _correlationIdMiddleware = new CorrelationIdMiddleware(_options, _correlationIdAccessor);
        _next = Substitute.For<RequestDelegate>();
    }
}