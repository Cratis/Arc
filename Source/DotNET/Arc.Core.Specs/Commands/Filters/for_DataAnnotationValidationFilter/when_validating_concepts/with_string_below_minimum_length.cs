// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_string_below_minimum_length : given.annotated_concepts
{
    void Establish() => Command(new MinimumText(new Text("a")));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
}
