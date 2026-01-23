// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryPipeline.given;

public class a_query_pipeline : Specification
{
    protected QueryPipeline _pipeline;
    protected ICorrelationIdAccessor _correlationIdAccessor;
    protected IQueryContextManager _queryContextManager;
    protected IQueryFilters query_filters;
    protected IQueryPerformerProviders _queryPerformerProviders;
    protected IQueryRenderers _queryRenderers;
    protected IServiceProvider _serviceProvider;
    protected IQueryPerformer _queryPerformer;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();

        _correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        _correlationIdAccessor.Current.Returns(_correlationId);

        _queryContextManager = Substitute.For<IQueryContextManager>();
        query_filters = Substitute.For<IQueryFilters>();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        _queryRenderers = Substitute.For<IQueryRenderers>();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _queryPerformer = Substitute.For<IQueryPerformer>();

        _pipeline = new QueryPipeline(
            _correlationIdAccessor,
            _queryContextManager,
            query_filters,
            _queryPerformerProviders,
            _queryRenderers);
    }
}