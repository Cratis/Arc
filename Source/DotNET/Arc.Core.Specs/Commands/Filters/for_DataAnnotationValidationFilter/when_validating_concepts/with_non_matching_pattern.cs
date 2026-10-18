// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_non_matching_pattern : given.annotated_concepts
{
    void Establish() => Command(new PatternedText(new Text("lower")));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
}
