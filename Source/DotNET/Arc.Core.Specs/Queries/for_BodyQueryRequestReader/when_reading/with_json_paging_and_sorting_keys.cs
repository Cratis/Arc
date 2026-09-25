// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_json_paging_and_sorting_keys : given.a_body_query_request_reader
{
    QueryRequest _result;

    void Establish()
    {
        const string json = """{"arguments":{"COUNT":5},"paging":{"page":2,"pageSize":10},"sorting":{"field":"name","direction":"desc"}}""";
        var envelope = JsonSerializer.Deserialize<QueryRequestEnvelope>(json, new ArcOptions().JsonSerializerOptions);
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(envelope));
    }

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_read_camel_case_page_size() => ((int)_result.Paging.Size).ShouldEqual(10);
    [Fact] void should_read_page() => ((int)_result.Paging.Page).ShouldEqual(2);
    [Fact] void should_read_sort_field() => ((string)_result.Sorting.Field).ShouldEqual("Name");
    [Fact] void should_read_sort_direction() => _result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    [Fact] void should_match_argument_name_regardless_of_case() => _result.Arguments["count"].ShouldEqual(5);
}
