// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.given;

public class a_data_annotation_validation_filter : Specification
{
    protected IQueryPerformerProviders _queryPerformerProviders;
    protected IQueryPerformer _queryPerformer;
    protected DataAnnotationValidationFilter _filter;
    protected QueryContext _context;
    protected CorrelationId _correlationId;
    protected QueryResult _result;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        _queryPerformer = Substitute.For<IQueryPerformer>();
        _filter = new DataAnnotationValidationFilter(_queryPerformerProviders);
    }

    protected void EstablishPerformerWith(QueryParameters parameters, QueryArguments arguments)
    {
        _queryPerformer.Parameters.Returns(parameters);
        _queryPerformerProviders
            .TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer>())
            .Returns(callInfo =>
            {
                callInfo[1] = _queryPerformer;
                return true;
            });

        _context = new QueryContext(
            new FullyQualifiedQueryName("TestReadModel.Query"),
            _correlationId,
            Paging.NotPaged,
            Sorting.None,
            arguments,
            []);
    }

    protected void EstablishNoPerformer()
    {
        _context = new QueryContext(
            new FullyQualifiedQueryName("TestReadModel.Query"),
            _correlationId,
            Paging.NotPaged,
            Sorting.None,
            QueryArguments.Empty,
            []);
    }
}
