// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_ordinary_parameter_below_range : given.annotated_parameters
{
    void Establish() => Parameter(typeof(int), 0, new RangeAttribute(1, 100));
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_identify_the_parameter() => _result.ValidationResults.Single().Members.ShouldContainOnly("input");
    [Fact] void should_use_the_display_name() => _result.ValidationResults.Single().Message.ShouldEqual("The field Label must be between 1 and 100.");
}
