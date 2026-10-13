// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

public class a_query_rejected_by_validation : a_query_request
{
    protected QueryResult _rejection;

    void Establish()
    {
        _rejection = QueryResult.WithValidationError(CorrelationId.New(), "searchTerm", "Search term is required.");
        _rejection.Data = new { Secret = "not for the client" };
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_rejection));
    }
}
