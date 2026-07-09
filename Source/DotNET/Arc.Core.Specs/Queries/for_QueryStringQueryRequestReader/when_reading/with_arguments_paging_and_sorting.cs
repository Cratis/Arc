// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.when_reading;

public class with_arguments_paging_and_sorting : given.a_query_string_query_request_reader
{
    QueryRequest _result;

    void Establish()
    {
        IReadOnlyDictionary<string, string> query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "abc",
            ["page"] = "1",
            ["pageSize"] = "10",
            ["sortby"] = "name",
            ["sortDirection"] = "desc"
        };
        _context.Query.Returns(query);
    }

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_read_the_argument() => _result.Arguments["id"].ShouldEqual("abc");
    [Fact] void should_be_paged() => _result.Paging.IsPaged.ShouldBeTrue();
    [Fact] void should_have_page_size() => ((int)_result.Paging.Size).ShouldEqual(10);
    [Fact] void should_sort_descending() => _result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    [Fact] void should_pascal_case_the_sort_field() => ((string)_result.Sorting.Field).ShouldEqual("Name");
}
