// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_valid_collection : given.a_query_request
{
    QueryArguments _received;

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

    [Fact]
    async Task should_return_ok_with_the_right_get_values()
    {
        _context.Query.Returns(new Dictionary<string, string>
        {
            ["ids"] = "1,2",
            ["dates"] = "2026-05-12",
            ["times"] = "14:30:45",
            ["uris"] = "https://example.com/a",
            ["optionalIds"] = "3,4"
        });
        await _mapper.HandlerFor("GET")(_context);
        AssertValues();
    }

    [Fact]
    async Task should_return_ok_with_the_right_query_body_values()
    {
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
        await _mapper.HandlerFor("QUERY")(_context);
        AssertValues();
    }

    [Fact]
    async Task should_bind_json_object_and_array_elements_from_query_body()
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

        await _mapper.HandlerFor("QUERY")(_context);

        _statusCode.ShouldEqual(200);
        var objects = ((IEnumerable<JsonObject>)_received["objects"]).ToArray();
        objects.Select(node => node["id"]!.GetValue<int>()).SequenceEqual([1, 2]).ShouldBeTrue();
        var arrays = ((IEnumerable<JsonArray>)_received["arrays"]).ToArray();
        arrays.Select(node => node.Count).SequenceEqual([2, 2]).ShouldBeTrue();
        arrays[1][0]!.GetValue<int>().ShouldEqual(3);
    }

    void AssertValues()
    {
        _statusCode.ShouldEqual(200);
        ((HashSet<int>)_received["ids"]).SetEquals([1, 2]).ShouldBeTrue();
        ((DateOnly[])_received["dates"]).Single().ShouldEqual(new DateOnly(2026, 5, 12));
        ((IEnumerable<TimeOnly>)_received["times"]).Single().ShouldEqual(new TimeOnly(14, 30, 45));
        ((IEnumerable<Uri>)_received["uris"]).Single().ToString().ShouldEqual("https://example.com/a");
        ((int?[])_received["optionalIds"]).SequenceEqual([3, 4]).ShouldBeTrue();
    }
}
