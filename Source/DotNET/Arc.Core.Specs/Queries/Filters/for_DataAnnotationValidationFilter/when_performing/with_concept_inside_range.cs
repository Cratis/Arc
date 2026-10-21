// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_concept_inside_range : given.annotated_concepts
{
    void Establish() => ConceptParameter("count", new Number(3), value => ((Number)value).Value, new RangeAttribute(1, 5));
    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_accept_the_value() => _result.IsValid.ShouldBeTrue();
}
