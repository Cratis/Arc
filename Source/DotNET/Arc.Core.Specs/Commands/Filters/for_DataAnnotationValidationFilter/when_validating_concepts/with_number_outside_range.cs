// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_number_outside_range : given.annotated_concepts
{
    void Establish() => Command(new RangedNumber(new Number(6)));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_identify_the_member() => _result.ValidationResults.Single().Members.ShouldContainOnly("count");
}
