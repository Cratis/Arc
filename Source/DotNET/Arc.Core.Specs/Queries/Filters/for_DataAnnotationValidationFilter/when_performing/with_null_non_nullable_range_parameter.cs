// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_null_non_nullable_range_parameter : given.annotated_parameters
{
    void Establish() => Parameter(typeof(int), null, new RangeAttribute(1, 100));
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_reject_the_value() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_preserve_the_required_message() => _result.ValidationResults.Single().Message.ShouldEqual("The field input is required.");
}
