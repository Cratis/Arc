// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_ordinary_parameter_inside_range : given.annotated_parameters
{
    void Establish() => Parameter(typeof(int), 50, new RangeAttribute(1, 100));
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_accept_the_value() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_validation_errors() => _result.ValidationResults.ShouldBeEmpty();
}
