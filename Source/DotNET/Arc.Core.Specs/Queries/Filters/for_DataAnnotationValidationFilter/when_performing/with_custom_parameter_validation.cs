// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_custom_parameter_validation : given.annotated_parameters
{
    void Establish() => Parameter(typeof(string), "abc", new AlwaysFailsAttribute());
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
}
