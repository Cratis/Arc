// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.when_reading;

public class with_a_recognized_sort_direction : given.a_query_string_query_request_reader
{
    async Task<SortDirection> DirectionFor(string sortDirection)
    {
        _context.Query.Returns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sortBy"] = "name",
            ["sortDirection"] = sortDirection
        });

        var result = await _reader.Read(_context, _performer);
        return result.Sorting.Direction;
    }

    [Fact] async Task should_sort_ascending_for_asc() => (await DirectionFor("asc")).ShouldEqual(SortDirection.Ascending);
    [Fact] async Task should_sort_ascending_for_ascending() => (await DirectionFor("ascending")).ShouldEqual(SortDirection.Ascending);
    [Fact] async Task should_sort_descending_for_desc() => (await DirectionFor("desc")).ShouldEqual(SortDirection.Descending);
    [Fact] async Task should_sort_descending_for_descending() => (await DirectionFor("descending")).ShouldEqual(SortDirection.Descending);
    [Fact] async Task should_ignore_casing() => (await DirectionFor("DeScEnDiNg")).ShouldEqual(SortDirection.Descending);
}
