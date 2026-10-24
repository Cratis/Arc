// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_valid_collection : given.a_collection_query_request
{
    void Establish() =>
        _context.Query.Returns(new Dictionary<string, string>
        {
            ["ids"] = "1,2",
            ["dates"] = "2026-05-12",
            ["times"] = "14:30:45",
            ["uris"] = "https://example.com/a",
            ["optionalIds"] = "3,4"
        });

    async Task Because() => await _mapper.HandlerFor("GET")(_context);

    [Fact] void should_return_ok_with_the_right_get_values() => AssertValues();
}
