// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryFilters.given;

public class a_query_filters : Specification
{
    protected QueryFilters _queryFilters;
    protected QueryContext _queryContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = new(Guid.NewGuid());
        _queryContext = new("Test Query", _correlationId, Paging.NotPaged, Sorting.None);
    }
}