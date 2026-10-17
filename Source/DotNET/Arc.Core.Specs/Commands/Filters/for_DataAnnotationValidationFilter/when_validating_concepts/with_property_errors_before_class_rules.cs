// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_property_errors_before_class_rules : given.annotated_concepts
{
    void Establish() => Command(new TypeRule(new Text(string.Empty)));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_report_only_the_property_error() => _result.ValidationResults.Single().Message.ShouldEqual("The Label field is required.");
}
