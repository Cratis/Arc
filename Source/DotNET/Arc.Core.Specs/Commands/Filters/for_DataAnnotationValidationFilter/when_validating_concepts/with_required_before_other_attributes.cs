// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_required_before_other_attributes : given.annotated_concepts
{
    void Establish() => Command(new RequiredFirst(new Text("valid"), string.Empty));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_report_only_the_required_error() => _result.ValidationResults.Single().Message.ShouldEqual("The Label field is required.");
}
