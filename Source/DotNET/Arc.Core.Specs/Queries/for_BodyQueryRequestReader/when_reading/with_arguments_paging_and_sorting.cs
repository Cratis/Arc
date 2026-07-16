// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_arguments_paging_and_sorting : given.a_body_query_request_reader
{
    QueryRequest _result;

    void Establish()
    {
        var envelope = new QueryRequestEnvelope
        {
            Arguments = new Dictionary<string, JsonElement>
            {
                ["count"] = JsonSerializer.SerializeToElement(5),
                ["name"] = JsonSerializer.SerializeToElement("bob")
            },
            Paging = new QueryRequestEnvelope.PagingRequest(2, 25),
            Sorting = new QueryRequestEnvelope.SortingRequest("displayName", "desc")
        };

        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(envelope));
    }

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_convert_int_argument_to_its_parameter_type() => _result.Arguments["count"].ShouldEqual(5);
    [Fact] void should_pass_string_argument() => _result.Arguments["name"].ShouldEqual("bob");
    [Fact] void should_be_paged() => _result.Paging.IsPaged.ShouldBeTrue();
    [Fact] void should_have_page_size() => ((int)_result.Paging.Size).ShouldEqual(25);
    [Fact] void should_have_page() => ((int)_result.Paging.Page).ShouldEqual(2);
    [Fact] void should_sort_descending() => _result.Sorting.Direction.ShouldEqual(SortDirection.Descending);
    [Fact] void should_pascal_case_the_sort_field() => ((string)_result.Sorting.Field).ShouldEqual("DisplayName");
}
