// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

public class a_collection_query_request : a_query_request
{
    protected QueryArguments _received;

    void Establish()
    {
        _performer.Parameters.Returns(new QueryParameters(
        [
            new QueryParameter("ids", typeof(HashSet<int>)),
            new QueryParameter("dates", typeof(DateOnly[])),
            new QueryParameter("times", typeof(IEnumerable<TimeOnly>)),
            new QueryParameter("uris", typeof(IEnumerable<Uri>)),
            new QueryParameter("optionalIds", typeof(int?[]))
        ]));
        _queryPipeline.Perform(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                _received = call.ArgAt<QueryArguments>(1);
                return Task.FromResult(QueryResult.Success(CorrelationId.New()));
            });
    }

    protected void AssertValues()
    {
        _statusCode.ShouldEqual(200);
        ((HashSet<int>)_received["ids"]).SetEquals([1, 2]).ShouldBeTrue();
        ((DateOnly[])_received["dates"]).Single().ShouldEqual(new DateOnly(2026, 5, 12));
        ((IEnumerable<TimeOnly>)_received["times"]).Single().ShouldEqual(new TimeOnly(14, 30, 45));
        ((IEnumerable<Uri>)_received["uris"]).Single().ToString().ShouldEqual("https://example.com/a");
        ((int?[])_received["optionalIds"]).SequenceEqual([3, 4]).ShouldBeTrue();
    }
}
