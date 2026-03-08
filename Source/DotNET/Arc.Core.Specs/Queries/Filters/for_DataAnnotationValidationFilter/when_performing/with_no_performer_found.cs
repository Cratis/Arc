// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.when_performing;

public class with_no_performer_found : given.a_data_annotation_validation_filter
{
    void Establish() => EstablishNoPerformer();

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_validation_results() => _result.ValidationResults.ShouldBeEmpty();
}
