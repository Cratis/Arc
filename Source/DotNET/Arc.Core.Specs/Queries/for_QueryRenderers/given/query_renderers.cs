// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Cratis.Types;

namespace Cratis.Arc.Queries.for_QueryRenderers.given;

public class query_renderers : Specification
{
    protected ITypes _types;
    protected IQueryContextManager _queryContextManager;
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected IServiceProvider _serviceProvider;
    protected QueryContext _queryContext;
    protected QueryRenderers _renderers;

    void Establish()
    {
        _types = Substitute.For<ITypes>();
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _queryContext = new("Test Query", CorrelationId.New(), Paging.NotPaged, Sorting.None) { TotalItems = 42 };
        _queryContextManager.Current.Returns(_queryContext);
    }

    protected void Construct(params Type[] rendererTypes)
    {
        _types.FindMultiple(typeof(IQueryRendererFor<>)).Returns(rendererTypes);
        _renderers = new(_queryContextManager, _correlationIdAccessor, _types);
    }

    protected void Register(object renderer) => _serviceProvider.GetService(renderer.GetType()).Returns(renderer);
}
