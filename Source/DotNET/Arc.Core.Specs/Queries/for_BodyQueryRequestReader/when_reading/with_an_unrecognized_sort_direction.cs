// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_BodyQueryRequestReader.when_reading;

public class with_an_unrecognized_sort_direction : given.a_body_query_request_reader
{
    Exception _exception;

    void Establish()
    {
        var envelope = new QueryRequestEnvelope
        {
            Sorting = new QueryRequestEnvelope.SortingRequest("displayName", "downwards")
        };

        _context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(envelope));
    }

    async Task Because() => _exception = await Catch.Exception(() => _reader.Read(_context, _performer));

    [Fact] void should_reject_the_request() => _exception.ShouldBeOfExactType<SortDirectionIsNotRecognized>();
    [Fact] void should_name_the_member_it_came_from() => ((SortDirectionIsNotRecognized)_exception).Member.ShouldEqual("sorting.direction");

    [Fact]
    void should_be_a_malformed_request_rather_than_a_server_fault() =>
        ((IValidationFailure)_exception).ValidationResult.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
}
