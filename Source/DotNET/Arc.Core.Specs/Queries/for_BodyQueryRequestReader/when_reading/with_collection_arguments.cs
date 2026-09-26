// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts;

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_collection_arguments : given.a_body_query_request_reader
{
    public record ProductCode(string Value) : ConceptAs<string>(Value);

    QueryRequest _result;

    void Establish()
    {
        _performer.Parameters.Returns(new QueryParameters(
        [
            new QueryParameter("ids", typeof(IEnumerable<int>)),
            new QueryParameter("names", typeof(List<string>)),
            new QueryParameter("emptyNames", typeof(string[])),
            new QueryParameter("codes", typeof(ProductCode[])),
            new QueryParameter("sets", typeof(HashSet<int>)),
            new QueryParameter("dates", typeof(DateOnly[])),
            new QueryParameter("times", typeof(IEnumerable<TimeOnly>)),
            new QueryParameter("uris", typeof(IEnumerable<Uri>)),
            new QueryParameter("optionalIds", typeof(int?[])),
            new QueryParameter("emptyIds", typeof(HashSet<int>))
        ]));
        var envelope = new QueryRequestEnvelope
        {
            Arguments = new Dictionary<string, JsonElement>
            {
                ["ids"] = JsonSerializer.SerializeToElement(new[] { 1, 2, 3 }),
                ["names"] = JsonSerializer.SerializeToElement(new[] { "first,part", "second" }),
                ["emptyNames"] = JsonSerializer.SerializeToElement(new[] { "", "x" }),
                ["codes"] = JsonSerializer.SerializeToElement(new[] { "A", "B" }),
                ["sets"] = JsonSerializer.SerializeToElement(new[] { 4, 5 }),
                ["dates"] = JsonSerializer.SerializeToElement(new[] { "2026-05-12" }),
                ["times"] = JsonSerializer.SerializeToElement(new[] { "14:30:45" }),
                ["uris"] = JsonSerializer.SerializeToElement(new[] { "https://example.com/a,b" }),
                ["optionalIds"] = JsonSerializer.SerializeToElement(new int?[] { 1, null, 3 }),
                ["emptyIds"] = JsonSerializer.SerializeToElement(Array.Empty<int>())
            }
        };
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(envelope));
    }

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_bind_integer_array() => ((IEnumerable<int>)_result.Arguments["ids"]).SequenceEqual([1, 2, 3]).ShouldBeTrue();
    [Fact] void should_preserve_string_boundaries_and_commas() => ((List<string>)_result.Arguments["names"]).SequenceEqual(["first,part", "second"]).ShouldBeTrue();
    [Fact] void should_preserve_empty_string_elements() => ((string[])_result.Arguments["emptyNames"]).SequenceEqual(["", "x"]).ShouldBeTrue();
    [Fact] void should_bind_concepts() => ((ProductCode[])_result.Arguments["codes"]).Select(_ => _.Value).SequenceEqual(["A", "B"]).ShouldBeTrue();
    [Fact] void should_bind_sets() => ((HashSet<int>)_result.Arguments["sets"]).SetEquals([4, 5]).ShouldBeTrue();
    [Fact] void should_bind_dates() => ((DateOnly[])_result.Arguments["dates"]).Single().ShouldEqual(new DateOnly(2026, 5, 12));
    [Fact] void should_bind_times() => ((IEnumerable<TimeOnly>)_result.Arguments["times"]).Single().ShouldEqual(new TimeOnly(14, 30, 45));
    [Fact] void should_bind_uris() => ((IEnumerable<Uri>)_result.Arguments["uris"]).Single().ToString().ShouldEqual("https://example.com/a,b");
    [Fact] void should_preserve_null_elements() => ((int?[])_result.Arguments["optionalIds"]).SequenceEqual([1, null, 3]).ShouldBeTrue();
    [Fact] void should_bind_an_empty_set() => ((HashSet<int>)_result.Arguments["emptyIds"]).ShouldBeEmpty();
}
