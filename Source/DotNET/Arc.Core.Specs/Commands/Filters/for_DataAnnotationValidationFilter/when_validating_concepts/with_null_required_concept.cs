// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_null_required_concept : given.annotated_concepts
{
    void Establish() => Command(new RequiredText(null));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_report_required_only_once() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_reject_the_null_value() => _result.IsValid.ShouldBeFalse();
}
