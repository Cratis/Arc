// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_empty_body : given.a_body_query_request_reader
{
    QueryRequest _result;

    void Establish() =>
        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(null));

    async Task Because() => _result = await _reader.Read(_context, _performer);

    [Fact] void should_have_no_arguments() => _result.Arguments.ShouldBeEmpty();
    [Fact] void should_not_be_paged() => _result.Paging.IsPaged.ShouldBeFalse();
    [Fact] void should_have_no_sorting() => _result.Sorting.ShouldEqual(Sorting.None);
}
