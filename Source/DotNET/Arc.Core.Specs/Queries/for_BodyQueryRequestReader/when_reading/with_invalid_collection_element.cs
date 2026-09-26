// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_invalid_collection_element : given.a_body_query_request_reader
{
    [Theory]
    [InlineData("[1,\"bad\"]")]
    [InlineData("[1,{}]")]
    [InlineData("[1,null]")]
    async Task should_reject_invalid_elements_with_the_named_scalar_error_shape(string json)
    {
        _performer.Parameters.Returns(new QueryParameters([new QueryParameter("ids", typeof(int[]))]));
        _performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Numbers.ByIds"));
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new QueryRequestEnvelope
            {
                Arguments = new Dictionary<string, JsonElement> { ["ids"] = JsonDocument.Parse(json).RootElement }
            }));

        var error = await Catch.Exception(() => _reader.Read(_context, _performer));
        (error is InvalidQueryArgument).ShouldBeTrue();
        var result = ((InvalidQueryArgument)error).ValidationResult;
        result.Members.ShouldContainOnly("ids");
        result.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
        result.Message.ShouldContain("'ids' of type 'Int32[]'");
    }
}
