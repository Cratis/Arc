// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.when_reading;

public class with_differently_cased_reserved_keys : given.a_query_string_query_request_reader
{
    async Task<QueryRequest> Read(IDictionary<string, string> query)
    {
        _context.Query.Returns(new Dictionary<string, string>(query));
        return await _reader.Read(_context, _performer);
    }

    [Fact] async Task should_sort_with_camel_case_sort_by()
    {
        var result = await Read(new Dictionary<string, string> { ["sortBy"] = "name", ["sortDirection"] = "desc" });
        ((string)result.Sorting.Field).ShouldEqual("Name");
        result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    }

    [Fact] async Task should_sort_with_lowercase_sort_by()
    {
        var result = await Read(new Dictionary<string, string> { ["sortby"] = "name", ["sortDirection"] = "desc" });
        ((string)result.Sorting.Field).ShouldEqual("Name");
        result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    }

    [Fact] async Task should_sort_with_uppercase_keys()
    {
        var result = await Read(new Dictionary<string, string> { ["SORTBY"] = "name", ["SORTDIRECTION"] = "desc" });
        ((string)result.Sorting.Field).ShouldEqual("Name");
        result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    }

    [Fact] async Task should_page_with_uppercase_keys()
    {
        var result = await Read(new Dictionary<string, string> { ["PAGE"] = "2", ["PAGESIZE"] = "10" });
        result.Paging.IsPaged.ShouldBeTrue();
        ((int)result.Paging.Page).ShouldEqual(2);
        ((int)result.Paging.Size).ShouldEqual(10);
    }

    [Fact] async Task should_page_with_camel_case_keys()
    {
        var result = await Read(new Dictionary<string, string> { ["page"] = "1", ["pageSize"] = "25" });
        result.Paging.IsPaged.ShouldBeTrue();
        ((int)result.Paging.Page).ShouldEqual(1);
        ((int)result.Paging.Size).ShouldEqual(25);
    }

    [Fact] async Task should_keep_argument_matching_case_insensitive_and_exclude_reserved_keys()
    {
        var result = await Read(new Dictionary<string, string> { ["ID"] = "abc", ["SORTBY"] = "name", ["SORTDIRECTION"] = "asc", ["PAGESIZE"] = "5" });
        result.Arguments["id"].ShouldEqual("abc");
        result.Arguments.Count.ShouldEqual(1);
    }
}
