// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.when_handling_a_query;

public class with_invalid_collection : given.a_query_request
{
    void Establish() => _performer.Parameters.Returns(new QueryParameters(
    [
        new QueryParameter("ids", typeof(int[]))
    ]));

    [Fact]
    async Task should_return_bad_request_for_invalid_get_element()
    {
        _context.Query.Returns(new Dictionary<string, string> { ["ids"] = "1,invalid" });
        await _mapper.HandlerFor("GET")(_context);
        AssertValidationResponse();
    }

    [Fact]
    async Task should_return_bad_request_for_invalid_query_element()
    {
        SetBody(JsonSerializer.SerializeToElement(new object[] { 1, "invalid" }));
        await _mapper.HandlerFor("QUERY")(_context);
        AssertValidationResponse();
    }

    [Fact]
    async Task should_return_bad_request_for_null_query_element()
    {
        SetBody(JsonSerializer.SerializeToElement(new int?[] { 1, null }));
        await _mapper.HandlerFor("QUERY")(_context);
        AssertValidationResponse();
    }

    [Fact]
    async Task should_return_bad_request_for_null_string_element()
    {
        _performer.Parameters.Returns(new QueryParameters([new QueryParameter("ids", typeof(string[]))]));
        SetBody(JsonSerializer.SerializeToElement(new string?[] { null, "x" }));
        await _mapper.HandlerFor("QUERY")(_context);
        AssertValidationResponse();
    }

    [Fact]
    async Task should_return_bad_request_for_nested_query_array()
    {
        _performer.Parameters.Returns(new QueryParameters([new QueryParameter("ids", typeof(int[][]))]));
        SetBody(JsonSerializer.SerializeToElement(new[] { new[] { 1, 2 } }));
        await _mapper.HandlerFor("QUERY")(_context);
        AssertValidationResponse();
    }

    [Theory]
    [InlineData(typeof(JsonObject))]
    [InlineData(typeof(JsonArray))]
    async Task should_return_bad_request_for_json_node_collections_on_get(Type elementType)
    {
        var collectionType = typeof(IEnumerable<>).MakeGenericType(elementType);
        _performer.Parameters.Returns(new QueryParameters([new QueryParameter("ids", collectionType)]));
        _context.Query.Returns(new Dictionary<string, string> { ["ids"] = elementType == typeof(JsonObject) ? "{\"id\":1}" : "[1,2]" });

        await _mapper.HandlerFor("GET")(_context);

        AssertValidationResponse();
    }

    void SetBody(JsonElement ids) =>
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new QueryRequestEnvelope
            {
                Arguments = new Dictionary<string, JsonElement> { ["ids"] = ids }
            }));

    void AssertValidationResponse()
    {
        _statusCode.ShouldEqual(400);
        _context.Received(1).WriteResponseAsJson(
            Arg.Is<QueryResult>(result => !result.IsValid && !result.HasExceptions &&
                result.ValidationResults.Any(validation => validation.Reason == ValidationResultReason.MalformedRequest)),
            typeof(QueryResult),
            Arg.Any<CancellationToken>());
        _queryPipeline.DidNotReceive().Perform(
            Arg.Any<FullyQualifiedQueryName>(),
            Arg.Any<QueryArguments>(),
            Arg.Any<Paging>(),
            Arg.Any<Sorting>(),
            Arg.Any<IServiceProvider>());
    }
}
