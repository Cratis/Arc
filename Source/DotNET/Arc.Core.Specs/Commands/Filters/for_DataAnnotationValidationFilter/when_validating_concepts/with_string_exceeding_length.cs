// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_string_exceeding_length : given.annotated_concepts
{
    Exception? _error;

    void Establish() => Command(new SizedText(new Text("long")));
    async Task Because() => _error = await Catch.Exception(async () => _result = await _filter.OnExecution(_context));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_report_the_length_rule() => _result.ValidationResults.Single().Message.ShouldEqual("The field Label must be a string with a maximum length of 3.");
}
