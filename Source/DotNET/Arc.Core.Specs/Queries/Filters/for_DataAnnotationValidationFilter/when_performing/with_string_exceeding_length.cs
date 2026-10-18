// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_string_exceeding_length : given.annotated_parameters
{
    void Establish() => Parameter(typeof(string), "abcd", new StringLengthAttribute(3));
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_identify_the_parameter() => _result.ValidationResults.Single().Members.ShouldContainOnly("input");
}
