// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_a_long_form_sort_direction : given.a_body_query_request_reader
{
    QueryRequest _result;

    void Establish()
    {
        var envelope = new QueryRequestEnvelope
        {
            Sorting = new QueryRequestEnvelope.SortingRequest("displayName", "descending")
        };

        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(envelope));
    }

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_sort_descending() => _result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
}
