// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_valid_collection_in_the_query_body : given.a_collection_query_request
{
    void Establish() =>
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new QueryRequestEnvelope
            {
                Arguments = new Dictionary<string, JsonElement>
                {
                    ["ids"] = JsonSerializer.SerializeToElement(new[] { 1, 2 }),
                    ["dates"] = JsonSerializer.SerializeToElement(new[] { "2026-05-12" }),
                    ["times"] = JsonSerializer.SerializeToElement(new[] { "14:30:45" }),
                    ["uris"] = JsonSerializer.SerializeToElement(new[] { "https://example.com/a" }),
                    ["optionalIds"] = JsonSerializer.SerializeToElement(new int?[] { 3, 4 })
                }
            }));

    async Task Because() => await _mapper.HandlerFor("QUERY")(_context);

    [Fact] void should_return_ok_with_the_right_query_body_values() => AssertValues();
}
