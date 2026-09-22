// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.when_reading;

public class with_an_unrecognized_sort_direction : given.a_query_string_query_request_reader
{
    Exception _exception;

    void Establish() => _context.Query.Returns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["sortBy"] = "name",
        ["sortDirection"] = "downwards"
    });

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_context, _performer));

    [Fact] void should_reject_the_request() => _exception.ShouldBeOfExactType<SortDirectionIsNotRecognized>();
    [Fact] void should_name_the_offending_value() => ((SortDirectionIsNotRecognized)_exception).Value.ShouldEqual("downwards");
    [Fact] void should_name_the_member_it_came_from() => ((SortDirectionIsNotRecognized)_exception).Member.ShouldEqual("sortDirection");

    [Fact]
    void should_be_a_malformed_request_rather_than_a_server_fault() =>
        ((IValidationFailure)_exception).ValidationResult.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
}
