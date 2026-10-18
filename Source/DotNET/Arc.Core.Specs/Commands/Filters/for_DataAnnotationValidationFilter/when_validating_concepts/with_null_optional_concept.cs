// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_null_optional_concept : given.annotated_concepts
{
    void Establish() => Command(new OptionalText(null));
    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_accept_the_absent_value() => _result.IsValid.ShouldBeTrue();
}
