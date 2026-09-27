// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.when_reading;

public class with_invalid_concept_collection_element : given.a_query_string_query_request_reader
{
    InvalidQueryArgument _exception;
    QueryResult _result;

    void Establish()
    {
        _performer.Parameters.Returns(new QueryParameters([new QueryParameter("rates", typeof(Rate[]))]));
        _performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName("Rates.ByRate"));
        _context.Query.Returns(new Dictionary<string, string> { ["rates"] = "1,bad" });
    }

    async Task Because()
    {
        _exception = await Catch.Exception(() => _reader.Read(_context, _performer)) as InvalidQueryArgument;
        _result = QueryResult.FromException(CorrelationId.New(), _exception);
    }

    [Fact] void should_reject_the_invalid_element() => _exception.ShouldNotBeNull();
    [Fact] void should_name_the_invalid_argument() => _exception.ValidationResult.Members.ShouldContainOnly("rates");
    [Fact] void should_be_a_validation_result() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_malformed_request() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
    [Fact] void should_name_the_parameter_and_type_in_the_message() => _exception.Message.ShouldContain("'rates' of type 'Rate[]'");
    [Fact] void should_not_be_a_server_error() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_name_the_argument_in_the_result() => _result.ValidationResults.Single().Members.ShouldContain("rates");

    public record Rate(decimal Value) : ConceptAs<decimal>(Value);
}
