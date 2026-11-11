// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.when_reading;

public class with_excluded_wait_for_first_result_keys : given.a_query_string_query_request_reader
{
    QueryRequest _result;

    void Establish() => _context.Query.Returns(new Dictionary<string, string>
    {
        ["WaItFoRfIrStReSuLt"] = "true",
        ["wAiTfOrFiRsTrEsUlTtImEoUt"] = "1000",
        ["id"] = "abc",
        ["custom"] = "value"
    });

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_exclude_wait_for_first_result() => _result.Arguments.ContainsKey("waitForFirstResult").ShouldBeFalse();
    [Fact] void should_exclude_wait_for_first_result_timeout() => _result.Arguments.ContainsKey("waitForFirstResultTimeout").ShouldBeFalse();
    [Fact] void should_keep_the_declared_argument() => _result.Arguments["id"].ShouldEqual("abc");
    [Fact] void should_keep_the_undeclared_argument() => _result.Arguments["custom"].ShouldEqual("value");
    [Fact] void should_only_include_non_reserved_arguments() => _result.Arguments.Count.ShouldEqual(2);
}
