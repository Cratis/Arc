// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_json_nodes_in_the_query_body : given.a_collection_query_request
{
    void Establish()
    {
        _performer.Parameters.Returns(new QueryParameters(
        [
            new QueryParameter("objects", typeof(IEnumerable<JsonObject>)),
            new QueryParameter("arrays", typeof(IEnumerable<JsonArray>))
        ]));
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new QueryRequestEnvelope
            {
                Arguments = new Dictionary<string, JsonElement>
                {
                    ["objects"] = JsonSerializer.SerializeToElement(new[] { new { id = 1 }, new { id = 2 } }),
                    ["arrays"] = JsonSerializer.SerializeToElement(new int[][] { [1, 2], [3, 4] })
                }
            }));
    }

    async Task Because() => await _mapper.HandlerFor("QUERY")(_context);

    [Fact]
    void should_bind_json_object_and_array_elements_from_query_body()
    {
        _statusCode.ShouldEqual(200);
        var objects = ((IEnumerable<JsonObject>)_received["objects"]).ToArray();
        objects.Select(node => node["id"]!.GetValue<int>()).SequenceEqual([1, 2]).ShouldBeTrue();
        var arrays = ((IEnumerable<JsonArray>)_received["arrays"]).ToArray();
        arrays.Select(node => node.Count).SequenceEqual([2, 2]).ShouldBeTrue();
        arrays[1][0]!.GetValue<int>().ShouldEqual(3);
    }
}
